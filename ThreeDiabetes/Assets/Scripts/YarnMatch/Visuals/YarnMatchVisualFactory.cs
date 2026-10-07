using System.Collections.Generic;
using UnityEngine;

public static class YarnMatchVisualFactory
{
    private static readonly Dictionary<int, Sprite> CellSprites = new Dictionary<int, Sprite>();
    private static readonly Dictionary<int, Sprite> SpoolSprites = new Dictionary<int, Sprite>();
    private static readonly Dictionary<int, Sprite> ThreadSprites = new Dictionary<int, Sprite>();
    private static readonly Dictionary<YarnMatchTunnelDirection, Sprite> TunnelSprites = new Dictionary<YarnMatchTunnelDirection, Sprite>();
    private static readonly Dictionary<int, Sprite> FreezeSprites = new Dictionary<int, Sprite>();
    private static readonly Dictionary<int, Sprite> ChainSprites = new Dictionary<int, Sprite>();
    private static Sprite _solidSprite;
    private static Sprite _panelSprite;
    private static Sprite _circleSprite;
    private static Texture2D _cellPatternTexture;
    private static Sprite _cellPatternSprite;

    public static Sprite GetSolidSprite()
    {
        if (_solidSprite == null)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            _solidSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);
        }
        return _solidSprite;
    }

    public static Sprite GetPanelSprite()
    {
        if (_panelSprite == null)
        {
            Texture2D texture = CreateRoundedTexture(64, 64, 14);
            _panelSprite = Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(18f, 18f, 18f, 18f));
        }
        return _panelSprite;
    }

    public static Sprite GetCircleSprite()
    {
        if (_circleSprite == null)
        {
            Texture2D texture = CreateRoundedTexture(64, 64, 32);
            _circleSprite = Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 100f);
        }
        return _circleSprite;
    }

    public static Sprite GetTunnelSprite(YarnMatchTunnelDirection direction)
    {
        if (!TunnelSprites.TryGetValue(direction, out Sprite sprite))
        {
            string resourcePath = "YarnMatch/Tunnels/YarnMatchTunnel_" + direction;
            sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                // The tunnel artwork is a supplied reference asset; do not invent a fallback shape.
                sprite = GetPanelSprite();
            }
            TunnelSprites.Add(direction, sprite);
        }
        return sprite;
    }

    public static Sprite GetFreezeSprite(int hitsRemaining)
    {
        int state = Mathf.Clamp(3 - hitsRemaining, 0, 3);
        if (!FreezeSprites.TryGetValue(state, out Sprite sprite))
        {
            string resourcePath = "YarnMatch/Mechanics/mechanic_freeze_state_" + state;
            sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                sprite = GetSolidSprite();
            }
            FreezeSprites.Add(state, sprite);
        }
        return sprite;
    }

    public static Sprite GetChainSprite(bool horizontal)
    {
        return GetChainSprite(horizontal, false);
    }

    public static Sprite GetChainSprite(bool horizontal, bool freezeCombo)
    {
        int key = (horizontal ? 1 : 0) | (freezeCombo ? 2 : 0);
        if (!ChainSprites.TryGetValue(key, out Sprite sprite))
        {
            string resourcePath = freezeCombo && horizontal
                ? "YarnMatch/Mechanics/mechanic_chain_freeze_combo"
                : "YarnMatch/Mechanics/mechanic_chain_" + (horizontal ? "horizontal" : "vertical");
            sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                sprite = GetSolidSprite();
            }
            ChainSprites.Add(key, sprite);
        }
        return sprite;
    }

    public static Sprite GetCellSprite(Color color)
    {
        int key = ColorUtility.ToHtmlStringRGB(color).GetHashCode();
        if (!CellSprites.TryGetValue(key, out Sprite sprite))
        {
            Texture2D texture = CreateYarnTexture(color);
            sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            CellSprites.Add(key, sprite);
        }
        return sprite;
    }

    public static Texture2D GetCellPatternTexture()
    {
        if (_cellPatternTexture == null)
        {
            _cellPatternTexture = CreateKnitPatternTexture();
        }

        return _cellPatternTexture;
    }

    public static Sprite GetCellPatternSprite()
    {
        if (_cellPatternSprite == null)
        {
            Texture2D texture = GetCellPatternTexture();
            _cellPatternSprite = Sprite.Create(texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
        }

        return _cellPatternSprite;
    }

    public static Sprite GetSpoolSprite(Color color)
    {
        int key = ColorUtility.ToHtmlStringRGB(color).GetHashCode();
        if (!SpoolSprites.TryGetValue(key, out Sprite sprite))
        {
            Texture2D texture = CreateSpoolTexture(color);
            sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            SpoolSprites.Add(key, sprite);
        }
        return sprite;
    }

    public static Sprite GetThreadSprite(Color color)
    {
        int key = ColorUtility.ToHtmlStringRGB(color).GetHashCode();
        if (!ThreadSprites.TryGetValue(key, out Sprite sprite))
        {
            Texture2D texture = CreateThreadTexture(color);
            sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            ThreadSprites.Add(key, sprite);
        }
        return sprite;
    }

    private static Texture2D CreateRoundedTexture(int width, int height, int radius)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - halfWidth) - (halfWidth - radius);
                float dy = Mathf.Abs(y + 0.5f - halfHeight) - (halfHeight - radius);
                float distance = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f)) + Mathf.Min(Mathf.Max(dx, dy), 0f) - radius;
                pixels[y * width + x] = distance < 0f ? Color.white : Color.clear;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    private static Texture2D CreateYarnTexture(Color baseColor)
    {
        const int size = 72;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float wave = SoftWaveValue(x, y);
                Color shadow = Color.Lerp(baseColor, Color.black, 0.08f);
                Color highlight = Color.Lerp(baseColor, Color.white, 0.06f);
                Color color = Color.Lerp(shadow, highlight, wave);
                pixels[y * size + x] = new Color(color.r, color.g, color.b, 1f);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    private static Texture2D CreateKnitPatternTexture()
    {
        const int size = 72;
        // A baked edge keeps every cell readable on the light board backdrop,
        // including white and cream yarn colors; the cell tint multiplies this pattern.
        const int edgeWidth = 5;
        Color edgeColor = new Color(0.24f, 0.28f, 0.36f);
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float wave = SoftWaveValue(x, y);
                Color color = Color.Lerp(new Color(0.82f, 0.90f, 0.93f), Color.white, wave);
                int edgeDistance = Mathf.Min(Mathf.Min(x, y), Mathf.Min(size - 1 - x, size - 1 - y));
                if (edgeDistance < edgeWidth)
                {
                    float depth = 1f - edgeDistance / (float)edgeWidth;
                    color = Color.Lerp(color, edgeColor, Mathf.Clamp01(depth * 1.2f));
                }
                pixels[y * size + x] = new Color(color.r, color.g, color.b, 1f);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Repeat;
        return texture;
    }

    private static float SoftWaveValue(int x, int y)
    {
        float wave = Mathf.Sin(y * 0.36f + Mathf.Sin(x * 0.09f) * 1.25f);
        float ridge = Mathf.Pow(Mathf.Abs(wave), 5.5f);
        return Mathf.Clamp01(0.56f + ridge * 0.44f);
    }

    private static Texture2D CreateThreadTexture(Color baseColor)
    {
        const int width = 72;
        const int height = 14;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - halfWidth) - (halfWidth - 5f);
                float dy = Mathf.Abs(y + 0.5f - halfHeight) - (halfHeight - 4f);
                float distance = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f)) + Mathf.Min(Mathf.Max(dx, dy), 0f) - 4f;
                if (distance > 1f)
                {
                    pixels[y * width + x] = Color.clear;
                    continue;
                }

                float edge = Mathf.Clamp01(-distance / 4f);
                Color color = Color.Lerp(baseColor * 0.62f, baseColor, edge);
                if (Mathf.Sin(x * 0.62f + y * 1.1f) > 0.55f)
                {
                    color = Color.Lerp(color, Color.white, 0.16f);
                }
                pixels[y * width + x] = new Color(color.r, color.g, color.b, 1f);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    private static Texture2D CreateSpoolTexture(Color baseColor)
    {
        const int width = 82;
        const int height = 92;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
        // Pale yarn sits on near-white tiles, so every spool gets a baked dark rim.
        // Light colors use a wider stroke to keep white and cream readable.
        float luminance = baseColor.r * 0.2126f + baseColor.g * 0.7152f + baseColor.b * 0.0722f;
        int rim = luminance >= 0.72f ? 3 : 2;
        Color rimColor = new Color(0.16f, 0.19f, 0.28f, 1f);

        bool Inside(int px, int py)
        {
            Vector2 point = new Vector2(px + 0.5f, py + 0.5f);
            bool body = py > 18f && py < 75f && Mathf.Abs(point.x - center.x) <= 25f;
            float topEllipse = ((point.x - center.x) * (point.x - center.x)) / (28f * 28f) + ((point.y - 18f) * (point.y - 18f)) / (10f * 10f);
            float bottomEllipse = ((point.x - center.x) * (point.x - center.x)) / (28f * 28f) + ((point.y - 75f) * (point.y - 75f)) / (10f * 10f);
            return body || topEllipse <= 1f || bottomEllipse <= 1f;
        }

        bool OnRim(int px, int py)
        {
            int radiusSquared = rim * rim;
            for (int offsetY = -rim; offsetY <= rim; offsetY++)
            {
                for (int offsetX = -rim; offsetX <= rim; offsetX++)
                {
                    if (offsetX * offsetX + offsetY * offsetY > radiusSquared) continue;
                    int sampleX = px + offsetX;
                    int sampleY = py + offsetY;
                    if (sampleX < 0 || sampleX >= width || sampleY < 0 || sampleY >= height
                        || !Inside(sampleX, sampleY))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                bool body = y > 18f && y < 75f && Mathf.Abs(point.x - center.x) <= 25f;
                float topEllipse = ((point.x - center.x) * (point.x - center.x)) / (28f * 28f) + ((point.y - 18f) * (point.y - 18f)) / (10f * 10f);
                float bottomEllipse = ((point.x - center.x) * (point.x - center.x)) / (28f * 28f) + ((point.y - 75f) * (point.y - 75f)) / (10f * 10f);
                if (!body && topEllipse > 1f && bottomEllipse > 1f)
                {
                    pixels[y * width + x] = Color.clear;
                    continue;
                }

                if (OnRim(x, y))
                {
                    pixels[y * width + x] = rimColor;
                    continue;
                }

                Color color = baseColor;
                if (body && Mathf.Abs(Mathf.Sin(y * 0.55f)) > 0.72f)
                {
                    color *= 0.76f;
                }
                if (topEllipse <= 1f)
                {
                    color = Color.Lerp(baseColor, Color.white, 0.30f);
                    float hole = ((point.x - center.x) * (point.x - center.x)) / 100f + ((point.y - 18f) * (point.y - 18f)) / 16f;
                    if (hole < 1f)
                    {
                        color = new Color(0.12f, 0.08f, 0.16f, 1f);
                    }
                }
                if (bottomEllipse <= 1f)
                {
                    color = Color.Lerp(baseColor * 0.76f, baseColor, 0.45f);
                }
                pixels[y * width + x] = new Color(color.r, color.g, color.b, 1f);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }
}
