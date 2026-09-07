using System;
using System.IO;

// Offline asset production. The game only loads the resulting local clip.
public static class BakeBackgroundMusic
{
    public static void Bake(string destination)
    {
        const int rate = 44100;
        const double beat = 60.0 / 72.0;
        int length = (int)Math.Round(rate * beat * 64);
        double[] left = new double[length], right = new double[length];
        int[][] harmony = {
            new[] { 48, 55, 59, 64 }, new[] { 45, 52, 55, 60 },
            new[] { 41, 48, 52, 57 }, new[] { 43, 50, 55, 60 },
            new[] { 48, 55, 59, 64 }, new[] { 45, 52, 57, 60 },
            new[] { 41, 48, 52, 57 }, new[] { 43, 50, 55, 59 }
        };
        int[][] melody = {
            new[] { 76, 74, 71, 67 }, new[] { 72, 71, 69, 64 },
            new[] { 69, 72, 76, 72 }, new[] { 74, 72, 69, 67 },
            new[] { 71, 74, 76, 79 }, new[] { 76, 72, 71, 69 },
            new[] { 72, 69, 67, 64 }, new[] { 67, 69, 71, 74 }
        };
        for (int bar = 0; bar < 16; bar++)
        {
            int[] chord = harmony[bar % 8];
            for (int n = 0; n < chord.Length; n++)
                Note(left, right, bar * 4 * beat, 5.8 * beat, chord[n],
                    0.032, 0.25 + n * 0.16, true, rate);
            for (int n = 0; n < 4; n++)
            {
                int tone = chord[(n + bar) % chord.Length] + 12;
                Note(left, right, (bar * 4 + n + 0.08) * beat, 2.5 * beat,
                    tone, 0.037, n % 2 == 0 ? 0.35 : 0.65, false, rate);
            }
            int phrase = (bar / 2) % melody.Length;
            int offset = bar % 2 * 2;
            for (int n = 0; n < 2; n++)
                Note(left, right, (bar * 4 + n * 2 + 0.12) * beat, 3.5 * beat,
                    melody[phrase][offset + n], 0.067, 0.5, false, rate);
        }
        double[] dryL = (double[])left.Clone(), dryR = (double[])right.Clone();
        for (int i = 0; i < length; i++)
        {
            left[i] += dryR[(i + length - (int)(rate * 0.31)) % length] * 0.19
                + dryL[(i + length - (int)(rate * 0.53)) % length] * 0.09;
            right[i] += dryL[(i + length - (int)(rate * 0.37)) % length] * 0.19
                + dryR[(i + length - (int)(rate * 0.59)) % length] * 0.09;
        }
        double peak = 0;
        for (int i = 0; i < length; i++)
            peak = Math.Max(peak, Math.Max(Math.Abs(left[i]), Math.Abs(right[i])));
        double gain = 0.68 / peak;
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        using (var writer = new BinaryWriter(File.Create(destination)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + length * 4);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16); writer.Write((short)1); writer.Write((short)2);
            writer.Write(rate); writer.Write(rate * 4); writer.Write((short)4); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(length * 4);
            for (int i = 0; i < length; i++)
            {
                writer.Write((short)(left[i] * gain * short.MaxValue));
                writer.Write((short)(right[i] * gain * short.MaxValue));
            }
        }
    }

    private static void Note(double[] left, double[] right, double start,
        double duration, int midi, double volume, double pan, bool pad, int rate)
    {
        double frequency = 440 * Math.Pow(2, (midi - 69) / 12.0);
        int offset = (int)(start * rate), count = (int)(duration * rate);
        for (int i = 0; i < count; i++)
        {
            double t = i / (double)rate;
            double attack = 1 - Math.Exp(-t / (pad ? 0.32 : 0.018));
            double decay = Math.Exp(-t / (pad ? 2.2 : 0.85));
            double release = Math.Min(1, (duration - t) / 0.2);
            double phase = Math.PI * 2 * frequency * t;
            double value = Math.Sin(phase) + (pad ? 0.09 : 0.22) * Math.Sin(2 * phase)
                * Math.Exp(-t * 1.8) + 0.045 * Math.Sin(3 * phase) * Math.Exp(-t * 3.5);
            value *= attack * decay * release * volume;
            int target = (offset + i) % left.Length;
            left[target] += value * Math.Sqrt(1 - pan);
            right[target] += value * Math.Sqrt(pan);
        }
    }
}
