using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
internal sealed class YarnMatchLevelThumbnail : MonoBehaviour
{
    private Image _image;
    private Texture2D _texture;
    private Sprite _sprite;
    private YarnMatchLevelConfig _round;
    private Task<YarnMatchPatternLayout> _pending;
    private bool _failed;
    internal bool IsReady => _sprite != null;

    internal void Show(YarnMatchLevelConfig round)
    {
        if (_round == round) return;
        if (_image == null) _image = GetComponent<Image>();
        Release();
        _round = round;
    }

    internal bool TryPublish()
    {
        if (_round == null || _sprite != null || _failed) return true;
        if (_pending == null) _pending = _round.PreparePatternAsync();
        if (!_pending.IsCompleted) return false;
        if (_pending.IsFaulted)
        {
            Debug.LogException(_pending.Exception.GetBaseException());
            _failed = true;
            return true;
        }
        YarnMatchPatternLayout layout = _pending.Result;
        _round.RetainPattern(layout);
        Color32[] pixels = new Color32[layout.Width * layout.Height];
        for (int i = 0; i < pixels.Length; i++)
            if (layout.Colors[i] >= 0) pixels[i] = YarnMatchUiTheme.Palette[layout.Colors[i]];

        _texture = new Texture2D(layout.Width, layout.Height, TextureFormat.RGBA32, false)
        {
            name = "Round Preview",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        _texture.SetPixels32(pixels);
        _texture.Apply(false, true);
        _sprite = Sprite.Create(_texture, new Rect(0f, 0f, layout.Width, layout.Height),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        _image.sprite = _sprite;
        _image.preserveAspect = true;
        _image.enabled = true;
        return true;
    }

    internal void Clear() => Release();

    private void Release()
    {
        _round?.ReleasePattern();
        _round = null;
        _pending = null;
        _failed = false;
        if (_image != null)
        {
            _image.sprite = null;
            _image.enabled = false;
        }
        if (_sprite != null) Destroy(_sprite);
        if (_texture != null) Destroy(_texture);
        _sprite = null;
        _texture = null;
    }

    private void OnDestroy() => Release();
}
