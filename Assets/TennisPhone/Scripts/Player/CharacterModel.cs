using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Personnage de dessin animé, fait de formes simples : grosse tête ronde, deux yeux, des
    /// joues roses, un corps compact, des bras reliés au corps et de grandes mains.
    /// Dessin original : il ne reprend aucun personnage existant.
    /// Les deux camps ont la même construction ; ils se distinguent par la taille, les couleurs
    /// et la coiffe (casquette à l'envers pour le joueur, bandeau pour l'adversaire).
    /// Origine entre les pieds, au sol. Le personnage regarde vers +z ; l'adversaire est
    /// retourné. Aucun squelette : trois mouvements appliqués par le code (respiration, course,
    /// bras qui suit la raquette).
    /// </summary>
    public sealed class CharacterModel
    {
        const float ShoulderWidth = 0.2f;
        const float ShoulderHeight = 1.02f;
        const float ArmMin = 0.18f;
        const float ArmMax = 0.62f;
        const float ArmThickness = 0.085f;
        const float HandSize = 0.15f;
        const float HeadHeight = 1.34f;

        const float BreathHeight = 0.012f;
        const float BreathFrequency = 1.6f;
        const float RunLeanDegrees = 6f;
        const float RunBobHeight = 0.03f;
        const float RunBobFrequency = 3f;

        /// <summary>Teintes de peau du public et des joueurs.</summary>
        public static readonly Color[] SkinTones =
        {
            Palette.Hex(0xF3CDA6), Palette.Hex(0xE2B88A), Palette.Hex(0xC68E5F), Palette.Hex(0x9A6540), Palette.Hex(0x6B4429),
        };

        /// <summary>Couleurs de cheveux. Aucune dans les jaunes-verts de la balle.</summary>
        public static readonly Color[] HairTones =
        {
            Palette.Hex(0x2B2422), Palette.Hex(0x5A3F2C), Palette.Hex(0x8A5A34), Palette.Hex(0xC98F58), Palette.Hex(0xB9B4AE), Palette.PruneNuit,
        };

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

            Color skin = opponent ? SkinTones[2] : SkinTones[1];
            Color jersey = opponent ? Palette.Lavande : Palette.Creme;
            Color team = opponent ? Palette.Prune : Palette.Framboise;

            root = new GameObject(name).transform;
            if (opponent)
            {
                // Un peu plus grand, et tourné vers le joueur.
                root.localRotation = Quaternion.Euler(0f, 180f, 0f);
                root.localScale = Vector3.one * 1.14f;
            }
            else
            {
                root.localScale = Vector3.one * 1.08f;
            }

            body = Group(root, "Corps");
            upper = Group(body, "Buste");

            foreach (float side in new[] { -1f, 1f })
            {
                Part(PrimitiveType.Sphere, body, "Chaussure", new Vector3(side * 0.1f, 0.05f, 0.04f), new Vector3(0.15f, 0.1f, 0.27f), Palette.Creme);
                Part(PrimitiveType.Capsule, body, "Jambe", new Vector3(side * 0.1f, 0.31f, 0f), new Vector3(0.13f, 0.26f, 0.13f), skin);
            }

            Part(PrimitiveType.Sphere, body, "Short", new Vector3(0f, 0.6f, 0f), new Vector3(0.4f, 0.24f, 0.32f), team);
            Part(PrimitiveType.Capsule, upper, "Torse", new Vector3(0f, 0.86f, 0f), new Vector3(0.4f, 0.27f, 0.31f), jersey);
            Part(PrimitiveType.Cylinder, upper, "Bande du maillot", new Vector3(0f, 0.9f, 0f), new Vector3(0.415f, 0.055f, 0.322f), team);

            BuildHead(upper, skin, opponent ? Palette.PruneNuit : HairTones[1]);
            if (opponent)
            {
                Part(PrimitiveType.Cylinder, upper, "Bandeau", new Vector3(0f, HeadHeight + 0.12f, -0.02f), new Vector3(0.457f, 0.022f, 0.45f), Palette.Creme);
            }
            else
            {
                // Casquette portée à l'envers : la visière pointe vers la caméra.
                Part(PrimitiveType.Sphere, upper, "Casquette", new Vector3(0f, HeadHeight + 0.12f, 0f), new Vector3(0.49f, 0.3f, 0.47f), team);
                Part(PrimitiveType.Cube, upper, "Visière", new Vector3(0f, HeadHeight + 0.1f, -0.27f), new Vector3(0.26f, 0.02f, 0.18f), team);
            }

            racketArm = Part(PrimitiveType.Capsule, body, "Bras de la raquette", Vector3.zero, Vector3.one * ArmThickness, skin);
            racketForearm = Part(PrimitiveType.Capsule, body, "Avant-bras de la raquette", Vector3.zero, Vector3.one * ArmThickness, skin);
            racketHand = Part(PrimitiveType.Sphere, body, "Main de la raquette", Vector3.zero, Vector3.one * HandSize, skin);
            bracelet = Part(PrimitiveType.Cylinder, body, "Bracelet", Vector3.zero, new Vector3(0.1f, 0.02f, 0.1f), team);
            freeArm = Part(PrimitiveType.Capsule, body, "Bras libre", Vector3.zero, Vector3.one * ArmThickness, skin);
            freeHand = Part(PrimitiveType.Sphere, body, "Main libre", Vector3.zero, Vector3.one * HandSize, skin);

            Pose(root.TransformPoint(new Vector3(dominant * 0.33f, 0.88f, 0.15f)));
        }

        /// <summary>Le joueur : maillot crème, accents framboise, casquette à l'envers.</summary>
        public static CharacterModel BuildPlayer(Handedness handedness)
        {
            return new CharacterModel("Joueur", false, handedness);
        }

        /// <summary>L'adversaire : maillot lavande, accents prune, bandeau.</summary>
        public static CharacterModel BuildOpponent()
        {
            return new CharacterModel("Adversaire", true, Handedness.Right);
        }

        /// <summary>Tête ronde avec ses cheveux, ses yeux et ses joues. Sert aussi au public.</summary>
        static void BuildHead(Transform parent, Color skin, Color hair)
        {
            Part(PrimitiveType.Sphere, parent, "Tête", new Vector3(0f, HeadHeight, 0f), new Vector3(0.44f, 0.42f, 0.44f), skin);

            // Les cheveux couvrent le dessus, les côtés et l'arrière ; le visage reste dégagé.
            Part(PrimitiveType.Sphere, parent, "Cheveux", new Vector3(0f, HeadHeight + 0.05f, -0.04f), new Vector3(0.47f, 0.4f, 0.44f), hair);

            foreach (float side in new[] { -1f, 1f })
            {
                Part(PrimitiveType.Sphere, parent, "Œil", new Vector3(side * 0.085f, HeadHeight + 0.02f, 0.196f), new Vector3(0.055f, 0.075f, 0.03f), Palette.Encre);
                Part(PrimitiveType.Sphere, parent, "Joue", new Vector3(side * 0.135f, HeadHeight - 0.05f, 0.162f), new Vector3(0.07f, 0.045f, 0.03f), Palette.Rose);
            }
        }

        /// <summary>
        /// Un spectateur assis, dans le même style que les joueurs, en trois formes : corps,
        /// tête et cheveux. Renvoie sa racine, posée sur le siège et tournée vers « facing ».
        /// </summary>
        public static Transform BuildSpectator(Transform parent, Vector3 seat, Vector3 facing, Color clothes, Color skin, Color hair)
        {
            var spectator = new GameObject("Spectateur").transform;
            spectator.SetParent(parent, false);
            spectator.localPosition = seat;
            spectator.localRotation = Quaternion.LookRotation(facing);

            Transform torso = Part(PrimitiveType.Capsule, spectator, "Corps", new Vector3(0f, 0.25f, 0f), new Vector3(0.36f, 0.25f, 0.3f), clothes);
            Transform head = Part(PrimitiveType.Sphere, spectator, "Tête", new Vector3(0f, 0.62f, 0.02f), new Vector3(0.34f, 0.32f, 0.34f), skin);
            Transform top = Part(PrimitiveType.Sphere, spectator, "Cheveux", new Vector3(0f, 0.66f, -0.02f), new Vector3(0.36f, 0.3f, 0.34f), hair);
            foreach (Transform part in new[] { torso, head, top })
            {
                part.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            return spectator;
        }

        /// <summary>
        /// Place le personnage et joue ses mouvements : respiration à l'arrêt, et en course le
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

            // La main suit la raquette, dans la limite de la longueur du bras.
            Vector3 reach = body.InverseTransformPoint(racketGrip) - shoulder;
            float length = Mathf.Clamp(reach.magnitude, ArmMin, ArmMax);
            Vector3 hand = shoulder + (reach.sqrMagnitude > 1e-6f ? reach.normalized : Vector3.down) * length;

            // Le coude : légèrement en arrière en coup droit ; en revers il passe devant le
            // torse, pour que le bras croise la poitrine au lieu de la traverser.
            float crossing = Mathf.Clamp01((0.05f - hand.x * dominant) / 0.3f);
            Vector3 elbow = (shoulder + hand) * 0.5f;
            elbow.x += dominant * 0.07f * (1f - crossing);
            elbow.z = Mathf.Lerp(elbow.z - 0.07f, 0.27f, crossing);

            Segment(racketArm, shoulder, elbow);
            Segment(racketForearm, elbow, hand);
            racketHand.localPosition = hand;
            bracelet.localPosition = Vector3.Lerp(elbow, hand, 0.6f);
            bracelet.localRotation = racketForearm.localRotation;

            Vector3 freeShoulder = new Vector3(-dominant * ShoulderWidth, ShoulderHeight, 0f) + lift;
            Vector3 freeRest = new Vector3(-dominant * 0.29f, 0.72f, 0.05f) + lift * 0.5f;
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
