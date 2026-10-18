using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Synthesises the game's sound effects in code and saves them as WAV files, so the
/// project needs no downloaded or licensed audio. Called by the scene builder.
/// </summary>
public static class SoundSynth
{
    private const string AudioFolder = "Assets/Audio";
    private const int SampleRate = 44100;

    public struct Clips
    {
        public AudioClip place;
        public AudioClip perfect;
        public AudioClip slice;
        public AudioClip gameOver;
        public AudioClip button;
        public AudioClip introJingle;
        public AudioClip music;
    }

    public static Clips GenerateAll()
    {
        Directory.CreateDirectory(AudioFolder);
        return new Clips
        {
            place = Save("Place", Place()),
            perfect = Save("Perfect", Perfect()),
            slice = Save("Slice", Slice()),
            gameOver = Save("GameOver", GameOver()),
            button = Save("Button", Button()),
            introJingle = Save("IntroJingle", IntroJingle()),
            music = Save("MusicLoop", MusicLoop(), true),
        };
    }

    // ------------------------------------------------------------------ music

    // Plays under the LD Games intro: one marimba note as each block lands,
    // a warm chord when "LD" appears and quick sparkles as "GAMES" rises.
    // Times match the constants in StudioIntro.
    private static float[] IntroJingle()
    {
        var buffer = new float[(int)(4f * SampleRate)];
        AddMallet(buffer, 0.72f, Midi(72), 0.5f, 6f);   // C5
        AddMallet(buffer, 1.02f, Midi(76), 0.5f, 6f);   // E5
        AddMallet(buffer, 1.32f, Midi(79), 0.5f, 6f);   // G5
        foreach (int note in new[] { 60, 64, 67, 72 })  // C major chord
            AddBell(buffer, 1.72f, Midi(note), 0.3f, 1.6f);
        int[] sparkle = { 88, 91, 93, 96, 98 };         // E6 G6 A6 C7 D7
        for (int i = 0; i < sparkle.Length; i++)
            AddMallet(buffer, 2.6f + i * 0.09f, Midi(sparkle[i]), 0.12f, 14f);
        return Finish(buffer, 0.8f, false);
    }

    // Calm 90 BPM loop (C - Am - F - G, twice): soft pads, a gentle bass,
    // marimba arpeggios and a quiet shaker. The tail is folded back onto the
    // start so it loops without a gap.
    private static float[] MusicLoop()
    {
        const float beat = 60f / 90f;
        const float bar = beat * 4f;
        const int bars = 8;
        int[][] chords =
        {
            new[] { 48, 60, 64, 67 },   // C
            new[] { 45, 57, 60, 64 },   // Am
            new[] { 41, 53, 57, 60 },   // F
            new[] { 43, 55, 59, 62 },   // G
        };
        int[] pattern = { 1, 2, 3, 2, 4, 3, 2, 3 };
        var rng = new System.Random(7);

        float length = bars * bar;
        var buffer = new float[(int)((length + 3f) * SampleRate)];
        for (int b = 0; b < bars; b++)
        {
            int[] chord = chords[b % chords.Length];
            float start = b * bar;

            for (int i = 1; i < 4; i++)
                AddPad(buffer, start, bar + 0.6f, Midi(chord[i]), 0.09f);

            AddBass(buffer, start, Midi(chord[0]), 0.35f);
            AddBass(buffer, start + 2f * beat, Midi(chord[0]), 0.25f);

            for (int s = 0; s < 8; s++)
            {
                int idx = pattern[s];
                int note = idx == 4 ? chord[1] + 24 : chord[idx] + 12;
                if (b >= 4 && s % 4 == 3)
                    note += 12;   // second half: a little lift
                float velocity = (s % 2 == 0 ? 0.16f : 0.11f) * (0.9f + 0.2f * (float)rng.NextDouble());
                AddMallet(buffer, start + s * beat * 0.5f, Midi(note), velocity, 9f);
            }

            for (int s = 0; s < 4; s++)
                AddShaker(buffer, start + (s + 0.5f) * beat, 0.03f, rng);
        }

        int loopLength = (int)(length * SampleRate);
        for (int i = loopLength; i < buffer.Length; i++)
            buffer[i - loopLength] += buffer[i];
        var loop = new float[loopLength];
        Array.Copy(buffer, loop, loopLength);
        return Finish(loop, 0.7f, true);
    }

