using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Conversion de l'orientation du navigateur (angles alpha, beta, gamma de DeviceOrientation)
    /// vers une rotation Unity.
    /// </summary>
    public static class DeviceOrientationMath
    {
        /// <summary>
        /// Rotation Unity d'un objet dont l'axe local Z est le haut du téléphone,
        /// l'axe local Y la normale de l'écran et l'axe local X la droite du téléphone.
        /// Téléphone à plat, haut vers le nord : rotation identité.
        /// </summary>
        /// <param name="orientationDegrees">x = alpha, y = beta, z = gamma, en degrés.</param>
        public static Quaternion ToUnityRotation(Vector3 orientationDegrees)
        {
            float alpha = orientationDegrees.x * Mathf.Deg2Rad;
            float beta = orientationDegrees.y * Mathf.Deg2Rad;
            float gamma = orientationDegrees.z * Mathf.Deg2Rad;

            float cA = Mathf.Cos(alpha), sA = Mathf.Sin(alpha);
            float cB = Mathf.Cos(beta), sB = Mathf.Sin(beta);
            float cG = Mathf.Cos(gamma), sG = Mathf.Sin(gamma);

            // Matrice du W3C R = Rz(alpha) · Rx(beta) · Ry(gamma) dans le repère est-nord-haut,
            // dont on prend les colonnes 2 (haut du téléphone) et 3 (normale de l'écran),
            // réécrites dans le repère Unity (x = est, y = haut, z = nord).
            Vector3 forward = new Vector3(-cB * sA, sB, cA * cB);
            Vector3 up = new Vector3(cG * sA * sB + cA * sG, cB * cG, sA * sG - cA * cG * sB);

            return Quaternion.LookRotation(forward, up);
        }
    }
}
