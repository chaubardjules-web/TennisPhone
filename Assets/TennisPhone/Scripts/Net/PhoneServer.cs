using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace TennisPhone
{
    /// <summary>
    /// Serveur local du jeu, sans dépendance externe :
    /// - en HTTPS : la page manette et le WebSocket qui reçoit les capteurs ;
    /// - en HTTP simple : la page d'installation du certificat sur le téléphone.
    /// Tout tourne sur des threads d'arrière-plan. Le jeu lit les files Samples et Messages
    /// depuis le thread principal.
    /// </summary>
    public sealed class PhoneServer : IDisposable
    {
        const int MaxQueuedSamples = 600;
        const int MaxRequestHeadBytes = 8192;
        const int RequestTimeoutMs = 10000;
        const int WebSocketTimeoutMs = 15000;

        public readonly ConcurrentQueue<SensorSample> Samples = new ConcurrentQueue<SensorSample>();
        public readonly ConcurrentQueue<string> Messages = new ConcurrentQueue<string>();

        readonly int httpsPort;
        readonly int httpPort;
        readonly X509Certificate2 certificate;
        readonly byte[] authorityCertificateDer;
        readonly byte[] controllerPage;
        readonly byte[] onboardingPage;
        readonly Stopwatch clock = Stopwatch.StartNew();
        readonly object clientsLock = new object();
        readonly List<Client> clients = new List<Client>();

        TcpListener httpsListener;
        TcpListener httpListener;
        volatile Client activeClient;
        volatile bool running;
        volatile float roundTripMs = -1f;
        volatile float clockOffsetMs;
        long totalSamples;
        long tlsFailures;

        sealed class Client
        {
            public TcpClient Tcp;
            public Stream Stream;
            public readonly object SendLock = new object();
        }

        public PhoneServer(int httpsPort, int httpPort, X509Certificate2 certificate,
            byte[] authorityCertificateDer, string controllerHtml, string onboardingHtml)
        {
            this.httpsPort = httpsPort;
            this.httpPort = httpPort;
            this.certificate = certificate;
            this.authorityCertificateDer = authorityCertificateDer;
            controllerPage = Encoding.UTF8.GetBytes(controllerHtml ?? string.Empty);
            onboardingPage = Encoding.UTF8.GetBytes(onboardingHtml ?? string.Empty);
        }

        public bool IsRunning => running;

        public int ClientCount
        {
            get
            {
                lock (clientsLock)
                {
                    return clients.Count;
                }
            }
        }

        /// <summary>Aller-retour réseau mesuré par le téléphone, en millisecondes (-1 si inconnu).</summary>
        public float RoundTripMs => roundTripMs;

        /// <summary>Écart « horloge serveur moins horloge téléphone », estimé par le téléphone.</summary>
        public float ClockOffsetMs => clockOffsetMs;

        public long TotalSamples => Interlocked.Read(ref totalSamples);

        public long TlsFailures => Interlocked.Read(ref tlsFailures);

        /// <summary>Horloge du serveur, en millisecondes depuis sa création.</summary>
        public double NowMs => clock.Elapsed.TotalMilliseconds;

        public void Start()
        {
            if (running)
            {
                return;
            }

            httpsListener = CreateListener(httpsPort);
            httpListener = CreateListener(httpPort);
            running = true;

            StartThread(() => AcceptLoop(httpsListener, true), "TennisPhone HTTPS");
            StartThread(() => AcceptLoop(httpListener, false), "TennisPhone HTTP");
        }

        public void Dispose()
        {
            running = false;
            StopListener(httpsListener);
            StopListener(httpListener);

            Client[] snapshot;
            lock (clientsLock)
            {
                snapshot = clients.ToArray();
                clients.Clear();
            }

            foreach (Client client in snapshot)
            {
                try
                {
                    client.Tcp.Close();
                }
                catch
                {
                    // Déjà fermé.
                }
            }
        }

        /// <summary>Envoie une ligne de texte à tous les téléphones connectés.</summary>
        public void Broadcast(string text)
        {
            Client[] snapshot;
            lock (clientsLock)
            {
                snapshot = clients.ToArray();
            }

            foreach (Client client in snapshot)
            {
                TrySend(client, text);
            }
        }

        static TcpListener CreateListener(int port)
        {
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Start();
            return listener;
        }

        static void StopListener(TcpListener listener)
        {
            try
            {
                listener?.Stop();
            }
            catch
            {
                // Déjà arrêté.
            }
        }

        static void StartThread(ThreadStart work, string name)
        {
            var thread = new Thread(work) { IsBackground = true, Name = name };
            thread.Start();
        }

        void AcceptLoop(TcpListener listener, bool tls)
        {
            while (running)
            {
                TcpClient tcp;
                try
                {
                    tcp = listener.AcceptTcpClient();
                }
                catch
                {
                    if (!running)
                    {
                        return;
                    }

                    Thread.Sleep(50);
                    continue;
                }

                StartThread(() => HandleConnection(tcp, tls), "TennisPhone client");
            }
        }

        void HandleConnection(TcpClient tcp, bool tls)
        {
            try
            {
                tcp.NoDelay = true;
                tcp.ReceiveTimeout = RequestTimeoutMs;
                tcp.SendTimeout = RequestTimeoutMs;

                Stream stream = tcp.GetStream();
                if (tls)
                {
                    var ssl = new SslStream(stream, false);
                    try
                    {
                        ssl.AuthenticateAsServer(certificate, false, SslProtocols.Tls12, false);
                    }
                    catch (Exception exception) when (IsConnectionLoss(exception))
                    {
                        // Cas habituel tant que le certificat n'est pas approuvé sur le téléphone.
                        Interlocked.Increment(ref tlsFailures);
                        return;
                    }

                    stream = ssl;
                }

                if (!TryReadRequest(stream, out string path, out Dictionary<string, string> headers))
                {
                    return;
                }

                if (tls)
                {
                    HandleSecureRequest(tcp, stream, path, headers);
                }
                else
                {
                    HandlePlainRequest(stream, path);
                }
            }
            catch (Exception exception)
            {
                if (running && !IsConnectionLoss(exception))
                {
                    Messages.Enqueue("Connexion fermée : " + exception.GetType().Name + " : " + exception.Message);
                }
            }
            finally
            {
                try
                {
                    tcp.Close();
                }
                catch
                {
                    // Déjà fermé.
                }
            }
        }

        /// <summary>
        /// Vrai pour les erreurs normales d'une connexion qui se ferme ou d'une poignée de main TLS
        /// refusée, y compris quand le runtime les enveloppe dans une autre exception.
        /// </summary>
        static bool IsConnectionLoss(Exception exception)
        {
            for (int depth = 0; exception != null && depth < 8; depth++)
            {
                if (exception is IOException || exception is SocketException
                    || exception is ObjectDisposedException || exception is AuthenticationException)
                {
                    return true;
                }

                exception = exception is AggregateException aggregate && aggregate.InnerExceptions.Count > 0
                    ? aggregate.InnerExceptions[0]
                    : exception.InnerException;
            }

            return false;
        }

        void HandleSecureRequest(TcpClient tcp, Stream stream, string path, Dictionary<string, string> headers)
        {
            bool upgrade = headers.TryGetValue("upgrade", out string upgradeValue)
                && upgradeValue.IndexOf("websocket", StringComparison.OrdinalIgnoreCase) >= 0;

            if (path == "/ws" && upgrade && headers.TryGetValue("sec-websocket-key", out string key))
            {
                RunWebSocket(tcp, stream, key);
            }
            else if (path == "/" || path == "/index.html")
            {
                WriteResponse(stream, "200 OK", "text/html; charset=utf-8", controllerPage);
            }
            else if (path == "/tennisphone-ca.crt")
            {
                WriteResponse(stream, "200 OK", "application/x-x509-ca-cert", authorityCertificateDer);
            }
            else
            {
                WriteResponse(stream, "404 Not Found", "text/plain; charset=utf-8", Encoding.UTF8.GetBytes("Introuvable"));
            }
        }

        void HandlePlainRequest(Stream stream, string path)
        {
            if (path == "/" || path == "/index.html")
            {
                WriteResponse(stream, "200 OK", "text/html; charset=utf-8", onboardingPage);
            }
            else if (path == "/tennisphone-ca.crt")
            {
                WriteResponse(stream, "200 OK", "application/x-x509-ca-cert", authorityCertificateDer);
            }
            else
            {
                WriteResponse(stream, "404 Not Found", "text/plain; charset=utf-8", Encoding.UTF8.GetBytes("Introuvable"));
            }
        }

        void RunWebSocket(TcpClient tcp, Stream stream, string key)
        {
            string handshake =
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: " + WebSocketFrames.ComputeAcceptKey(key) + "\r\n\r\n";
            byte[] handshakeBytes = Encoding.ASCII.GetBytes(handshake);
            stream.Write(handshakeBytes, 0, handshakeBytes.Length);
            stream.Flush();

            tcp.ReceiveTimeout = WebSocketTimeoutMs;
            var client = new Client { Tcp = tcp, Stream = stream };
            lock (clientsLock)
            {
                // Le dernier téléphone connecté devient la raquette ; les autres sont ignorés.
                clients.Add(client);
                activeClient = client;
            }

            try
            {
                while (running && WebSocketFrames.TryReadFrame(stream, out byte opcode, out bool _, out byte[] payload))
                {
                    if (opcode == WebSocketFrames.OpcodeText)
                    {
                        HandleText(client, Encoding.UTF8.GetString(payload));
                    }
                    else if (opcode == WebSocketFrames.OpcodePing)
                    {
                        TrySendFrame(client, WebSocketFrames.EncodeFrame(WebSocketFrames.OpcodePong, payload));
                    }
                    else if (opcode == WebSocketFrames.OpcodeClose)
                    {
                        TrySendFrame(client, WebSocketFrames.EncodeFrame(WebSocketFrames.OpcodeClose, new byte[0]));
                        break;
                    }
                }
            }
            finally
            {
                bool removed;
                lock (clientsLock)
                {
                    removed = clients.Remove(client);
                    if (activeClient == client)
                    {
                        activeClient = clients.Count > 0 ? clients[clients.Count - 1] : null;
                    }
                }

                if (removed && running)
                {
                    Messages.Enqueue("Téléphone déconnecté.");
                }
            }
        }

        void HandleText(Client client, string text)
        {
            if (text.StartsWith(SensorSample.Prefix))
            {
                if (client == activeClient && SensorSample.TryParse(text, NowMs, out SensorSample sample))
                {
                    Interlocked.Increment(ref totalSamples);
                    if (Samples.Count < MaxQueuedSamples)
                    {
                        Samples.Enqueue(sample);
                    }
                }
            }
            else if (text.StartsWith("p,"))
            {
                // Synchronisation d'horloge : on renvoie l'heure du téléphone et la nôtre.
                TrySend(client, "q," + text.Substring(2) + ","
                    + NowMs.ToString("F1", CultureInfo.InvariantCulture));
            }
            else if (text.StartsWith("s,"))
            {
                string[] parts = text.Split(',');
                if (client == activeClient && parts.Length >= 3
                    && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float rtt)
                    && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float offset))
                {
                    roundTripMs = rtt;
                    clockOffsetMs = offset;
                }
            }
            else if (text.StartsWith("h,"))
            {
                Messages.Enqueue("Téléphone connecté : " + text.Substring(2));
            }
        }

        void TrySend(Client client, string text)
        {
            TrySendFrame(client, WebSocketFrames.EncodeText(text));
        }

        static void TrySendFrame(Client client, byte[] frame)
        {
            try
            {
                lock (client.SendLock)
                {
                    client.Stream.Write(frame, 0, frame.Length);
                    client.Stream.Flush();
                }
            }
            catch
            {
                // Le client est parti : la boucle de lecture s'en apercevra.
            }
        }

        static bool TryReadRequest(Stream stream, out string path, out Dictionary<string, string> headers)
        {
            path = null;
            headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var head = new MemoryStream();
            int matched = 0;
            byte[] one = new byte[1];
            while (matched < 4)
            {
                if (head.Length >= MaxRequestHeadBytes || stream.Read(one, 0, 1) <= 0)
                {
                    return false;
                }

                head.WriteByte(one[0]);
                bool expectCarriageReturn = matched == 0 || matched == 2;
                if (one[0] == (expectCarriageReturn ? (byte)'\r' : (byte)'\n'))
                {
                    matched++;
                }
                else
                {
                    matched = one[0] == (byte)'\r' ? 1 : 0;
                }
            }

            string[] lines = Encoding.ASCII.GetString(head.ToArray()).Split(new[] { "\r\n" }, StringSplitOptions.None);
            string[] requestLine = lines[0].Split(' ');
            if (requestLine.Length < 2 || requestLine[0] != "GET")
            {
                return false;
            }

            path = requestLine[1];
            int query = path.IndexOf('?');
            if (query >= 0)
            {
                path = path.Substring(0, query);
            }

            for (int i = 1; i < lines.Length; i++)
            {
                int colon = lines[i].IndexOf(':');
                if (colon > 0)
                {
                    headers[lines[i].Substring(0, colon).Trim()] = lines[i].Substring(colon + 1).Trim();
                }
            }

            return true;
        }

        static void WriteResponse(Stream stream, string status, string contentType, byte[] body)
        {
            string head =
                "HTTP/1.1 " + status + "\r\n" +
                "Content-Type: " + contentType + "\r\n" +
                "Content-Length: " + body.Length + "\r\n" +
                "Cache-Control: no-store\r\n" +
                "Connection: close\r\n\r\n";
            byte[] headBytes = Encoding.ASCII.GetBytes(head);
            stream.Write(headBytes, 0, headBytes.Length);
            stream.Write(body, 0, body.Length);
            stream.Flush();
        }
    }
}
