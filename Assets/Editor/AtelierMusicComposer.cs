using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KarakuriLabo.Editor
{
    /// <summary>Original score and synthesized instruments. No samples or external music dependencies.</summary>
    public static class AtelierMusicComposer
    {
        public const string ClipPath = "Assets/Audio/ClockworkAfternoon.wav";
        private const int SampleRate = 44100;
        private const double Beat = 60.0 / 96.0;
        private const int Bars = 32;

        // Cmaj9, Am7, Fmaj9, G6, Em7, Am9, Dm9, G6. MIDI notes, four beats per bar.
        private static readonly int[][] Chords =
        {
            new[] { 60, 64, 67, 71, 74 }, new[] { 57, 60, 64, 67, 71 },
            new[] { 53, 57, 60, 64, 67 }, new[] { 55, 59, 62, 67, 69 },
            new[] { 52, 55, 59, 62, 67 }, new[] { 57, 60, 64, 67, 71 },
            new[] { 50, 57, 60, 64, 65 }, new[] { 55, 59, 62, 64, 69 }
        };
        private static readonly int[][] Melody =
        {
            new[] { 76, 74, 71, 67 }, new[] { 72, 76, 79, 76 },
            new[] { 76, 72, 69, 67 }, new[] { 71, 74, 76, 74 },
            new[] { 71, 67, 74, 71 }, new[] { 72, 71, 69, 76 },
            new[] { 77, 76, 72, 69 }, new[] { 71, 69, 67, 74 }
        };

        public static AudioClip EnsureClip()
        {
            if (!File.Exists(ClipPath))
            {
                Compose();
            }
            AssetDatabase.ImportAsset(ClipPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (AudioImporter)AssetImporter.GetAtPath(ClipPath);
            importer.forceToMono = false;
            importer.loadInBackground = false;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.preloadAudioData = false;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.75f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
        }

        [MenuItem("Karakuri Labo/Audio/Regenerate Original Music")]
        public static void Compose()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException("Stop Play before regenerating the music asset.");
            }
            int frames = (int)Math.Round(Bars * 4 * Beat * SampleRate);
            var left = new float[frames];
            var right = new float[frames];
            int[] arp = { 0, 2, 1, 3, 2, 4, 1, 3 };
            double[] phrasing = { 0.5, 1.25, 2.5, 3.25 };
            for (int bar = 0; bar < Bars; bar++)
            {
                int section = bar / 8;
                int[] chord = Chords[bar % 8];
                double start = bar * 4;
                for (int step = 0; step < 8; step++)
                {
                    // Slight swing and varying touch keep the accompaniment from sounding mechanical.
                    double delay = step % 2 == 1 ? 0.035 : 0;
                    AddNote(left, right, start + step * 0.5 + delay, chord[arp[step]],
                        step % 2 == 0 ? 0.065 : 0.046, -0.28, 0);
                }
                AddNote(left, right, start, chord[0] - 12, 0.12, 0, 2);
                AddNote(left, right, start + 2.5, chord[0] - 5, 0.068, 0.08, 2);
                for (int note = 0; note < 4; note++)
                {
                    if ((section == 2 && note % 2 == 1) || (bar % 4 == 3 && note == 3))
                    {
                        continue;
                    }
                    int pitch = Melody[bar % 8][note];
                    if (section == 1 && note == 3)
                    {
                        pitch = chord[3] + 12;
                    }
                    double time = start + phrasing[note] + (section == 3 && note == 0 ? -0.25 : 0);
                    AddNote(left, right, time, pitch, note == 0 ? 0.105 : 0.08, 0.23, 1);
                }
                // A very quiet wooden pulse; leave breathing room every fourth bar.
                if (bar % 4 != 3)
                {
                    AddNote(left, right, start + 1, 84, 0.011, -0.5, 3);
                    AddNote(left, right, start + 3, 81, 0.009, 0.5, 3);
                }
            }
            double peak = 0;
            double sum = 0;
            for (int i = 0; i < frames; i++)
            {
                peak = Math.Max(peak, Math.Max(Math.Abs(left[i]), Math.Abs(right[i])));
            }
            double gain = 0.7 / Math.Max(peak, 0.001);
            Directory.CreateDirectory(Path.GetDirectoryName(ClipPath));
            using (var writer = new BinaryWriter(File.Create(ClipPath)))
            {
                int bytes = frames * 4;
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + bytes);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)2);
                writer.Write(SampleRate);
                writer.Write(SampleRate * 4);
                writer.Write((short)4);
                writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(bytes);
                for (int i = 0; i < frames; i++)
                {
                    double l = left[i] * gain;
                    double r = right[i] * gain;
                    writer.Write((short)Math.Round(l * short.MaxValue));
                    writer.Write((short)Math.Round(r * short.MaxValue));
                    sum += l * l + r * r;
                }
            }
            AssetDatabase.ImportAsset(ClipPath, ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"Composed Clockwork Afternoon: 80 seconds, 96 BPM, stereo; peak 0.7, RMS {Math.Sqrt(sum / (frames * 2)):F4}.");
        }

        private static void AddNote(float[] left, float[] right, double beat, int midi, double level, double pan, int voice)
        {
            double frequency = 440 * Math.Pow(2, (midi - 69) / 12.0);
            double duration = voice == 3 ? 0.13 : voice == 2 ? 1.25 : 2.8;
            int count = (int)(duration * SampleRate);
            int start = (int)Math.Round(beat * Beat * SampleRate);
            double lGain = Math.Sqrt((1 - pan) * 0.5) * level;
            double rGain = Math.Sqrt((1 + pan) * 0.5) * level;
            for (int i = 0; i < count; i++)
            {
                double t = (double)i / SampleRate;
                double phase = 2 * Math.PI * frequency * t;
                double envelope = (1 - Math.Exp(-t * 180)) * Math.Min(1, (duration - t) / 0.15);
                double sample;
                if (voice == 1)
                {
                    sample = Math.Sin(phase) * Math.Exp(-t * 3.2) +
                        0.22 * Math.Sin(phase * 2.01) * Math.Exp(-t * 11) +
                        0.055 * Math.Sin(phase * 3.96) * Math.Exp(-t * 19);
                }
                else if (voice == 2)
                {
                    sample = (Math.Sin(phase) + 0.3 * Math.Sin(phase * 2)) * Math.Exp(-t * 3.5);
                }
                else if (voice == 3)
                {
                    sample = (Math.Sin(phase) + 0.35 * Math.Sin(phase * 1.73)) * Math.Exp(-t * 60);
                }
                else
                {
                    sample = (Math.Sin(phase) + 0.22 * Math.Sin(phase * 2) + 0.07 * Math.Sin(phase * 3)) * Math.Exp(-t * 2.5);
                }
                sample *= envelope;
                // Wrap tails and room reflections across the end: the loop has no silence or cut release.
                Mix(left, right, start + i, sample * lGain, sample * rGain);
                if (voice < 2)
                {
                    Mix(left, right, start + i + 4807, sample * rGain * 0.16, sample * lGain * 0.12);
                    Mix(left, right, start + i + 11025, sample * lGain * 0.07, sample * rGain * 0.1);
                }
            }
        }

        private static void Mix(float[] left, float[] right, int index, double l, double r)
        {
            index %= left.Length;
            left[index] += (float)l;
            right[index] += (float)r;
        }
    }
}
