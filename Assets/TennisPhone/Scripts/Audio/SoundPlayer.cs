using System.Collections.Generic;
using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Joue les sons du match. Abonné aux événements de RallyController : frappe, rebond, filet,
    /// swing dans le vide, lancer, faute, fin de point, de jeu et de set.
    /// Les sons sont fabriqués au lancement par SoundSynth ; six sources en 2D les jouent à tour de rôle.
    /// </summary>
    [RequireComponent(typeof(RallyController))]
    public sealed class SoundPlayer : MonoBehaviour
    {
        const int SourceCount = 6;

        // Le son du jeu ou du set attend la fin de l'annonce du point.
        const float FollowUpDelay = 1.1f;

        struct Scheduled
        {
            public float Time;
            public AudioClip Clip;
            public float Volume;
        }

        [Tooltip("Volume général des sons du match.")]
        [Range(0f, 1f)] [SerializeField] float volume = 0.8f;

        RallyController rally;
        GameplayConfig config;
        AudioSource[] sources;
        int nextSource;
        readonly List<Scheduled> scheduled = new List<Scheduled>();

        AudioClip hitFlat;
        AudioClip hitLift;
        AudioClip hitSlice;
        AudioClip bounce;
        AudioClip net;
        AudioClip netCord;
        AudioClip whiff;
        AudioClip toss;
        AudioClip fault;
        AudioClip pointWon;
        AudioClip pointLost;
        AudioClip gameWon;
        AudioClip gameLost;
        AudioClip setWon;

        /// <summary>Nombre de sons joués depuis le lancement (contrôle).</summary>
        public int PlayedCount { get; private set; }

        void Awake()
        {
            rally = GetComponent<RallyController>();
            config = GetComponent<RacketInput>().Config;

            sources = new AudioSource[SourceCount];
            for (int i = 0; i < SourceCount; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                sources[i] = source;
            }

            // Toujours dans le même ordre : le bruit, tiré d'une graine fixe, donne les mêmes sons.
            SoundSynth.ResetNoise();
            hitFlat = SoundSynth.HitFlat();
            hitLift = SoundSynth.HitLift();
            hitSlice = SoundSynth.HitSlice();
            bounce = SoundSynth.Bounce();
            net = SoundSynth.Net();
            netCord = SoundSynth.NetCord();
            whiff = SoundSynth.Whiff();
            toss = SoundSynth.Toss();
            fault = SoundSynth.Fault();
            pointWon = SoundSynth.PointWon();
            pointLost = SoundSynth.PointLost();
            gameWon = SoundSynth.GameWon();
            gameLost = SoundSynth.GameLost();
            setWon = SoundSynth.Set();
        }

        void OnEnable()
        {
            rally.BallHit += OnBallHit;
            rally.Bounced += OnBounced;
            rally.NetTouched += OnNetTouched;
            rally.Whiffed += OnWhiffed;
            rally.TossStarted += OnTossStarted;
            rally.ServiceFaulted += OnServiceFaulted;
            rally.PointEnded += OnPointEnded;
        }

        void OnDisable()
        {
            rally.BallHit -= OnBallHit;
            rally.Bounced -= OnBounced;
            rally.NetTouched -= OnNetTouched;
            rally.Whiffed -= OnWhiffed;
            rally.TossStarted -= OnTossStarted;
            rally.ServiceFaulted -= OnServiceFaulted;
            rally.PointEnded -= OnPointEnded;
        }

        void Update()
        {
            float now = Time.unscaledTime;
            for (int i = scheduled.Count - 1; i >= 0; i--)
            {
                if (now >= scheduled[i].Time)
                {
                    Play(scheduled[i].Clip, scheduled[i].Volume, 1f);
                    scheduled.RemoveAt(i);
                }
            }
        }

        // ---------------------------------------------------------------- Événements du match

        void OnBallHit(HitInfo hit)
        {
            AudioClip clip = hit.Spin > 0.25f ? hitLift : (hit.Spin < -0.25f ? hitSlice : hitFlat);

            // Plus le swing est fort, plus la frappe est sonore et aiguë. L'adversaire est loin.
            float power = Mathf.Clamp01(hit.Power);
            float level = 0.55f + 0.45f * power;
            float pitch = 0.94f + 0.12f * power;
            if (!hit.ByPlayer)
            {
                level *= 0.6f;
                pitch *= 0.97f;
            }

            Play(clip, level, pitch);
        }

        void OnBounced(BounceInfo bounce)
        {
            float level = 0.2f + 0.8f * Mathf.Min(1f, bounce.ImpactSpeed / 12f);
            if (bounce.Point.z > 0f)
            {
                level *= 0.6f;
            }

            if (!bounce.Live)
            {
                level *= 0.5f;
            }

            Play(this.bounce, level, 1f);
        }

        void OnNetTouched(Vector3 position)
        {
            // Près du haut du filet, la balle touche la bande ; plus bas, elle s'étouffe dans les mailles.
            bool cord = position.y > config.ToCourt().NetHeightAt(position.x) - 0.15f;
            Play(cord ? netCord : net, cord ? 0.6f : 0.8f, 1f);
        }

        void OnWhiffed(string detail)
        {
            Play(whiff, 0.5f, 1f);
        }

        void OnTossStarted(bool byPlayer)
        {
            Play(toss, byPlayer ? 0.4f : 0.25f, 1f);
        }

        void OnServiceFaulted()
        {
            Play(fault, 0.6f, 1f);
        }

        void OnPointEnded(PointInfo point)
        {
            AudioClip jingle = point.WonByPlayer ? pointWon : pointLost;
            float jingleLevel = point.WonByPlayer ? 0.6f : 0.5f;
            switch (point.Reason)
            {
                case PointReason.Out:
                    Play(fault, 0.6f, 1f);
                    break;
                case PointReason.Net:
                    // Le filet vient de se faire entendre.
                    break;
                case PointReason.DoubleFault:
                    Play(fault, 0.6f, 1f);
                    Schedule(jingle, jingleLevel, 0.3f);
                    break;
                default:
                    Play(jingle, jingleLevel, 1f);
                    break;
            }

            if (point.Result == ScoreEvent.Set)
            {
                Schedule(point.WonByPlayer ? setWon : gameLost, 0.8f, FollowUpDelay);
            }
            else if (point.Result == ScoreEvent.Game)
            {
                Schedule(point.WonByPlayer ? gameWon : gameLost, 0.7f, FollowUpDelay);
            }
        }

        // ---------------------------------------------------------------- Lecture

        void Schedule(AudioClip clip, float level, float delay)
        {
            scheduled.Add(new Scheduled { Time = Time.unscaledTime + delay, Clip = clip, Volume = level });
        }

        void Play(AudioClip clip, float level, float pitch)
        {
            AudioSource source = sources[nextSource];
            nextSource = (nextSource + 1) % sources.Length;

            // Hauteur tirée à ±2 % : deux frappes de suite ne sonnent jamais exactement pareil.
            source.pitch = pitch * Random.Range(0.98f, 1.02f);
            source.volume = Mathf.Clamp01(level * volume);
            source.clip = clip;
            source.Play();
            PlayedCount++;
        }
    }
}
