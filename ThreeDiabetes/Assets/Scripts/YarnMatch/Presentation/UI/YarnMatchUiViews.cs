using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class YarnMatchPoolCellView
{
    internal Image ShadowImage;
    internal Image SlotImage;
    internal Button Button;
    internal Image SpoolImage;
    internal Image FreezeImage;
    internal YarnMatchSpoolCapacityBadge CapacityBadge;
    internal CanvasGroup Group;
    internal Outline Outline;
    internal float BaseAlpha;
}

internal sealed class YarnMatchPoolTokenView
{
    internal Button Button;
    internal Image Image;
    internal CanvasGroup Group;
    internal Outline Outline;
    internal float BaseAlpha;
}

internal sealed class YarnMatchTunnelView
{
    internal TMP_Text CountLabel;
    internal Image CountBadge;
}

internal sealed class YarnMatchChainView
{
    internal RectTransform Rect;
    internal Image Image;
}

internal sealed class YarnMatchRackEntryView
{
    internal int ActiveCollections;
    internal float InFlightProgress;
    internal RectTransform SpoolRect;
    internal Vector3 BaseScale;
    internal Image SpoolImage;
    internal CanvasGroup SpoolGroup;
    internal Image FillImage;
    internal TMP_Text CountLabel;
}

internal sealed class YarnMatchTrailVisual
{
    internal readonly List<RectTransform> Segments = new List<RectTransform>();
}