    private static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

    private static void AddMallet(float[] buffer, float start, float freq, float amp, float decay)
    {
        AddVoice(buffer, start, 6f / decay, t =>
        {
            float w = 2f * Mathf.PI * freq * t;
            float attack = Mathf.Clamp01(t / 0.003f);
            float tone = Mathf.Sin(w) + 0.25f * Mathf.Sin(4f * w) * Mathf.Exp(-t * 30f) + 0.15f * Mathf.Sin(2f * w) * Mathf.Exp(-t * 12f);
            return attack * tone * Mathf.Exp(-t * decay) * amp;
        });
    }

    private static void AddBell(float[] buffer, float start, float freq, float amp, float decay)
    {
        AddVoice(buffer, start, 6f / decay, t =>
        {
            float w = 2f * Mathf.PI * freq * t;
            float attack = Mathf.Clamp01(t / 0.02f);
            float tone = Mathf.Sin(w) + 0.5f * Mathf.Sin(2.01f * w) * Mathf.Exp(-t * 2f) + 0.2f * Mathf.Sin(3.98f * w) * Mathf.Exp(-t * 4f);
            return attack * tone * Mathf.Exp(-t * decay) * amp;
        });
    }

    private static void AddPad(float[] buffer, float start, float duration, float freq, float amp)
    {
        AddVoice(buffer, start, duration, t =>
        {
            float attack = Mathf.Clamp01(t / 0.5f);
            float release = Mathf.Clamp01((duration - t) / 0.6f);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * 0.997f * t) + Mathf.Sin(2f * Mathf.PI * freq * 1.003f * t)
                         + 0.3f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t);
            return tone * attack * release * amp;
        });
    }

    private static void AddBass(float[] buffer, float start, float freq, float amp)
    {
        AddVoice(buffer, start, 1.4f, t =>
        {
            float attack = Mathf.Clamp01(t / 0.01f);
            return Mathf.Sin(2f * Mathf.PI * freq * t) * attack * Mathf.Exp(-t * 3f) * amp;
        });
    }

    private static void AddShaker(float[] buffer, float start, float amp, System.Random rng)
    {
        float previous = 0f;
        AddVoice(buffer, start, 0.08f, t =>
        {
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float high = noise - previous;   // keep only the hiss
            previous = noise;
            return high * Mathf.Exp(-t * 60f) * amp;
        });
    }

    private static void AddVoice(float[] buffer, float start, float duration, Func<float, float> sample)
    {
        int first = Mathf.Max(0, (int)(start * SampleRate));
        int count = (int)(duration * SampleRate);
        for (int i = 0; i < count && first + i < buffer.Length; i++)
            buffer[first + i] += sample(i / (float)SampleRate);
    }

    // Normalise; a one-shot also gets a short fade at the end, a loop must not
    private static float[] Finish(float[] data, float peakTarget, bool loop)
    {
        float peak = 0f;
        foreach (float s in data)
            peak = Mathf.Max(peak, Mathf.Abs(s));
        float gain = peak > 0f ? peakTarget / peak : 1f;
        int fade = loop ? 0 : Mathf.Min(data.Length, SampleRate / 50);
        for (int i = 0; i < data.Length; i++)
        {
            float tail = i >= data.Length - fade ? (data.Length - i) / (float)fade : 1f;
            data[i] *= gain * tail;
        }
        return data;
    }

    // Soft wooden "thock": a low tone whose pitch drops quickly, plus a tiny click
    private static float[] Place()
    {
        var rng = new System.Random(1);
        return Render(0.2f, t =>
        {
            float tone = Mathf.Sin(Phase(t, 190f, 110f, 0.12f)) * Mathf.Exp(-t * 28f);
            float click = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 400f) * 0.35f;
            return tone + click;
        });
    }

    // Bright marimba-like note; played higher on every perfect in a row
    private static float[] Perfect()
    {
        const float f = 659.25f; // E5
        return Render(0.7f, t =>
        {
            float attack = Mathf.Clamp01(t / 0.003f);
            float body = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 7f);
            float overtone = Mathf.Sin(2f * Mathf.PI * f * 4f * t) * Mathf.Exp(-t * 30f) * 0.25f;
            float octave = Mathf.Sin(2f * Mathf.PI * f * 2f * t) * Mathf.Exp(-t * 12f) * 0.2f;
            return attack * (body + overtone + octave);
        });
    }

    // Short filtered noise "chop" for the piece being cut off
    private static float[] Slice()
    {
        var rng = new System.Random(2);
        float low = 0f, prev = 0f;
        return Render(0.16f, t =>
        {
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            low += (noise - low) * 0.35f;       // soften the hiss
            float band = low - prev * 0.6f;     // remove the rumble
            prev = low;
            return band * Mathf.Exp(-t * 26f) * 1.6f;
        });
    }

    // Two descending notes with a soft fade, gentle rather than harsh
    private static float[] GameOver()
    {
        return Render(0.9f, t =>
        {
            float first = Note(t, 0f, 392f, 0.35f);      // G4
            float second = Note(t, 0.28f, 261.63f, 0.6f); // C4
            return first + second;
        });
    }

    // Quick rising pop for buttons
    private static float[] Button()
    {
        return Render(0.08f, t =>
        {
            float tone = Mathf.Sin(Phase(t, 520f, 880f, 0.06f));
            return tone * Mathf.Exp(-t * 45f) * Mathf.Clamp01(t / 0.002f);
        });
    }

    // Triangle-ish note starting at `start`, with its own decay
    private static float Note(float t, float start, float freq, float length)
    {
        float local = t - start;
        if (local < 0f || local > length) return 0f;
        float attack = Mathf.Clamp01(local / 0.005f);
        float release = Mathf.Clamp01((length - local) / 0.08f);
        float x = 2f * Mathf.PI * freq * local;
        float tone = Mathf.Sin(x) + Mathf.Sin(3f * x) / 9f;
        return tone * attack * release * Mathf.Exp(-local * 3f) * 0.8f;
    }

    // Phase of a tone gliding linearly from `from` to `to` Hz over `glide` seconds
    private static float Phase(float t, float from, float to, float glide)
    {
        float g = Mathf.Min(t, glide);
        float k = (to - from) / glide;
        float phase = from * g + 0.5f * k * g * g;
        if (t > glide)
            phase += to * (t - glide);
        return 2f * Mathf.PI * phase;
    }

    private static float[] Render(float seconds, Func<float, float> sample)
    {
        int count = Mathf.CeilToInt(seconds * SampleRate);
        var data = new float[count];
        float peak = 0f;
        for (int i = 0; i < count; i++)
        {
            data[i] = sample(i / (float)SampleRate);
            peak = Mathf.Max(peak, Mathf.Abs(data[i]));
        }

        // Normalise and fade the last few milliseconds so nothing clicks
        float gain = peak > 0f ? 0.8f / peak : 1f;
        int fade = Mathf.Min(count, SampleRate / 200);
        for (int i = 0; i < count; i++)
        {
            float tail = i >= count - fade ? (count - i) / (float)fade : 1f;
            data[i] *= gain * tail;
        }
        return data;
    }

    private static AudioClip Save(string name, float[] samples, bool isMusic = false)
    {
        string path = AudioFolder + "/" + name + ".wav";
        WriteWav(path, samples);
        AssetDatabase.ImportAsset(path);

        var importer = (AudioImporter)AssetImporter.GetAtPath(path);
        importer.forceToMono = true;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        // Short effects stay uncompressed in memory for instant playback;
        // the long music loop is Vorbis-compressed to keep the app small.
        settings.loadType = isMusic ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = isMusic ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
        if (isMusic)
            settings.quality = 0.6f;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static void WriteWav(string path, float[] samples)
    {
        using (var stream = new FileStream(path, FileMode.Create))
        using (var writer = new BinaryWriter(stream))
        {
            int dataSize = samples.Length * 2;
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataSize);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });
            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);              // fmt chunk size
            writer.Write((short)1);        // PCM
            writer.Write((short)1);        // mono
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);  // byte rate
            writer.Write((short)2);        // block align
            writer.Write((short)16);       // bits per sample
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataSize);
            foreach (float s in samples)
                writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));
        }
    }
}
