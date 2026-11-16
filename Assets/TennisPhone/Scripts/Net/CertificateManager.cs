using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace TennisPhone
{
    /// <summary>
    /// Crée et charge le certificat local qui permet au téléphone d'ouvrir la page en HTTPS.
    /// Une autorité locale est créée une seule fois (c'est elle qu'on installe sur le téléphone).
    /// Le certificat serveur est régénéré quand les adresses IP du PC changent.
    /// Les fichiers sont dans le dossier Certs, à la racine du projet, jamais versionné.
    /// </summary>
    public static class CertificateManager
    {
        public const string AuthorityName = "TennisPhone Local CA";

        const string PfxPassword = "tennisphone";
        const int AuthorityDays = 3650;
        const int ServerDays = 397;
        const int RenewBeforeDays = 7;

        public sealed class Result
        {
            public X509Certificate2 ServerCertificate;
            public byte[] AuthorityCertificateDer;
            public string Error;
            public bool Regenerated;

            public bool Success => Error == null;
        }

        /// <summary>
        /// Garantit qu'un certificat serveur valide pour ces adresses IP existe, puis le charge.
        /// </summary>
        /// <param name="certDirectory">Dossier des certificats (créé si besoin).</param>
        /// <param name="ipAddresses">Adresses IPv4 du PC sur le réseau local.</param>
        public static Result Ensure(string certDirectory, IList<string> ipAddresses)
        {
            var result = new Result();
            try
            {
                Directory.CreateDirectory(certDirectory);

                string caKey = Path.Combine(certDirectory, "ca.key");
                string caCrt = Path.Combine(certDirectory, "ca.crt");
                string serverPfx = Path.Combine(certDirectory, "server.pfx");
                string stampPath = Path.Combine(certDirectory, "server.stamp.txt");

                var sortedIps = ipAddresses.Distinct().OrderBy(ip => ip, StringComparer.Ordinal).ToList();
                string stamp = string.Join(",", sortedIps);

                bool authorityMissing = !File.Exists(caKey) || !File.Exists(caCrt);
                bool serverStale = authorityMissing
                    || !File.Exists(serverPfx)
                    || !File.Exists(stampPath)
                    || File.ReadAllText(stampPath).Trim() != stamp
                    || IsExpiringSoon(serverPfx);

                if (authorityMissing || serverStale)
                {
                    string openssl = FindOpenSsl();
                    if (openssl == null)
                    {
                        result.Error = "OpenSSL est introuvable. Installe Git pour Windows (il fournit openssl.exe) "
                            + "ou ajoute openssl au PATH.";
                        return result;
                    }

                    if (authorityMissing)
                    {
                        CreateAuthority(openssl, certDirectory);
                    }

                    CreateServerCertificate(openssl, certDirectory, sortedIps);
                    File.WriteAllText(stampPath, stamp);
                    result.Regenerated = true;
                }

                result.ServerCertificate = new X509Certificate2(File.ReadAllBytes(serverPfx), PfxPassword);
                result.AuthorityCertificateDer = PemToDer(File.ReadAllText(caCrt));
                if (!result.ServerCertificate.HasPrivateKey)
                {
                    result.Error = "Le certificat serveur a été chargé sans sa clé privée.";
                }
            }
            catch (Exception exception)
            {
                result.Error = "Certificat local : " + exception.Message;
            }

            return result;
        }

        static bool IsExpiringSoon(string pfxPath)
        {
            try
            {
                using (var certificate = new X509Certificate2(File.ReadAllBytes(pfxPath), PfxPassword))
                {
                    return certificate.NotAfter < DateTime.Now.AddDays(RenewBeforeDays);
                }
            }
            catch
            {
                return true;
            }
        }

        static void CreateAuthority(string openssl, string directory)
        {
            File.WriteAllText(Path.Combine(directory, "ca.cnf"),
                "[req]\n" +
                "distinguished_name = dn\n" +
                "prompt = no\n" +
                "x509_extensions = v3_ca\n" +
                "[dn]\n" +
                "CN = " + AuthorityName + "\n" +
                "O = TennisPhone\n" +
                "[v3_ca]\n" +
                "basicConstraints = critical,CA:TRUE\n" +
                "keyUsage = critical,keyCertSign,cRLSign\n" +
                "subjectKeyIdentifier = hash\n");

            Run(openssl, directory,
                "req -x509 -newkey rsa:2048 -nodes -sha256 -days " + AuthorityDays
                + " -keyout ca.key -out ca.crt -config ca.cnf");
        }

        static void CreateServerCertificate(string openssl, string directory, IList<string> ipAddresses)
        {
            File.WriteAllText(Path.Combine(directory, "server.cnf"),
                "[req]\n" +
                "distinguished_name = dn\n" +
                "prompt = no\n" +
                "[dn]\n" +
                "CN = TennisPhone\n" +
                "O = TennisPhone\n");

            var names = new List<string>();
            foreach (string ip in ipAddresses)
            {
                names.Add("IP:" + ip);
            }

            names.Add("IP:127.0.0.1");
            names.Add("DNS:localhost");

            File.WriteAllText(Path.Combine(directory, "server.ext"),
                "basicConstraints = CA:FALSE\n" +
                "keyUsage = critical,digitalSignature,keyEncipherment\n" +
                "extendedKeyUsage = serverAuth\n" +
                "subjectAltName = " + string.Join(",", names) + "\n");

            Run(openssl, directory,
                "req -newkey rsa:2048 -nodes -sha256 -keyout server.key -out server.csr -config server.cnf");
            Run(openssl, directory,
                "x509 -req -in server.csr -CA ca.crt -CAkey ca.key -CAcreateserial -out server.crt -days "
                + ServerDays + " -sha256 -extfile server.ext");

            // Chiffrement 3DES et empreinte SHA-1 : le format PKCS#12 le plus largement lisible.
            Run(openssl, directory,
                "pkcs12 -export -out server.pfx -inkey server.key -in server.crt -certfile ca.crt"
                + " -passout pass:" + PfxPassword
                + " -keypbe PBE-SHA1-3DES -certpbe PBE-SHA1-3DES -macalg sha1");
        }

        static void Run(string executable, string workingDirectory, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using (Process process = Process.Start(startInfo))
            {
                string errors = process.StandardError.ReadToEnd();
                process.StandardOutput.ReadToEnd();
                if (!process.WaitForExit(30000))
                {
                    process.Kill();
                    throw new InvalidOperationException("OpenSSL ne répond pas.");
                }

                if (process.ExitCode != 0)
                {
                    string firstWord = arguments.Split(' ')[0];
                    throw new InvalidOperationException("openssl " + firstWord + " a échoué : " + errors.Trim());
                }
            }
        }

        static string FindOpenSsl()
        {
            bool windows = Path.DirectorySeparatorChar == '\\';
            string fileName = windows ? "openssl.exe" : "openssl";

            var candidates = new List<string>();
            string pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (string directory in pathVariable.Split(Path.PathSeparator))
            {
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    candidates.Add(Path.Combine(directory.Trim(), fileName));
                }
            }

            if (windows)
            {
                candidates.Add(@"C:\Program Files\Git\mingw64\bin\openssl.exe");
                candidates.Add(@"C:\Program Files\Git\usr\bin\openssl.exe");
                candidates.Add(@"C:\Program Files\OpenSSL-Win64\bin\openssl.exe");
            }
            else
            {
                candidates.Add("/usr/bin/openssl");
                candidates.Add("/opt/homebrew/bin/openssl");
                candidates.Add("/usr/local/bin/openssl");
            }

            foreach (string candidate in candidates)
            {
                try
                {
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
                catch
                {
                    // Chemin invalide dans le PATH : on l'ignore.
                }
            }

            return null;
        }

        static byte[] PemToDer(string pem)
        {
            var base64 = new StringBuilder();
            foreach (string line in pem.Split('\n'))
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("-----"))
                {
                    continue;
                }

                base64.Append(trimmed);
            }

            return Convert.FromBase64String(base64.ToString());
        }
    }
}
