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
        };
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

    private static AudioClip Save(string name, float[] samples)
    {
        string path = AudioFolder + "/" + name + ".wav";
        WriteWav(path, samples);
        AssetDatabase.ImportAsset(path);

        var importer = (AudioImporter)AssetImporter.GetAtPath(path);
        importer.forceToMono = true;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.ADPCM;
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
