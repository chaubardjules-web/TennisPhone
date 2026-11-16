using System.Collections.Generic;
using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Retours visuels brefs autour d'une frappe ou d'une faute : éclair du cordage, anneau
    /// d'impact, éclats de la frappe parfaite, recul de la caméra, clignotement d'une ligne,
    /// recul du filet. Chaque effet est une forme simple animée par le code ; aucun ne change le jeu.
    /// </summary>
    public sealed class HitEffects
    {
        const float FlashDuration = 0.12f;
        const float RingDuration = 0.18f;
        const float RingStart = 0.15f;
        const float RingEnd = 0.9f;
        const float SparkDuration = 0.25f;
        const float SparkDistance = 0.5f;
        const int SparkCount = 6;
        const float RecoilDuration = 0.08f;
        const float RecoilDistance = 0.04f;
        const float BlinkStep = 0.12f;
        const float NetPushDuration = 0.25f;
        const float NetPushDistance = 0.06f;

        struct Flash
        {
            public Material Material;
            public float Age;
        }

        sealed class Ring
        {
            public Transform Transform;
            public Material Material;
            public Color Color;
            public float Size;
            public float Age = float.MaxValue;
        }

        readonly Camera view;
        readonly CourtVisual court;
        readonly Transform root;
        readonly List<Flash> flashes = new List<Flash>();
        readonly Ring[] rings = new Ring[2];
        readonly Transform[] sparks = new Transform[SparkCount];
        readonly Material whiteLine;

        int nextRing;
        Vector3 sparkOrigin;
        float sparkAge = float.MaxValue;
        float recoilAge = float.MaxValue;
        float recoilPower;
        Renderer blinkLine;
        Material blinkOriginal;
        float blinkAge;
        Transform netPanel;
        float netPushAge;
        float netPushSign;

        /// <summary>Décalage à ajouter à la position de la caméra (recul à l'impact).</summary>
        public Vector3 CameraOffset { get; private set; }

        public HitEffects(Camera view, CourtVisual court)
        {
            this.view = view;
            this.court = court;
            root = new GameObject("Effets").transform;
            whiteLine = Palette.Lumineux(Palette.Blanc, Palette.Blanc * 0.6f);

            for (int i = 0; i < rings.Length; i++)
            {
                Material material = Palette.NewVoile(Palette.TraineePlat);
                Transform ring = CourtBuilder.Primitive(PrimitiveType.Cylinder, root, "Anneau d'impact", Vector3.zero,
                    Quaternion.identity, Vector3.one, material);
                Palette.AsVeil(ring.GetComponent<Renderer>(), material);
                ring.gameObject.SetActive(false);
                rings[i] = new Ring { Transform = ring, Material = material };
            }

            for (int i = 0; i < SparkCount; i++)
            {
                sparks[i] = CourtBuilder.Box(root, "Éclat", Vector3.zero, Vector3.one * 0.04f, Palette.Creme);
                sparks[i].GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                sparks[i].gameObject.SetActive(false);
            }
        }

        /// <summary>Le cordage passe au blanc, puis revient à sa couleur en 0,12 s.</summary>
        public void FlashStrings(Renderer strings)
        {
            if (strings == null)
            {
                return;
            }

            Material material = strings.sharedMaterial;
            flashes.RemoveAll(flash => flash.Material == material);
            flashes.Add(new Flash { Material = material });
            material.SetColor("_EmissionColor", Color.white);
        }

        /// <summary>Disque face à la caméra qui s'élargit et s'efface au point d'impact.</summary>
        public void Impact(Vector3 point, Color color, float size)
        {
            Ring ring = rings[nextRing];
            nextRing = (nextRing + 1) % rings.Length;
            ring.Color = color;
            ring.Size = size;
            ring.Age = 0f;
            ring.Transform.position = point;
            ring.Transform.gameObject.SetActive(true);
            UpdateRing(ring);
        }

        /// <summary>Frappe parfaite : six petits éclats partent en étoile.</summary>
        public void Sparks(Vector3 point)
        {
            sparkOrigin = point;
            sparkAge = 0f;
            foreach (Transform spark in sparks)
            {
                spark.gameObject.SetActive(true);
            }

            UpdateSparks();
        }

        /// <summary>La caméra recule de 4 cm × puissance, puis revient.</summary>
        public void Recoil(float power)
        {
            recoilPower = Mathf.Clamp01(power);
            recoilAge = 0f;
        }

        /// <summary>La ligne la plus proche du point clignote deux fois en blanc.</summary>
        public void BlinkNearestLine(Vector3 point)
        {
            StopBlink();
            blinkLine = court.NearestLine(point);
            if (blinkLine != null)
            {
                blinkOriginal = blinkLine.sharedMaterial;
                blinkAge = 0f;
            }
        }

        /// <summary>Le panneau de filet touché recule de 6 cm, puis revient.</summary>
        public void PushNet(Vector3 ballPosition)
        {
            ResetNet();
            netPanel = court.NetPanelAt(ballPosition.x);
            netPushAge = 0f;

            // La balle s'arrête du côté d'où elle vient : le filet recule de l'autre côté.
            netPushSign = ballPosition.z >= 0f ? -1f : 1f;
        }

        public void Update(float deltaTime)
        {
            for (int i = flashes.Count - 1; i >= 0; i--)
            {
                Flash flash = flashes[i];
                flash.Age += deltaTime;
                float fade = Mathf.Clamp01(flash.Age / FlashDuration);
                flash.Material.SetColor("_EmissionColor",
                    Color.Lerp(Color.white, Palette.Cordage * RacketModel.StringGlow, fade));
                if (fade >= 1f)
                {
                    flashes.RemoveAt(i);
                }
                else
                {
                    flashes[i] = flash;
                }
            }

            foreach (Ring ring in rings)
            {
                if (ring.Age < RingDuration)
                {
                    ring.Age += deltaTime;
                    UpdateRing(ring);
                }
            }

            if (sparkAge < SparkDuration)
            {
                sparkAge += deltaTime;
                UpdateSparks();
            }

            CameraOffset = Vector3.zero;
            if (recoilAge < RecoilDuration && view != null)
            {
                recoilAge += deltaTime;
                float remaining = 1f - Mathf.Clamp01(recoilAge / RecoilDuration);
                CameraOffset = -view.transform.forward * (RecoilDistance * recoilPower * remaining);
            }

            if (blinkLine != null)
            {
                blinkAge += deltaTime;
                int step = Mathf.FloorToInt(blinkAge / BlinkStep);
                if (step >= 4)
                {
                    StopBlink();
                }
                else
                {
                    blinkLine.sharedMaterial = step % 2 == 0 ? whiteLine : blinkOriginal;
                }
            }

            if (netPanel != null)
            {
                netPushAge += deltaTime;
                float progress = Mathf.Clamp01(netPushAge / NetPushDuration);

                // Départ sec, retour amorti.
                float push = NetPushDistance * (1f - progress) * (1f - progress);
                SetNetOffset(netPushSign * push);
                if (progress >= 1f)
                {
                    ResetNet();
                }
            }
        }

        void UpdateRing(Ring ring)
        {
            float progress = Mathf.Clamp01(ring.Age / RingDuration);
            if (progress >= 1f)
            {
                ring.Transform.gameObject.SetActive(false);
                return;
            }

            float diameter = Mathf.Lerp(RingStart, RingEnd, progress) * ring.Size;
            ring.Transform.localScale = new Vector3(diameter, 0.001f, diameter);
            if (view != null)
            {
                ring.Transform.rotation = Quaternion.FromToRotation(Vector3.up, -view.transform.forward);
            }

            ring.Material.color = Palette.WithAlpha(ring.Color, Mathf.Lerp(0.8f, 0f, progress));
        }

        void UpdateSparks()
        {
            float progress = Mathf.Clamp01(sparkAge / SparkDuration);
            bool done = progress >= 1f;
            Vector3 right = view != null ? view.transform.right : Vector3.right;
            Vector3 up = view != null ? view.transform.up : Vector3.up;
            float distance = SparkDistance * (1f - (1f - progress) * (1f - progress));
            for (int i = 0; i < SparkCount; i++)
            {
                if (done)
                {
                    sparks[i].gameObject.SetActive(false);
                    continue;
                }

                float angle = (i + 0.25f) * 2f * Mathf.PI / SparkCount;
                sparks[i].position = sparkOrigin + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * distance;
                sparks[i].rotation = Quaternion.Euler(0f, 0f, 45f + 360f * progress);
                sparks[i].localScale = Vector3.one * (0.04f * (1f - 0.6f * progress));
            }
        }

        void StopBlink()
        {
            if (blinkLine != null)
            {
                blinkLine.sharedMaterial = blinkOriginal;
                blinkLine = null;
            }
        }

        void SetNetOffset(float z)
        {
            Vector3 position = netPanel.localPosition;
            position.z = z;
            netPanel.localPosition = position;
        }

        void ResetNet()
        {
            if (netPanel != null)
            {
                SetNetOffset(0f);
                netPanel = null;
            }
        }
    }
}
