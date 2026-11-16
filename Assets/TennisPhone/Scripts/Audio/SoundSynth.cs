using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Sons du jeu, fabriqués par le code au lancement : aucun fichier audio (ART_DIRECTION.md,
    /// section 8). Percussions de bois : « poc » de la frappe, « toc » du rebond, « fouf » du
    /// filet, petits motifs de lames de bois pour les points, les jeux et le set.
    /// Mono, 44 100 Hz. Le bruit vient d'un générateur à graine fixe : les sons sont identiques
    /// à chaque lancement.
    /// </summary>
    public static class SoundSynth
    {
        public const int SampleRate = 44100;

        const float Tau = 2f * Mathf.PI;

        // Notes des motifs, en hertz.
        const float A3 = 220f;
        const float C4 = 261.63f;
        const float D4 = 293.66f;
        const float E4 = 329.63f;
        const float G4 = 392f;
        const float C5 = 523.25f;
        const float E5 = 659.26f;
        const float G5 = 783.99f;
        const float C6 = 1046.5f;
        const float E6 = 1318.51f;

        static System.Random random = new System.Random(12345);

        /// <summary>Remet le générateur de bruit à sa graine, avant de fabriquer la série de sons.</summary>
        public static void ResetNoise()
        {
            random = new System.Random(12345);
        }

        // ---------------------------------------------------------------- Frappes et balle

        /// <summary>Frappe à plat : « poc ».</summary>
        public static AudioClip HitFlat()
        {
            float[] samples = Buffer(0.11f);
            AddHit(samples, 520f, 190f, 0.9f, true);
            return Finish("Frappe à plat", samples);
        }

        /// <summary>Frappe liftée : le « poc », plus un frottement aigu.</summary>
        public static AudioClip HitLift()
        {
            float[] samples = Buffer(0.13f);
            AddHit(samples, 480f, 170f, 0.9f, true);

            // Frottement : bruit entre 2 500 et 5 000 Hz, qui monte en 10 ms puis s'éteint.
            float low = 0f;
            float high = 0f;
            for (int i = 0; i < Count(0.06f) && i < samples.Length; i++)
            {
                float t = Time(i);
                float envelope = t < 0.01f ? t / 0.01f : Mathf.Exp(-(t - 0.01f) / 0.02f);
                float band = LowPass(HighPass(Noise(), 2500f, ref low), 5000f, ref high);
                samples[i] += band * envelope * 0.25f;
            }

            return Finish("Frappe liftée", samples);
        }

        /// <summary>Frappe slicée : un corps plus doux, plus un souffle.</summary>
        public static AudioClip HitSlice()
        {
            float[] samples = Buffer(0.14f);
            AddHit(samples, 440f, 220f, 0.7f, false);

            float state = 0f;
            for (int i = 0; i < Count(0.08f) && i < samples.Length; i++)
            {
                samples[i] += LowPass(Noise(), 1200f, ref state) * Mathf.Exp(-Time(i) / 0.03f) * 0.3f;
            }

            return Finish("Frappe slicée", samples);
        }

        /// <summary>Rebond : « toc ».</summary>
        public static AudioClip Bounce()
        {
            float[] samples = Buffer(0.08f);
            AddGlide(samples, 260f, 120f, 0.018f, 0.02f, 0.8f);

            float state = 0f;
            for (int i = 0; i < Count(0.006f); i++)
            {
                samples[i] += LowPass(Noise(), 800f, ref state) * 0.3f;
            }

            return Finish("Rebond", samples);
        }

        /// <summary>Balle dans le filet : « fouf », avec trois petits clics de maille.</summary>
        public static AudioClip Net()
        {
            float[] samples = Buffer(0.26f);
            float state = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = Time(i);
                float attack = Mathf.Clamp01(t / 0.006f);
                samples[i] += LowPass(Noise(), 700f, ref state) * attack * Mathf.Exp(-t / 0.07f) * 0.7f;
                samples[i] += Mathf.Sin(Tau * 90f * t) * Mathf.Exp(-t / 0.05f) * 0.5f;
            }

            foreach (float start in new[] { 0.02f, 0.055f, 0.095f })
            {
                int first = Count(start);
                for (int i = 0; i < Count(0.002f); i++)
                {
                    samples[first + i] += Noise() * 0.15f;
                }
            }

            return Finish("Filet", samples);
        }

        /// <summary>Balle sur la bande du filet : un tintement sec.</summary>
        public static AudioClip NetCord()
        {
            float[] samples = Buffer(0.05f);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = Time(i);
                samples[i] = (Mathf.Sin(Tau * 1900f * t) + 0.4f * Mathf.Sin(Tau * 2850f * t)) * Mathf.Exp(-t / 0.012f) * 0.4f;
            }

            return Finish("Bande du filet", samples);
        }

        /// <summary>Swing dans le vide : un souffle qui monte.</summary>
        public static AudioClip Whiff()
        {
            const float duration = 0.18f;
            float[] samples = Buffer(duration);
            float low = 0f;
            float high = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = Time(i);
                float frequency = Mathf.Lerp(600f, 2400f, t / duration);
                float band = LowPass(HighPass(Noise(), frequency * 0.7f, ref low), frequency * 1.4f, ref high);
                samples[i] = band * Mathf.Sin(Mathf.PI * t / duration) * 0.3f;
            }

            return Finish("Dans le vide", samples);
        }

        /// <summary>Lancer de la balle de service : une note qui monte.</summary>
        public static AudioClip Toss()
        {
            const float duration = 0.12f;
            float[] samples = Buffer(duration);
            float phase = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = Time(i);
                phase += Tau * Mathf.Lerp(300f, 600f, t / duration) / SampleRate;
                samples[i] = Mathf.Sin(phase) * Mathf.Sin(Mathf.PI * t / duration) * 0.2f;
            }

            return Finish("Lancer", samples);
        }

        // ---------------------------------------------------------------- Motifs

        /// <summary>Faute : deux coups de bois secs.</summary>
        public static AudioClip Fault()
        {
            float[] samples = Buffer(0.22f);
            foreach (float start in new[] { 0f, 0.11f })
            {
                int first = Count(start);
                for (int i = first; i < samples.Length; i++)
                {
                    float t = Time(i - first);
                    samples[i] += (Mathf.Sin(Tau * 880f * t) + 0.5f * Mathf.Sin(Tau * 1320f * t)) * Mathf.Exp(-t / 0.018f) * 0.45f;
                }
            }

            return Finish("Faute", samples);
        }

        /// <summary>Point gagné : quatre lames qui montent.</summary>
        public static AudioClip PointWon()
        {
            float[] samples = Buffer(0.62f);
            AddPointWon(samples);
            return Finish("Point gagné", samples);
        }

        /// <summary>Point perdu : deux lames qui descendent, la seconde fléchit.</summary>
        public static AudioClip PointLost()
        {
            float[] samples = Buffer(0.52f);
            AddBar(samples, 0f, E4, 0.2f, 0.4f);
            AddBar(samples, 0.16f, C4, 0.2f, 0.4f, 40f, 0.25f);

            float state = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = LowPass(samples[i], 2500f, ref state);
            }

            return Finish("Point perdu", samples);
        }

        /// <summary>Jeu gagné : le motif du point, puis deux lames ensemble.</summary>
        public static AudioClip GameWon()
        {
            float[] samples = Buffer(0.9f);
            AddPointWon(samples);
            AddBar(samples, 0.38f, E5, 0.4f, 0.5f);
            AddBar(samples, 0.38f, C6, 0.4f, 0.5f);
            return Finish("Jeu gagné", samples);
        }

        /// <summary>Jeu perdu : quatre lames qui descendent.</summary>
        public static AudioClip GameLost()
        {
            float[] samples = Buffer(0.8f);
            float[] notes = { E4, D4, C4, A3 };
            for (int i = 0; i < notes.Length; i++)
            {
                AddBar(samples, 0.15f * i, notes[i], 0.2f, 0.4f);
            }

            return Finish("Jeu perdu", samples);
        }

        /// <summary>Set : cinq lames qui montent, puis un accord qui résonne.</summary>
        public static AudioClip Set()
        {
            float[] samples = Buffer(1.6f);
            float[] run = { C5, E5, G5, C6, E6 };
            for (int i = 0; i < run.Length; i++)
            {
                AddBar(samples, 0.1f * i, run[i], 0.2f, 0.5f);
            }

            foreach (float note in new[] { C5, E5, G5, C6 })
            {
                AddBar(samples, 0.6f, note, 0.6f, 0.4f);
            }

            return Finish("Set", samples);
        }

        // ---------------------------------------------------------------- Briques

        static void AddPointWon(float[] samples)
        {
            AddBar(samples, 0f, G4, 0.16f, 0.5f);
            AddBar(samples, 0.09f, C5, 0.16f, 0.5f);
            AddBar(samples, 0.18f, E5, 0.16f, 0.5f);
            AddBar(samples, 0.27f, G5, 0.3f, 0.5f);
        }

        /// <summary>Corps d'une frappe, avec ou sans le claquement et la note du cordage.</summary>
        static void AddHit(float[] samples, float from, float to, float gain, bool snap)
        {
            AddGlide(samples, from, to, 0.025f, 0.03f, gain);
            if (!snap)
            {
                return;
            }

            // Claquement : bruit aigu très bref.
            float state = 0f;
            for (int i = 0; i < Count(0.012f); i++)
            {
                samples[i] += HighPass(Noise(), 1800f, ref state) * Mathf.Exp(-Time(i) / 0.004f) * 0.5f;
            }

            // Cordage : une note tenue, très discrète.
            for (int i = 0; i < samples.Length; i++)
            {
                float t = Time(i);
                samples[i] += Mathf.Sin(Tau * 1240f * t) * Mathf.Exp(-t / 0.04f) * 0.1f;
            }
        }

        /// <summary>Sinus dont la fréquence glisse de « from » à « to » : f(t) = to + (from − to) × exp(−t ÷ glide).</summary>
        static void AddGlide(float[] samples, float from, float to, float glide, float decay, float gain)
        {
            float phase = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = Time(i);
                phase += Tau * (to + (from - to) * Mathf.Exp(-t / glide)) / SampleRate;
                samples[i] += Mathf.Sin(phase) * Mathf.Exp(-t / decay) * gain;
            }
        }

        /// <summary>
        /// Lame de bois : la note, plus deux harmoniques brèves qui donnent l'attaque du maillet.
        /// « bendCents » fait descendre la note de ce nombre de centièmes de demi-ton en « bendTime ».
        /// </summary>
        static void AddBar(float[] samples, float start, float frequency, float decay, float gain,
            float bendCents = 0f, float bendTime = 1f)
        {
            int first = Count(start);
            float phase = 0f;
            for (int i = first; i < samples.Length; i++)
            {
                float t = Time(i - first);
                float bend = bendCents > 0f ? Mathf.Pow(2f, -bendCents * Mathf.Clamp01(t / bendTime) / 1200f) : 1f;
                phase += Tau * frequency * bend / SampleRate;

                float bar = Mathf.Sin(phase)
                    + 0.35f * Mathf.Sin(4f * phase) * Mathf.Exp(-t / 0.025f)
                    + 0.12f * Mathf.Sin(10f * phase) * Mathf.Exp(-t / 0.01f);
                float attack = Mathf.Clamp01(t / 0.003f);
                samples[i] += bar * attack * Mathf.Exp(-t / decay) * gain;
            }
        }

        static float[] Buffer(float seconds)
        {
            return new float[Count(seconds)];
        }

        static int Count(float seconds)
        {
            return Mathf.RoundToInt(seconds * SampleRate);
        }

        static float Time(int sample)
        {
            return sample / (float)SampleRate;
        }

        static float Noise()
        {
            return (float)(random.NextDouble() * 2.0 - 1.0);
        }

        /// <summary>Filtre passe-bas à un pôle : y += a × (x − y).</summary>
        static float LowPass(float input, float frequency, ref float state)
        {
            state += (1f - Mathf.Exp(-Tau * frequency / SampleRate)) * (input - state);
            return state;
        }

        /// <summary>Filtre passe-haut : le signal moins sa partie grave.</summary>
        static float HighPass(float input, float frequency, ref float state)
        {
            return input - LowPass(input, frequency, ref state);
        }

        /// <summary>Normalise le son à 0,9 en crête, le termine par un fondu de 5 ms et en fait un clip.</summary>
        static AudioClip Finish(string name, float[] samples)
        {
            float peak = 0f;
            foreach (float sample in samples)
            {
                peak = Mathf.Max(peak, Mathf.Abs(sample));
            }

            float gain = peak > 1e-5f ? 0.9f / peak : 1f;
            int fade = Count(0.005f);
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] *= gain * Mathf.Clamp01((samples.Length - 1 - i) / (float)fade);
            }

            AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
