using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Dimensions du court, en mètres. Le filet est sur le plan z = 0, le joueur du côté z négatif,
    /// l'adversaire du côté z positif. L'axe x est la largeur, l'axe y la hauteur.
    /// </summary>
    public struct CourtDimensions
    {
        /// <summary>Distance du filet à la ligne de fond.</summary>
        public float HalfLength;

        /// <summary>Distance de l'axe du court à la ligne de côté (simple).</summary>
        public float HalfWidth;

        /// <summary>Distance du filet à la ligne de service.</summary>
        public float ServiceLine;

        public float NetHeightCenter;
        public float NetHeightPosts;

        /// <summary>Distance de l'axe du court au poteau du filet.</summary>
        public float NetHalfWidth;

        /// <summary>Hauteur du filet à cette distance de l'axe : il est plus bas au centre.</summary>
        public float NetHeightAt(float x)
        {
            float ratio = NetHalfWidth > 0f ? Mathf.Clamp01(Mathf.Abs(x) / NetHalfWidth) : 0f;
            return Mathf.Lerp(NetHeightCenter, NetHeightPosts, ratio);
        }

        /// <summary>Vrai si le point est dans le court de simple. Les lignes font partie du court.</summary>
        public bool IsInside(float x, float z, float tolerance = 0f)
        {
            return Mathf.Abs(x) <= HalfWidth + tolerance && Mathf.Abs(z) <= HalfLength + tolerance;
        }

        /// <summary>
        /// Vrai si le point est dans le carré de service visé.
        /// </summary>
        /// <param name="receiverSide">Côté du relanceur : +1 pour z positif, -1 pour z négatif.</param>
        /// <param name="boxSide">Moitié du court visée : +1 pour x positif, -1 pour x négatif.</param>
        public bool IsInServiceBox(float x, float z, int receiverSide, int boxSide, float tolerance = 0f)
        {
            float depth = z * receiverSide;
            float lateral = x * boxSide;
            return depth >= -tolerance && depth <= ServiceLine + tolerance
                && lateral >= -tolerance && lateral <= HalfWidth + tolerance;
        }
    }
}
