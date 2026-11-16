using System;

namespace TennisPhone
{
    /// <summary>Ce qu'un point vient de conclure.</summary>
    public enum ScoreEvent
    {
        Point,
        Game,
        Set,
    }

    /// <summary>
    /// Score d'un set de tennis entre deux camps (0 et 1) : points 15, 30, 40, égalité, avantage,
    /// puis jeux. Le service change de camp à chaque jeu.
    /// Classe sans dépendance à Unity.
    /// </summary>
    public sealed class MatchScore
    {
        static readonly string[] PointNames = { "0", "15", "30", "40" };

        readonly int[] points = new int[2];
        readonly int[] games = new int[2];
        readonly int gamesToWin;
        readonly bool twoGameLead;

        /// <param name="gamesToWin">Nombre de jeux à gagner pour remporter le set.</param>
        /// <param name="twoGameLead">Si vrai, il faut aussi deux jeux d'écart.</param>
        /// <param name="firstServer">Camp qui sert le premier jeu (0 ou 1).</param>
        public MatchScore(int gamesToWin, bool twoGameLead, int firstServer)
        {
            this.gamesToWin = Math.Max(1, gamesToWin);
            this.twoGameLead = twoGameLead;
            Server = firstServer == 1 ? 1 : 0;
            Winner = -1;
        }

        /// <summary>Camp qui sert le jeu en cours.</summary>
        public int Server { get; private set; }

        /// <summary>Camp qui a gagné le set, ou -1 s'il est en cours.</summary>
        public int Winner { get; private set; }

        public bool IsOver => Winner >= 0;

        public int Games(int side)
        {
            return games[side];
        }

        /// <summary>Nombre de points marqués dans le jeu en cours.</summary>
        public int Points(int side)
        {
            return points[side];
        }

        /// <summary>Vrai à 40 partout ou au-delà, à égalité de points.</summary>
        public bool IsDeuce => points[0] >= 3 && points[0] == points[1];

        /// <summary>Camp qui a l'avantage, ou -1.</summary>
        public int Advantage
        {
            get
            {
                if (points[0] < 3 || points[1] < 3 || points[0] == points[1])
                {
                    return -1;
                }

                return points[0] > points[1] ? 0 : 1;
            }
        }

        /// <summary>
        /// Côté d'où part le service : 0 pour la droite du serveur (total de points pair),
        /// 1 pour sa gauche.
        /// </summary>
        public int ServeCourt => (points[0] + points[1]) % 2;

        /// <summary>Score d'un camp dans le jeu en cours : « 0 », « 15 », « 30 », « 40 » ou « Av. ».</summary>
        public string PointText(int side)
        {
            int other = 1 - side;
            if (points[side] >= 3 && points[other] >= 3)
            {
                return points[side] > points[other] ? "Av." : "40";
            }

            return PointNames[Math.Min(points[side], 3)];
        }

        /// <summary>Annonce du score, serveur en premier : « 30 - 15 », « Égalité », « Avantage service ».</summary>
        public string Call()
        {
            if (IsDeuce)
            {
                return "Égalité";
            }

            int advantage = Advantage;
            if (advantage >= 0)
            {
                return advantage == Server ? "Avantage service" : "Avantage retour";
            }

            return PointText(Server) + " - " + PointText(1 - Server);
        }

        /// <summary>Enregistre un point pour ce camp et renvoie ce qu'il conclut.</summary>
        public ScoreEvent PointWonBy(int side)
        {
            if (IsOver)
            {
                return ScoreEvent.Set;
            }

            int other = 1 - side;
            points[side]++;

            bool gameWon = points[side] >= 4 && points[side] - points[other] >= 2;
            if (!gameWon)
            {
                return ScoreEvent.Point;
            }

            points[0] = 0;
            points[1] = 0;
            games[side]++;
            Server = 1 - Server;

            bool setWon = games[side] >= gamesToWin && (!twoGameLead || games[side] - games[other] >= 2);
            if (setWon)
            {
                Winner = side;
                return ScoreEvent.Set;
            }

            return ScoreEvent.Game;
        }
    }
}
