using System.Globalization;
using System.Text;
using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Télécommande du match sur le téléphone : exécute les boutons de la page manette (lancer la
    /// balle, recentrer, pause, niveau, rejouer un set, caméra) et renvoie au téléphone l'état du
    /// match et les annonces, pour qu'il affiche le score et le bon bouton au bon moment.
    /// </summary>
    [RequireComponent(typeof(PhoneLink), typeof(RallyController), typeof(RacketInput))]
    public sealed class PhoneRemote : MonoBehaviour
    {
        /// <summary>L'état est renvoyé au moins à cette cadence (s), même sans changement.</summary>
        const float ResendInterval = 0.5f;

        PhoneLink link;
        RallyController rally;
        RacketInput input;

        readonly StringBuilder builder = new StringBuilder(96);
        string lastState = string.Empty;
        float lastStateTime = -10f;
        int lastClientCount;

        // Annonces de l'image en cours : la fin d'un point en produit plusieurs, envoyées en une seule.
        bool hasAnnouncement;
        Announcement firstAnnouncement;
        bool hasPoint;
        PointInfo point;
        string whiff;

        void Awake()
        {
            link = GetComponent<PhoneLink>();
            rally = GetComponent<RallyController>();
            input = GetComponent<RacketInput>();
        }

        void OnEnable()
        {
            link.CommandReceived += OnCommand;
            rally.Announced += OnAnnounced;
            rally.PointEnded += OnPointEnded;
            rally.Whiffed += OnWhiffed;
        }

        void OnDisable()
        {
            link.CommandReceived -= OnCommand;
            rally.Announced -= OnAnnounced;
            rally.PointEnded -= OnPointEnded;
            rally.Whiffed -= OnWhiffed;
        }

        void OnCommand(string command)
        {
            switch (command)
            {
                case "toss":
                    rally.RequestToss(rally.Config.tossDefaultStrength);
                    break;
                case "recenter":
                    input.Recenter();
                    break;
                case "pause":
                    rally.Paused = !rally.Paused;
                    break;
                case "restart":
                    rally.RestartSet();
                    break;
                case "level":
                    rally.CycleDifficulty();
                    break;
                case "camera":
                    rally.WebcamMovement = !rally.WebcamMovement;
                    break;
            }

            // Le téléphone voit tout de suite l'effet de son bouton.
            lastStateTime = -10f;
        }

        void OnAnnounced(Announcement announcement)
        {
            if (!hasAnnouncement)
            {
                hasAnnouncement = true;
                firstAnnouncement = announcement;
            }
        }

        void OnPointEnded(PointInfo info)
        {
            hasPoint = true;
            point = info;
        }

        void OnWhiffed(string detail)
        {
            whiff = detail;
        }

        void LateUpdate()
        {
            if (rally.Score == null)
            {
                return;
            }

            int clients = link.ClientCount;
            bool newClient = clients > lastClientCount;
            lastClientCount = clients;

            if (clients > 0)
            {
                SendAnnouncements();

                string state = BuildState();
                float now = Time.unscaledTime;
                if (newClient || state != lastState || now - lastStateTime >= ResendInterval)
                {
                    lastState = state;
                    lastStateTime = now;
                    link.Send(state);
                }
            }

            hasAnnouncement = false;
            hasPoint = false;
            whiff = null;
        }

        /// <summary>
        /// st|phase|service|prêt|pause|niveau|jeux toi|jeux adv.|points toi|points adv.|serveur|fautes|caméra
        /// Caméra : 0 éteinte, 1 elle déplace le joueur, 2 allumée mais elle ne voit personne bouger.
        /// </summary>
        string BuildState()
        {
            MatchScore score = rally.Score;
            int camera = !rally.WebcamMovement ? 0 : (rally.WebcamActive ? 1 : 2);

            builder.Length = 0;
            builder.Append("st|").Append(rally.CurrentPhase.ToString())
                .Append('|').Append(rally.PlayerServing ? '1' : '0')
                .Append('|').Append(rally.PlayerReadyToToss ? '1' : '0')
                .Append('|').Append(rally.Paused ? '1' : '0')
                .Append('|').Append(rally.Difficulty.ToString())
                .Append('|').Append(score.Games(0).ToString(CultureInfo.InvariantCulture))
                .Append('|').Append(score.Games(1).ToString(CultureInfo.InvariantCulture))
                .Append('|').Append(score.PointText(0))
                .Append('|').Append(score.PointText(1))
                .Append('|').Append(score.Server.ToString(CultureInfo.InvariantCulture))
                .Append('|').Append(rally.FaultCount.ToString(CultureInfo.InvariantCulture))
                .Append('|').Append(camera.ToString(CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        /// <summary>o|genre|titre|détail : une ligne par événement (fin de point, faute, consigne, swing dans le vide).</summary>
        void SendAnnouncements()
        {
            if (hasPoint)
            {
                bool won = point.WonByPlayer;
                string title;
                switch (point.Result)
                {
                    case ScoreEvent.Set:
                        title = won ? "Set gagné" : "Set perdu";
                        break;
                    case ScoreEvent.Game:
                        title = won ? "Jeu pour toi" : "Jeu pour l'adversaire";
                        break;
                    default:
                        title = won ? "Point pour toi" : "Point perdu";
                        break;
                }

                // Ce qui s'est passé : « Dehors · Point perdu · Ta balle est dehors de 0,4 m ».
                string detail = string.Empty;
                if (hasAnnouncement)
                {
                    bool plain = firstAnnouncement.Title.StartsWith("POINT");
                    detail = plain
                        ? firstAnnouncement.Subtitle
                        : Capitalize(firstAnnouncement.Title) + " · " + firstAnnouncement.Subtitle;
                }

                SendOutcome(won ? "won" : "lost", title, detail);
            }
            else if (hasAnnouncement && firstAnnouncement.Title != "PAUSE" && firstAnnouncement.Title != "REPRISE")
            {
                // La pause se lit dans l'état du match, pas dans une annonce.
                string kind = firstAnnouncement.Tone == MessageTone.Bad ? "fault"
                    : (firstAnnouncement.Tone == MessageTone.Good ? "won" : "info");
                SendOutcome(kind, Capitalize(firstAnnouncement.Title), firstAnnouncement.Subtitle);
            }
            else if (whiff != null)
            {
                SendOutcome("info", "Dans le vide", whiff);
            }
        }

        void SendOutcome(string kind, string title, string detail)
        {
            link.Send("o|" + kind + "|" + Clean(title) + "|" + Clean(detail));
        }

        /// <summary>« DOUBLE FAUTE » devient « Double faute » : le téléphone écrit ses titres en minuscules.</summary>
        static string Capitalize(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            CultureInfo french = CultureInfo.GetCultureInfo("fr-FR");
            return char.ToUpper(text[0], french) + text.Substring(1).ToLower(french);
        }

        /// <summary>Le séparateur du protocole et les retours à la ligne ne doivent pas apparaître dans un texte.</summary>
        static string Clean(string text)
        {
            return string.IsNullOrEmpty(text) ? string.Empty : text.Replace('|', '/').Replace('\n', ' ').Replace('\r', ' ');
        }
    }
}
