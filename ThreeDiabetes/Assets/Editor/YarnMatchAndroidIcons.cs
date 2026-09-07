#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class YarnMatchAndroidIcons
{
    private const string Folder = "Assets/Art/YarnMatch/AppIcon";
    private const string IconPath = Folder + "/YarnMatchIcon.png";
    private const string ForegroundPath = Folder + "/YarnMatchIconForeground.png";
    private const string BackgroundPath = Folder + "/YarnMatchIconBackground.png";
    private const int Size = 512;
    private static readonly Color Background = new Color(0.94f, 0.98f, 0.97f);
    private static readonly Color Red = new Color(0.96f, 0.22f, 0.30f);

    [MenuItem("Yarn Match/Update App Icon")]
    public static void GenerateAndApply()
    {
        Directory.CreateDirectory(Folder);
        Save(IconPath, Render(Background, 1f));
        Save(ForegroundPath, Render(Color.clear, 0.68f));
        Color[] background = new Color[Size * Size];
        Array.Fill(background, Background);
        Save(BackgroundPath, background);
        Apply();
    }

    public static void Apply()
    {
        if (!File.Exists(IconPath) || !File.Exists(ForegroundPath) || !File.Exists(BackgroundPath))
        {
            GenerateAndApply();
            return;
        }
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        Texture2D foreground = AssetDatabase.LoadAssetAtPath<Texture2D>(ForegroundPath);
        Texture2D background = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        foreach (PlatformIconKind kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
        {
            PlatformIcon[] slots = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
            foreach (PlatformIcon slot in slots)
            {
                if (slot.minLayerCount > 1)
                    slot.SetTextures(new[] { background, foreground });
                else
                    slot.SetTexture(icon);
            }
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, slots);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Yarn Match Android launcher icons configured: " + IconPath);
    }

    private static Color[] Render(Color background, float scale)
    {
        Color[] pixels = new Color[Size * Size];
        Array.Fill(pixels, background);
        Color[] colors = { Red, new Color(1f, 0.75f, 0.05f), new Color(0.10f, 0.72f, 0.38f),
            new Color(0.95f, 0.20f, 0.58f), Red, new Color(0.03f, 0.70f, 0.90f) };
        for (int i = 0; i < colors.Length; i++)
        {
            float height = i == 4 ? 64f : 96f;
            Vector2 center = new Vector2(150f + i % 3 * 106f, 382f - i / 3 * 106f);
            if (i == 4) center.y += 16f;
            Texture2D tile = YarnMatchVisualFactory.GetCellSprite(colors[i]).texture;
            Blit(pixels, tile, center + new Vector2(3f, -5f), new Vector2(96f, height),
                0f, scale, new Color(0.18f, 0.28f, 0.30f, 0.16f));
            Blit(pixels, tile, center, new Vector2(96f, height), 0f, scale, Color.white);
        }
        for (int i = 0; i <= 120; i++)
        {
            float t = i / 120f, u = 1f - t;
            Vector2 position = u * u * u * new Vector2(256f, 252f)
                + 3f * u * u * t * new Vector2(185f, 181f)
                + 3f * u * t * t * new Vector2(367f, 227f)
                + t * t * t * new Vector2(289f, 149f);
            Dot(pixels, Transform(position, scale), 6f * scale, Red);
        }
        Blit(pixels, YarnMatchVisualFactory.GetSpoolSprite(colors[5]).texture,
            new Vector2(164f, 124f), new Vector2(138f, 155f), 165f, scale, Color.white);
        Blit(pixels, YarnMatchVisualFactory.GetSpoolSprite(Red).texture,
            new Vector2(286f, 135f), new Vector2(200f, 224f), 190f, scale, Color.white);
        return pixels;
    }

    private static void Blit(Color[] pixels, Texture2D texture, Vector2 center,
        Vector2 size, float angle, float scale, Color tint)
    {
        center = Transform(center, scale);
        size *= scale;
        float cos = Mathf.Cos(angle * Mathf.Deg2Rad), sin = Mathf.Sin(angle * Mathf.Deg2Rad);
        int extent = Mathf.CeilToInt(size.magnitude * 0.5f);
        for (int y = Math.Max(0, (int)center.y - extent); y < Math.Min(Size, center.y + extent); y++)
        for (int x = Math.Max(0, (int)center.x - extent); x < Math.Min(Size, center.x + extent); x++)
        {
            float dx = x + 0.5f - center.x, dy = y + 0.5f - center.y;
            float u = (dx * cos + dy * sin) / size.x + 0.5f;
            float v = (-dx * sin + dy * cos) / size.y + 0.5f;
            if (u < 0 || u > 1 || v < 0 || v > 1) continue;
            Color sample = texture.GetPixelBilinear(u, v) * tint;
            Blend(pixels, y * Size + x, sample);
        }
    }

    private static void Dot(Color[] pixels, Vector2 center, float radius, Color color)
    {
        for (int y = Math.Max(0, (int)(center.y - radius - 1)); y < Math.Min(Size, center.y + radius + 1); y++)
        for (int x = Math.Max(0, (int)(center.x - radius - 1)); x < Math.Min(Size, center.x + radius + 1); x++)
        {
            Color sample = color;
            sample.a *= Mathf.Clamp01(radius + 0.5f - Vector2.Distance(center, new Vector2(x + 0.5f, y + 0.5f)));
            Blend(pixels, y * Size + x, sample);
        }
    }

    private static Vector2 Transform(Vector2 point, float scale)
        => (point - Vector2.one * (Size * 0.5f)) * scale + Vector2.one * (Size * 0.5f);

    private static void Blend(Color[] pixels, int index, Color source)
    {
        if (source.a <= 0f) return;
        Color target = pixels[index];
        float alpha = source.a + target.a * (1f - source.a);
        Color result = (source * source.a + target * (target.a * (1f - source.a))) / alpha;
        result.a = alpha;
        pixels[index] = result;
    }

    private static void Save(string path, Color[] pixels)
    {
        Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = Size;
        importer.SaveAndReimport();
    }
}
#endif
