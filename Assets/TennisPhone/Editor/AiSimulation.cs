using System.Text;
using UnityEditor;
using UnityEngine;

namespace TennisPhone.EditorTools
{
    /// <summary>
    /// Outil de réglage : tire un grand nombre de frappes de l'adversaire pour chaque niveau
    /// et affiche dans la console la part qui finit dans le court, dehors ou dans le filet.
    /// </summary>
    public static class AiSimulation
    {
        // Chaque frappe demande un calcul de trajectoire complet : rester sous quelques centaines
        // pour ne pas bloquer l'éditeur trop longtemps.
        const int ShotsPerLevel = 400;
        const float StretchedShare = 0.25f;

        [MenuItem("TennisPhone/Simuler les frappes de l'adversaire")]
        public static void SimulateShots()
        {
            // Une ligne de console par niveau, pour rester lisible.
            foreach (string line in Run(SceneBuilder.EnsureGameplayConfig()).Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    Debug.Log(line.StartsWith("[TennisPhone]") ? line : "[TennisPhone] IA " + line);
                }
            }
        }

        public static string Run(GameplayConfig config)
        {
            CourtDimensions court = config.ToCourt();
            BallSettings ball = config.ToBallSettings();
            var random = new System.Random(12345);
            var report = new StringBuilder("[TennisPhone] Frappes de l'adversaire (" + ShotsPerLevel + " par niveau, "
                + Mathf.RoundToInt(StretchedShare * 100f) + " % en bout de course)\n");

            foreach (AiDifficulty difficulty in new[] { AiDifficulty.Easy, AiDifficulty.Normal, AiDifficulty.Hard })
            {
                AiLevel level = config.GetAiLevel(difficulty);
                var ai = new OpponentAi(1000 + (int)difficulty);
                int inside = 0, outside = 0, net = 0;
                int setShots = 0, setFaults = 0, stretchedShots = 0, stretchedFaults = 0;

                for (int i = 0; i < ShotsPerLevel; i++)
                {
                    var ballPosition = new Vector3(
                        Mathf.Lerp(-3.5f, 3.5f, (float)random.NextDouble()),
                        0.9f,
                        Mathf.Lerp(8f, 11.5f, (float)random.NextDouble()));
                    var playerPosition = new Vector3(Mathf.Lerp(-3.5f, 3.5f, (float)random.NextDouble()), 0f, -12.7f);
                    bool stretched = random.NextDouble() < StretchedShare;

                    AiShot shot = ai.ChooseShot(ballPosition, playerPosition, stretched, level, config, ball, court);
                    var state = new BallState { Position = ballPosition, Velocity = shot.Solution.Velocity, Spin = shot.Solution.Spin };
                    BallPrediction prediction = BallPhysics.Predict(state, ball, court);

                    bool fault;
                    if (prediction.HitsNet)
                    {
                        net++;
                        fault = true;
                    }
                    else if (prediction.Lands && prediction.Landing.z < 0f && court.IsInside(prediction.Landing.x, prediction.Landing.z))
                    {
                        inside++;
                        fault = false;
                    }
                    else
                    {
                        outside++;
                        fault = true;
                    }

                    if (stretched)
                    {
                        stretchedShots++;
                        stretchedFaults += fault ? 1 : 0;
                    }
                    else
                    {
                        setShots++;
                        setFaults += fault ? 1 : 0;
                    }
                }

                report.Append(difficulty + " : dans le court " + Percent(inside, ShotsPerLevel)
                    + ", dehors " + Percent(outside, ShotsPerLevel)
                    + ", filet " + Percent(net, ShotsPerLevel)
                    + " | fautes bien placé " + Percent(setFaults, setShots)
                    + ", en bout de course " + Percent(stretchedFaults, stretchedShots) + "\n");
            }

            return report.ToString();
        }

        static string Percent(int count, int total)
        {
            return total > 0 ? (100f * count / total).ToString("F1") + " %" : "—";
        }
    }
}
