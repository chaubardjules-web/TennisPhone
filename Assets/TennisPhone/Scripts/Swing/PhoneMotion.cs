using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Transforme une mesure brute des capteurs en grandeurs de mouvement pour le détecteur.
    /// </summary>
    public static class PhoneMotion
    {
        public static MotionFrame ToFrame(SensorSample sample)
        {
            // Vitesse angulaire dans le repère du téléphone (x droite, y haut de l'écran, z sortant).
            // Le navigateur donne alpha autour de z, beta autour de x, gamma autour de y.
            var angularVelocity = new Vector3(sample.RotationRate.y, sample.RotationRate.z, sample.RotationRate.x);

            // Direction « vers le haut » exprimée dans le repère du téléphone : troisième ligne
            // de la matrice d'orientation du W3C. Elle ne dépend que de beta et gamma,
            // donc pas de la boussole.
            float beta = sample.Orientation.y * Mathf.Deg2Rad;
            float gamma = sample.Orientation.z * Mathf.Deg2Rad;
            var up = new Vector3(
                -Mathf.Cos(beta) * Mathf.Sin(gamma),
                Mathf.Sin(beta),
                Mathf.Cos(beta) * Mathf.Cos(gamma));

            return new MotionFrame
            {
                TimeSeconds = sample.PhoneTimeMs / 1000.0,
                Acceleration = sample.Acceleration.magnitude,
                RotationSpeed = sample.RotationRate.magnitude,
                YawRate = Vector3.Dot(angularVelocity, up),
                RollRate = angularVelocity.y,
            };
        }
    }
}
