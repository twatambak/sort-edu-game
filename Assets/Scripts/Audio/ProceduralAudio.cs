using UnityEngine;

namespace OndeVai.Audio
{
    /// <summary>
    /// Synthesizes soft, child-friendly placeholder sounds so the game has audio feedback before
    /// final assets exist. Every clip can be replaced by an authored clip in <see cref="GameAudio"/>.
    /// </summary>
    public static class ProceduralAudio
    {
        private const int SampleRate = 22050;

        /// <summary>A note of a synthesized sequence. Frequency 0 is a rest.</summary>
        public readonly struct Note
        {
            public readonly float Frequency;
            public readonly float Duration;

            public Note(float frequency, float duration)
            {
                Frequency = frequency;
                Duration = duration;
            }
        }

        public static AudioClip CreatePop() => CreateSweep("sfx_pop", 420f, 900f, 0.09f, 0.6f);

        public static AudioClip CreatePickup() => CreateSweep("sfx_pickup", 520f, 780f, 0.07f, 0.45f);

        public static AudioClip CreateClick() => CreateSweep("sfx_click", 900f, 700f, 0.05f, 0.4f);

        public static AudioClip CreateCorrect() => CreateSequence("sfx_correct", 0.55f, new Note(784f, 0.09f), new Note(1047f, 0.09f), new Note(1319f, 0.18f));

        public static AudioClip CreateWrong() => CreateSequence("sfx_wrong", 0.5f, new Note(330f, 0.12f), new Note(247f, 0.22f));

        public static AudioClip CreateTick() => CreateSweep("sfx_tick", 1200f, 1100f, 0.04f, 0.35f);

        public static AudioClip CreateWin() => CreateSequence("sfx_win", 0.55f,
            new Note(523f, 0.12f), new Note(659f, 0.12f), new Note(784f, 0.12f), new Note(1047f, 0.12f), new Note(0f, 0.06f), new Note(784f, 0.1f), new Note(1047f, 0.35f));

        public static AudioClip CreateLose() => CreateSequence("sfx_lose", 0.45f,
            new Note(523f, 0.16f), new Note(440f, 0.16f), new Note(392f, 0.32f));

        /// <summary>Calm looping pentatonic melody with a soft bass line.</summary>
        public static AudioClip CreateMusicLoop()
        {
            float[] melody = { 523f, 587f, 659f, 784f, 659f, 587f, 523f, 440f, 523f, 659f, 784f, 880f, 784f, 659f, 587f, 523f };
            float[] bass = { 131f, 131f, 175f, 175f, 196f, 196f, 131f, 131f };
            const float beat = 0.42f;

            int beatSamples = Mathf.RoundToInt(beat * SampleRate);
            float[] data = new float[beatSamples * melody.Length * 2];
            for (int i = 0; i < melody.Length * 2; i++)
            {
                float melodyFrequency = i % 2 == 0 ? melody[(i / 2) % melody.Length] : 0f;
                float bassFrequency = bass[(i / 4) % bass.Length];
                WriteTone(data, i * beatSamples, beatSamples * 2, melodyFrequency, 0.12f);
                if (i % 4 == 0)
                    WriteTone(data, i * beatSamples, beatSamples * 4, bassFrequency, 0.1f);
            }

            return CreateClip("music_loop", data);
        }

        private static AudioClip CreateSweep(string clipName, float startFrequency, float endFrequency, float duration, float volume)
        {
            int length = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
            float[] data = new float[length];
            float phase = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / length;
                float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
                phase += 2f * Mathf.PI * frequency / SampleRate;
                data[i] = Mathf.Sin(phase) * Envelope(t) * volume;
            }

            return CreateClip(clipName, data);
        }

        private static AudioClip CreateSequence(string clipName, float volume, params Note[] notes)
        {
            int total = 0;
            foreach (Note note in notes)
                total += Mathf.RoundToInt(note.Duration * SampleRate);

            float[] data = new float[Mathf.Max(1, total)];
            int offset = 0;
            foreach (Note note in notes)
            {
                int length = Mathf.RoundToInt(note.Duration * SampleRate);
                WriteTone(data, offset, length, note.Frequency, volume);
                offset += length;
            }

            return CreateClip(clipName, data);
        }

        /// <summary>Adds a sine tone with a light octave harmonic and a soft envelope into the buffer.</summary>
        private static void WriteTone(float[] buffer, int start, int length, float frequency, float volume)
        {
            if (frequency <= 0f)
                return;

            for (int i = 0; i < length && start + i < buffer.Length; i++)
            {
                float t = (float)i / length;
                float time = (float)i / SampleRate;
                float wave = Mathf.Sin(2f * Mathf.PI * frequency * time) + 0.25f * Mathf.Sin(4f * Mathf.PI * frequency * time);
                buffer[start + i] += wave * 0.8f * Envelope(t) * volume;
            }
        }

        private static float Envelope(float t)
        {
            const float attack = 0.08f;
            float rise = Mathf.Clamp01(t / attack);
            float fall = 1f - Mathf.Clamp01((t - attack) / (1f - attack));
            return rise * fall * fall;
        }

        private static AudioClip CreateClip(string clipName, float[] data)
        {
            AudioClip clip = AudioClip.Create(clipName, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
