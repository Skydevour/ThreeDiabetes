using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

internal sealed class YarnMatchEffectPool
{
    private readonly Transform _root;
    private readonly Queue<Image> _flyingImages = new Queue<Image>();
    private readonly Dictionary<Image, YarnMatchSpoolCapacityBadge> _flyingBadges = new Dictionary<Image, YarnMatchSpoolCapacityBadge>();
    private readonly Queue<Image> _trailImages = new Queue<Image>();

    internal YarnMatchEffectPool(Transform root)
    {
        _root = root;
    }

    internal void Build()
    {
        for (int index = 0; index < 8; index++)
        {
            _flyingImages.Enqueue(CreateFlyingImage());
        }
        for (int index = 0; index < 24; index++)
        {
            _trailImages.Enqueue(CreatePooledImage("Pooled Thread"));
        }
    }

    internal Image GetFlyingImage(int capacity)
    {
        Image image = _flyingImages.Count > 0 ? _flyingImages.Dequeue() : CreateFlyingImage();
        _flyingBadges[image].SetCapacity(capacity);
        image.rectTransform.localScale = Vector3.one;
        image.gameObject.SetActive(true);
        return image;
    }

    internal Image GetTrailImage()
    {
        Image image = _trailImages.Count > 0 ? _trailImages.Dequeue() : CreatePooledImage("Pooled Thread");
        image.gameObject.SetActive(true);
        return image;
    }

    internal YarnMatchTrailVisual GetTrailVisual(Color color)
    {
        YarnMatchTrailVisual trail = new YarnMatchTrailVisual();
        for (int index = 0; index < 14; index++)
        {
            Image segment = GetTrailImage();
            segment.color = new Color(color.r, color.g, color.b, 0.88f);
            segment.rectTransform.sizeDelta = new Vector2(10f, 5f);
            trail.Segments.Add(segment.rectTransform);
        }
        return trail;
    }

    internal void ReleaseFlyingImage(Image image)
    {
        image.gameObject.SetActive(false);
        _flyingImages.Enqueue(image);
    }

    internal void ReleaseTrailVisual(YarnMatchTrailVisual trail)
    {
        for (int index = 0; index < trail.Segments.Count; index++)
        {
            Image image = trail.Segments[index].GetComponent<Image>();
            image.gameObject.SetActive(false);
            _trailImages.Enqueue(image);
        }
    }

    internal void Clear()
    {
        _flyingImages.Clear();
        _trailImages.Clear();
        if (_root == null)
        {
            return;
        }

        for (int index = 0; index < _root.childCount; index++)
        {
            Transform child = _root.GetChild(index);
            if (child != null)
            {
                child.gameObject.SetActive(false);
                Image image = child.GetComponent<Image>();
                if (image != null && child.name.Contains("Pooled Yarn"))
                {
                    _flyingImages.Enqueue(image);
                }
                else if (image != null && child.name.Contains("Pooled Thread"))
                {
                    _trailImages.Enqueue(image);
                }
            }
        }
    }

    private Image CreateFlyingImage()
    {
        Image image = CreatePooledImage("Pooled Yarn");
        _flyingBadges.Add(image, new YarnMatchSpoolCapacityBadge(image.transform, 62f));
        return image;
    }

    private Image CreatePooledImage(string name)
    {
        Image image = YarnMatchUiPrimitives.CreateImage(name, _root, YarnMatchVisualFactory.GetSolidSprite(), Color.white, new Vector2(20f, 20f), Vector2.zero, false);
        image.raycastTarget = false;
        image.gameObject.SetActive(false);
        return image;
    }
}
