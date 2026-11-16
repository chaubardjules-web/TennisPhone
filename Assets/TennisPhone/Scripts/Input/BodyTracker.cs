using System;

namespace TennisPhone
{
    /// <summary>Réglages du suivi par webcam, copiés depuis GameplayConfig.</summary>
    public struct BodyTrackerSettings
    {
        /// <summary>Écart de luminosité (0 à 255) entre deux images pour qu'un point compte comme « en mouvement ».</summary>
        public float MotionThreshold;

        /// <summary>Durée (s) pendant laquelle un point qui a bougé reste pris en compte.</summary>
        public float MemorySeconds;

        /// <summary>
        /// Part minimale de l'image qui doit avoir bougé récemment pour mettre la position à jour.
        /// Assez haute pour qu'un bras seul ne compte pas, assez basse pour qu'un pas de côté compte.
        /// </summary>
        public float MinCoverage;

        /// <summary>Part de l'image au-delà de laquelle on ignore l'image (changement d'éclairage, caméra bousculée).</summary>
        public float MaxCoverage;

        /// <summary>Temps de lissage (s) de la position.</summary>
        public float SmoothingSeconds;

        /// <summary>Durée (s) sans mouvement au bout de laquelle le joueur est considéré comme perdu.</summary>
        public float HoldSeconds;
    }

    /// <summary>
    /// Suit la position latérale d'une personne devant une caméra fixe, sans bibliothèque externe.
    /// Principe : on compare chaque image à la précédente ; les points qui changent sont ceux du
    /// corps en mouvement. On garde un court souvenir de ces points et on prend le milieu de la
    /// zone qu'ils occupent en largeur. Seul un mouvement de la taille d'un corps compte : une main
    /// qui bouge ne suffit pas. Quand la personne s'immobilise, sa dernière position est conservée.
    /// Un bras qui frappe bouge beaucoup sans que le corps se déplace : l'appelant « gèle » le
    /// suivi pendant le swing, que le téléphone signale.
    /// Travaille sur une petite grille de luminosité. Classe sans dépendance à Unity.
    /// </summary>
    public sealed class BodyTracker
    {
        readonly int width;
        readonly int height;
        readonly byte[] previous;
        readonly float[] memory;
        readonly float[] columns;
        bool hasPrevious;
        double lastSeenTime = double.NegativeInfinity;

        public BodyTracker(int width, int height)
        {
            this.width = Math.Max(2, width);
            this.height = Math.Max(1, height);
            previous = new byte[this.width * this.height];
            memory = new float[this.width * this.height];
            columns = new float[this.width];
        }

        public int Width => width;

        public int Height => height;

        /// <summary>Position en largeur dans l'image : -1 au bord gauche, +1 au bord droit.</summary>
        public float X { get; private set; }

        /// <summary>Vrai tant que la personne a été vue récemment.</summary>
        public bool HasTarget { get; private set; }

        /// <summary>Part de l'image actuellement en mouvement (0 à 1).</summary>
        public float Coverage { get; private set; }

        /// <summary>Souvenir du mouvement par point (0 à 1), pour l'aperçu à l'écran.</summary>
        public float[] Memory => memory;

        public void Reset()
        {
            hasPrevious = false;
            HasTarget = false;
            Array.Clear(memory, 0, memory.Length);
            lastSeenTime = double.NegativeInfinity;
        }

        /// <summary>
        /// Traite une image.
        /// </summary>
        /// <param name="luma">Luminosité de chaque point de la grille (largeur × hauteur), ligne par ligne.</param>
        /// <param name="time">Horloge, en secondes.</param>
        /// <param name="deltaTime">Temps écoulé depuis l'image précédente, en secondes.</param>
        /// <param name="frozen">Vrai pendant un swing : la position est conservée, le mouvement vu est ignoré.</param>
        public void Process(byte[] luma, double time, float deltaTime, bool frozen, in BodyTrackerSettings settings)
        {
            if (luma == null || luma.Length < previous.Length)
            {
                return;
            }

            if (frozen)
            {
                // On suit l'image pour repartir d'une base à jour, sans rien retenir du geste.
                Buffer.BlockCopy(luma, 0, previous, 0, previous.Length);
                hasPrevious = true;
                Array.Clear(memory, 0, memory.Length);
                Coverage = 0f;
                if (HasTarget)
                {
                    lastSeenTime = time;
                }

                return;
            }

            if (!hasPrevious)
            {
                Buffer.BlockCopy(luma, 0, previous, 0, previous.Length);
                hasPrevious = true;
                return;
            }

            float fade = settings.MemorySeconds > 0f ? deltaTime / settings.MemorySeconds : 1f;
            Array.Clear(columns, 0, columns.Length);
            int moving = 0;
            float total = 0f;

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    int index = row + x;
                    int difference = luma[index] - previous[index];
                    if (difference < 0)
                    {
                        difference = -difference;
                    }

                    previous[index] = luma[index];

                    float value;
                    if (difference > settings.MotionThreshold)
                    {
                        value = 1f;
                        moving++;
                    }
                    else
                    {
                        value = memory[index] - fade;
                        if (value < 0f)
                        {
                            value = 0f;
                        }
                    }

                    memory[index] = value;
                    columns[x] += value;
                    total += value;
                }
            }

            Coverage = (float)moving / previous.Length;

            // Toute l'image change d'un coup : éclairage ou caméra, pas une personne.
            if (Coverage > settings.MaxCoverage)
            {
                Array.Clear(memory, 0, memory.Length);
                return;
            }

            float remembered = total / previous.Length;
            if (remembered >= settings.MinCoverage && total > 0f)
            {
                // Milieu entre le premier et le dernier quart du mouvement : un corps qui se déplace
                // bouge surtout par ses deux bords, et ce milieu tombe entre eux, sur le corps.
                float middle = (ColumnAt(total * 0.25f) + ColumnAt(total * 0.75f)) * 0.5f;
                float target = middle / (width - 1) * 2f - 1f;
                if (HasTarget && settings.SmoothingSeconds > 0f)
                {
                    float blend = 1f - (float)Math.Exp(-deltaTime / settings.SmoothingSeconds);
                    X += (target - X) * blend;
                }
                else
                {
                    X = target;
                }

                HasTarget = true;
                lastSeenTime = time;
            }
            else if (HasTarget && time - lastSeenTime > settings.HoldSeconds)
            {
                HasTarget = false;
            }
        }

        /// <summary>
        /// Position (en colonnes, avec décimales) en dessous de laquelle se trouve cette quantité
        /// de mouvement, en comptant depuis le bord gauche.
        /// </summary>
        float ColumnAt(float amount)
        {
            float cumulative = 0f;
            for (int x = 0; x < width; x++)
            {
                float next = cumulative + columns[x];
                if (next >= amount)
                {
                    // Position précise à l'intérieur de la colonne.
                    float inside = columns[x] > 0f ? (amount - cumulative) / columns[x] : 0.5f;
                    return x - 0.5f + inside;
                }

                cumulative = next;
            }

            return width - 1;
        }
    }
}
