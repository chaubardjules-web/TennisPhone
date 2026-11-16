using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Figurine articulée en bois peint, faite de formes simples : tête ovale sans visage,
    /// bras et jambes reliés au corps. Les deux camps ont la même construction ; ils se
    /// distinguent par la taille, les couleurs et la coiffe.
    /// Origine entre les pieds, au sol. La figurine regarde vers +z ; celle de l'adversaire est
    /// retournée. Aucun squelette : trois mouvements appliqués par le code (respiration, course,
    /// bras qui suit la raquette).
    /// </summary>
    public sealed class CharacterModel
    {
        const float ShoulderWidth = 0.25f;
        const float ShoulderHeight = 1.36f;
        const float ArmMin = 0.4f;
        const float ArmMax = 0.75f;
        const float ArmThickness = 0.1f;

        const float BreathHeight = 0.015f;
        const float BreathFrequency = 1.6f;
        const float RunLeanDegrees = 6f;
        const float RunBobHeight = 0.03f;
        const float RunBobFrequency = 3f;

        readonly Transform root;
        readonly Transform body;
        readonly Transform upper;
        readonly Transform racketArm;
        readonly Transform racketForearm;
        readonly Transform racketHand;
        readonly Transform bracelet;
        readonly Transform freeArm;
        readonly Transform freeHand;

        /// <summary>+1 si la raquette est tenue de la main droite, -1 de la gauche.</summary>
        readonly float dominant;

        Vector3 run;
        float phase;

        public Transform Root => root;

        CharacterModel(string name, bool opponent, Handedness handedness)
        {
            dominant = handedness == Handedness.Right ? 1f : -1f;

            Color wood = Palette.BoisNaturel;
            Color jersey = opponent ? Palette.Lavande : Palette.Creme;
            Color team = opponent ? Palette.Prune : Palette.Framboise;

            root = new GameObject(name).transform;
            if (opponent)
            {
                // Un peu plus grand, et tourné vers le joueur.
                root.localRotation = Quaternion.Euler(0f, 180f, 0f);
                root.localScale = new Vector3(0.97f, 1.06f, 0.97f);
            }

            body = Group(root, "Corps");
            upper = Group(body, "Buste");

            foreach (float side in new[] { -1f, 1f })
            {
                Part(PrimitiveType.Cube, body, "Chaussure", new Vector3(side * 0.11f, 0.04f, 0.04f), new Vector3(0.11f, 0.08f, 0.26f), Palette.Creme);
                Part(PrimitiveType.Capsule, body, "Jambe", new Vector3(side * 0.11f, 0.46f, 0f), new Vector3(0.15f, 0.4f, 0.15f), wood);
            }

            Part(PrimitiveType.Cylinder, body, "Short", new Vector3(0f, 0.8f, 0f), new Vector3(0.4f, 0.12f, 0.28f), team);
            Part(PrimitiveType.Capsule, upper, "Torse", new Vector3(0f, 1.14f, 0f), new Vector3(0.44f, 0.31f, 0.28f), jersey);
            Part(PrimitiveType.Cylinder, upper, "Bande du maillot", new Vector3(0f, 1.2f, 0f), new Vector3(0.455f, 0.07f, 0.29f), team);
            Part(PrimitiveType.Cylinder, upper, "Cou", new Vector3(0f, 1.47f, 0f), new Vector3(0.1f, 0.03f, 0.1f), wood);
            Part(PrimitiveType.Capsule, upper, "Tête", new Vector3(0f, 1.615f, 0f), new Vector3(0.22f, 0.125f, 0.23f), wood);

            // La coiffe donne l'orientation : il n'y a pas de visage.
            if (opponent)
            {
                Part(PrimitiveType.Sphere, upper, "Cheveux", new Vector3(0f, 1.7f, 0f), new Vector3(0.215f, 0.1f, 0.225f), Palette.PruneNuit);
                Part(PrimitiveType.Cylinder, upper, "Bandeau", new Vector3(0f, 1.66f, 0f), new Vector3(0.228f, 0.022f, 0.238f), Palette.Creme);
            }
            else
            {
                // Casquette portée à l'envers : la visière pointe vers la caméra.
                Part(PrimitiveType.Sphere, upper, "Casquette", new Vector3(0f, 1.685f, 0f), new Vector3(0.235f, 0.13f, 0.245f), team);
                Part(PrimitiveType.Cube, upper, "Visière", new Vector3(0f, 1.66f, -0.15f), new Vector3(0.17f, 0.018f, 0.12f), team);
            }

            racketArm = Part(PrimitiveType.Capsule, body, "Bras de la raquette", Vector3.zero, Vector3.one * ArmThickness, wood);
            racketForearm = Part(PrimitiveType.Capsule, body, "Avant-bras de la raquette", Vector3.zero, Vector3.one * ArmThickness, wood);
            racketHand = Part(PrimitiveType.Sphere, body, "Main de la raquette", Vector3.zero, Vector3.one * 0.11f, wood);
            bracelet = Part(PrimitiveType.Cylinder, body, "Bracelet", Vector3.zero, new Vector3(0.115f, 0.025f, 0.115f), team);
            freeArm = Part(PrimitiveType.Capsule, body, "Bras libre", Vector3.zero, Vector3.one * ArmThickness, wood);
            freeHand = Part(PrimitiveType.Sphere, body, "Main libre", Vector3.zero, Vector3.one * 0.11f, wood);

            Pose(root.TransformPoint(new Vector3(dominant * 0.35f, 0.95f, 0.15f)));
        }

        /// <summary>Figurine du joueur : maillot crème, accents framboise, casquette.</summary>
        public static CharacterModel BuildPlayer(Handedness handedness)
        {
            return new CharacterModel("Joueur", false, handedness);
        }

        /// <summary>Figurine de l'adversaire : maillot lavande, accents prune, bandeau.</summary>
        public static CharacterModel BuildOpponent()
        {
            return new CharacterModel("Adversaire", true, Handedness.Right);
        }

        /// <summary>
        /// Place la figurine et joue ses mouvements : respiration à l'arrêt, et en course le
        /// corps penché vers la direction du déplacement avec un petit rebond.
        /// </summary>
        /// <param name="position">Position des pieds.</param>
        /// <param name="velocity">Vitesse de déplacement, en m/s.</param>
        public void Move(Vector3 position, Vector3 velocity, float deltaTime)
        {
            root.position = position;

            velocity.y = 0f;
            float speed = velocity.magnitude;
            Vector3 target = speed > 0.4f ? velocity / speed : Vector3.zero;
            run = Vector3.Lerp(run, target, 1f - Mathf.Exp(-10f * deltaTime));
            float running = Mathf.Clamp01(run.magnitude);
            phase += deltaTime;

            Quaternion lean = running > 0.01f
                ? Quaternion.AngleAxis(RunLeanDegrees * running, Vector3.Cross(Vector3.up, run.normalized))
                : Quaternion.identity;
            body.rotation = lean * root.rotation;
            body.localPosition = new Vector3(0f,
                RunBobHeight * running * Mathf.Abs(Mathf.Sin(Mathf.PI * RunBobFrequency * phase)), 0f);
            upper.localPosition = new Vector3(0f,
                BreathHeight * (1f - running) * Mathf.Sin(2f * Mathf.PI * BreathFrequency * phase), 0f);
        }

        /// <summary>Tend le bras de la raquette vers la main, et laisse l'autre au repos.</summary>
        /// <param name="racketGrip">Position du bout du manche de la raquette, dans le monde.</param>
        public void Pose(Vector3 racketGrip)
        {
            Vector3 lift = upper.localPosition;
            Vector3 shoulder = new Vector3(dominant * ShoulderWidth, ShoulderHeight, 0f) + lift;

            // La main suit la raquette ; la longueur du bras reste entre 0,40 et 0,75 m.
            Vector3 reach = body.InverseTransformPoint(racketGrip) - shoulder;
            float length = Mathf.Clamp(reach.magnitude, ArmMin, ArmMax);
            Vector3 hand = shoulder + (reach.sqrMagnitude > 1e-6f ? reach.normalized : Vector3.down) * length;

            // Le coude : légèrement en arrière en coup droit ; en revers il passe devant le
            // torse, pour que le bras croise la poitrine au lieu de la traverser.
            float crossing = Mathf.Clamp01((0.05f - hand.x * dominant) / 0.3f);
            Vector3 elbow = (shoulder + hand) * 0.5f;
            elbow.x += dominant * 0.06f * (1f - crossing);
            elbow.z = Mathf.Lerp(elbow.z - 0.08f, 0.26f, crossing);

            Segment(racketArm, shoulder, elbow);
            Segment(racketForearm, elbow, hand);
            racketHand.localPosition = hand;
            bracelet.localPosition = Vector3.Lerp(elbow, hand, 0.7f);
            bracelet.localRotation = racketForearm.localRotation;

            Vector3 freeShoulder = new Vector3(-dominant * ShoulderWidth, ShoulderHeight, 0f) + lift;
            Vector3 freeRest = new Vector3(-dominant * 0.31f, 0.93f, 0.05f) + lift * 0.5f;
            Segment(freeArm, freeShoulder, freeRest);
            freeHand.localPosition = freeRest;
        }

        /// <summary>Place une capsule entre deux points du corps.</summary>
        static void Segment(Transform part, Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            float length = direction.magnitude;
            part.localPosition = (from + to) * 0.5f;
            part.localRotation = length > 1e-4f ? Quaternion.FromToRotation(Vector3.up, direction / length) : Quaternion.identity;

            // Les bouts arrondis dépassent un peu : les deux moitiés du bras se rejoignent au coude.
            part.localScale = new Vector3(ArmThickness, length * 0.5f + ArmThickness * 0.25f, ArmThickness);
        }

        static Transform Group(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        static Transform Part(PrimitiveType type, Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            return CourtBuilder.Primitive(type, parent, name, position, Quaternion.identity, scale, color);
        }
    }
}
