using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Fait le lien entre le serveur du téléphone et le jeu : démarre le serveur,
    /// puis redistribue les mesures des capteurs sur le thread principal.
    /// </summary>
    public sealed class PhoneLink : MonoBehaviour
    {
        [Header("Réseau")]
        [Tooltip("Port HTTPS : page manette et WebSocket.")]
        [SerializeField] int httpsPort = 8443;

        [Tooltip("Port HTTP : page d'installation du certificat sur le téléphone.")]
        [SerializeField] int httpPort = 8081;

        PhoneServer server;
        readonly List<string> addresses = new List<string>();
        int samplesThisWindow;
        float windowStart;

        /// <summary>Appelé sur le thread principal pour chaque mesure reçue.</summary>
        public event Action<SensorSample> SampleReceived;

        public bool IsServerRunning => server != null && server.IsRunning;

        /// <summary>Message d'erreur à afficher si le serveur n'a pas pu démarrer.</summary>
        public string Error { get; private set; }

        public IReadOnlyList<string> Addresses => addresses;

        /// <summary>Adresse de la page manette (HTTPS).</summary>
        public string ControllerUrl { get; private set; }

        /// <summary>Adresse de la page d'installation du certificat (HTTP).</summary>
        public string OnboardingUrl { get; private set; }

        public int ClientCount => server != null ? server.ClientCount : 0;

        public bool HasSample { get; private set; }

        public SensorSample Latest { get; private set; }

        /// <summary>Nombre de mesures reçues par seconde, sur la dernière seconde.</summary>
        public float SampleRateHz { get; private set; }

        public float RoundTripMs => server != null ? server.RoundTripMs : -1f;

        public long TlsFailures => server != null ? server.TlsFailures : 0;

        public long TotalSamples => server != null ? server.TotalSamples : 0;

        /// <summary>Dernier message du serveur (connexion, déconnexion, erreur).</summary>
        public string LastMessage { get; private set; }

        void OnEnable()
        {
            StartServer();
        }

        void OnDisable()
        {
            StopServer();
        }

        void StartServer()
        {
            Error = null;
            addresses.Clear();
            addresses.AddRange(LanAddress.GetLanIPv4());

            if (addresses.Count == 0)
            {
                Error = "Aucune adresse réseau locale trouvée. Connecte le PC au Wi-Fi.";
                return;
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            CertificateManager.Result certificate =
                CertificateManager.Ensure(Path.Combine(projectRoot, "Certs"), addresses);
            if (!certificate.Success)
            {
                Error = certificate.Error;
                return;
            }

            string host = addresses[0];
            ControllerUrl = "https://" + host + ":" + httpsPort.ToString(CultureInfo.InvariantCulture) + "/";
            OnboardingUrl = "http://" + host + ":" + httpPort.ToString(CultureInfo.InvariantCulture) + "/";

            TextAsset controller = Resources.Load<TextAsset>("controller");
            TextAsset onboarding = Resources.Load<TextAsset>("onboarding");
            if (controller == null || onboarding == null)
            {
                Error = "Pages web introuvables dans Resources (controller, onboarding).";
                return;
            }

            string onboardingHtml = onboarding.text
                .Replace("{{CONTROLLER_URL}}", ControllerUrl)
                .Replace("{{AUTHORITY_NAME}}", CertificateManager.AuthorityName);

            try
            {
                server = new PhoneServer(httpsPort, httpPort, certificate.ServerCertificate,
                    certificate.AuthorityCertificateDer, controller.text, onboardingHtml);
                server.Start();
                Debug.Log("[TennisPhone] Serveur démarré. Manette : " + ControllerUrl
                    + " | Certificat : " + OnboardingUrl
                    + (certificate.Regenerated ? " | certificat serveur régénéré" : string.Empty));
            }
            catch (Exception exception)
            {
                server?.Dispose();
                server = null;
                Error = "Le serveur n'a pas pu démarrer (ports " + httpsPort + " et " + httpPort + ") : "
                    + exception.Message;
            }

            if (Error != null)
            {
                Debug.LogError("[TennisPhone] " + Error);
            }
        }

        void StopServer()
        {
            if (server != null)
            {
                server.Dispose();
                server = null;
            }

            HasSample = false;
            SampleRateHz = 0f;
        }

        void Update()
        {
            if (server == null)
            {
                return;
            }

            while (server.Messages.TryDequeue(out string message))
            {
                LastMessage = message;
                Debug.Log("[TennisPhone] " + message);
            }

            while (server.Samples.TryDequeue(out SensorSample sample))
            {
                Latest = sample;
                HasSample = true;
                samplesThisWindow++;
                SampleReceived?.Invoke(sample);
            }

            float now = Time.unscaledTime;
            if (now - windowStart >= 1f)
            {
                SampleRateHz = samplesThisWindow / (now - windowStart);
                samplesThisWindow = 0;
                windowStart = now;
                if (ClientCount == 0)
                {
                    HasSample = false;
                }
            }
        }

        /// <summary>Demande au téléphone un retour haptique.</summary>
        public void SendVibration(int milliseconds)
        {
            server?.Broadcast("v," + Mathf.Clamp(milliseconds, 1, 1000).ToString(CultureInfo.InvariantCulture));
        }
    }
}
