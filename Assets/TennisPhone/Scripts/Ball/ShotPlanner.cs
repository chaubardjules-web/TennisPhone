using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Traduit un swing en frappe : le timing donne la direction, la puissance donne la vitesse
    /// et la profondeur, l'effet donne la rotation de la balle.
    /// </summary>
    public static class ShotPlanner
    {
        /// <summary>Paramètres d'une frappe, avant le calcul de trajectoire.</summary>
        public struct Plan
        {
            public Vector3 Target;
            public float Speed;

            /// <summary>Rotation en rad/s : positive pour un lift, négative pour un slice.</summary>
            public float SpinRate;

            public float NetMargin;
        }

        /// <param name="timing">Écart au timing idéal, de -1 (très tôt) à +1 (très tard).</param>
        /// <param name="towardSide">Côté visé : +1 pour envoyer la balle vers z positif, -1 vers z négatif.</param>
        public static Plan Build(SwingEvent swing, float timing, int towardSide, GameplayConfig config)
        {
            float power = Mathf.Clamp01(swing.Power);
            float lift = Mathf.Clamp01(swing.Spin);
            float slice = Mathf.Clamp01(-swing.Spin);

            // Tôt : la balle part du côté opposé à la raquette (croisé).
            // Tard : elle part du côté de la raquette (long de ligne).
            float racketSide = (swing.Side == SwingSide.Forehand ? 1f : -1f)
                * (config.handedness == Handedness.Right ? 1f : -1f);
            float lateral = Mathf.Clamp(timing, -1f, 1f) * racketSide * config.aimMaxOffset;

            // La droite d'un joueur qui regarde vers z positif est x positif ;
            // pour le joueur d'en face, c'est l'inverse.
            float targetX = lateral * towardSide;
            float depth = Mathf.Lerp(config.shotDepthMin, config.shotDepthMax, power);

            float speedFactor = Mathf.Lerp(1f, config.liftSpeedFactor, lift) * Mathf.Lerp(1f, config.sliceSpeedFactor, slice);
            float rpm = lift * config.liftRpmMax - slice * config.sliceRpmMax;

            return new Plan
            {
                Target = new Vector3(targetX, 0f, depth * towardSide),
                Speed = Mathf.Lerp(config.shotSpeedMin, config.shotSpeedMax, power) * speedFactor,
                SpinRate = rpm * 2f * Mathf.PI / 60f,
                NetMargin = config.shotNetMargin + lift * config.liftNetMarginBonus,
            };
        }
    }
}
