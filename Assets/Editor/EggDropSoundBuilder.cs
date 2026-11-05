using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EggDrop.Editor
{
    internal static class EggDropSoundBuilder
    {
        private const int SampleRate = 44100;
        private const string AudioFolder = "Assets/Audio";

        internal static void CreateSounds()
        {
            Directory.CreateDirectory(AudioFolder);
            Write("Drop", 0.38f, (time, noise) =>
                0.38 * noise * Envelope(time, 0f, 0.38f, 0.025f) * Math.Sin(Math.PI * time / 0.38f)
                + 0.16 * Math.Sin(2 * Math.PI * (380 * time - 270 * time * time)) * Math.Exp(-time * 14));
            Write("Landing", 0.32f, (time, noise) =>
                0.6 * Math.Sin(2 * Math.PI * (110 * time - 80 * time * time)) * Math.Exp(-time * 22)
                + 0.16 * noise * Math.Exp(-time * 45));
            Write("Success", 0.8f, (time, noise) =>
                Bell(time, 0f, 659.25, 0.55f) * 0.4 + Bell(time, 0.16f, 880, 0.6f) * 0.36);
            Write("Collect", 1.5f, (time, noise) =>
                Bell(time, 0f, 523.25, 0.8f) * 0.32 + Bell(time, 0.12f, 659.25, 0.8f) * 0.3
                + Bell(time, 0.24f, 783.99, 0.9f) * 0.3 + Bell(time, 0.4f, 1046.5, 1.05f) * 0.28);
            Write("Crack", 0.85f, (time, noise) =>
                Crunch(time, 0f, noise) * 0.55 + Crunch(time, 0.043f, noise) * 0.25
                + Crunch(time, 0.11f, noise) * 0.2
                + Math.Sin(2 * Math.PI * (150 * time - 58 * time * time)) * Math.Exp(-time * 9) * 0.2);
            Write("Click", 0.09f, (time, noise) => Bell(time, 0f, 990, 0.085f) * 0.35);
            Write("Falling", 1.6f, (time, noise) =>
                (noise * 0.3 + Math.Sin(2 * Math.PI * 117 * time) * 0.08)
                * Envelope(time, 0f, 1.6f, 0.1f) * (0.65 + 0.25 * Math.Sin(2 * Math.PI * time / 1.6)));
            Write("Music", 16f, Music);
            AssetDatabase.Refresh();
            foreach (string file in Directory.GetFiles(AudioFolder, "*.wav"))
            {
                AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(file.Replace('\\', '/'));
                if (importer == null) continue;
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                bool music = file.EndsWith("Music.wav", StringComparison.Ordinal);
                settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
                settings.quality = 0.8f;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }
        }

        private static double Music(float time, double noise)
        {
            double[] roots = { 130.8128, 110, 87.3071, 97.9989 };
            double result = 0;
            for (int beat = 0; beat < 24; beat++)
            {
                float start = beat * (2f / 3f);
                double root = roots[beat / 6];
                int step = beat % 6;
                double ratio = step == 0 || step == 3 ? 2 : step == 1 || step == 4 ? 2.5 : 3;
                result += Bell(time, start, root * ratio, 1.0f) * 0.21;
                if (step == 0)
                {
                    float local = time - start;
                    result += Math.Sin(2 * Math.PI * root * local) * Envelope(time, start, 3.7f, 0.3f) * 0.06;
                    result += Bell(time, start, root, 2.6f) * 0.14;
                }
            }
            return result * Envelope(time, 0f, 16f, 0.06f);
        }

        private static double Bell(float time, float start, double frequency, float length)
        {
            float local = time - start;
            if (local < 0 || local >= length) return 0;
            double phase = 2 * Math.PI * frequency * local;
            return (Math.Sin(phase) + 0.22 * Math.Sin(phase * 2.005) + 0.08 * Math.Sin(phase * 3.99))
                * Math.Exp(-local * 5.5 / length) * Envelope(time, start, length, 0.004f);
        }

        private static double Crunch(float time, float start, double noise)
        {
            float local = time - start;
            if (local < 0) return 0;
            return noise * Math.Exp(-local * 30) * (0.6 + 0.4 * Math.Sin(local * 17000));
        }

        private static double Envelope(float time, float start, float length, float fade)
        {
            float local = time - start;
            return local < 0 || local > length ? 0
                : Math.Min(1, local / fade) * Math.Min(1, (length - local) / fade);
        }

        private static void Write(string name, float length, Func<float, double, double> sample)
        {
            string path = AudioFolder + "/" + name + ".wav";
            if (File.Exists(path)) return;
            int sampleCount = (int)(SampleRate * length);
            var random = new System.Random(1487);
            using var writer = new BinaryWriter(File.Create(path));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + sampleCount * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(sampleCount * 2);
            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)SampleRate;
                double value = sample(time, random.NextDouble() * 2 - 1);
                double edgeFade = Envelope(time, 0, length, 0.002f);
                writer.Write((short)(Math.Tanh(value) * edgeFade * short.MaxValue * 0.85));
            }
        }
    }
}
