using System.Collections.Generic;
using UnityEngine;

internal sealed class YarnMatchPoolBadgeLayer : MonoBehaviour
{
    private sealed class Binding
    {
        internal RectTransform Source;
        internal RectTransform Root;
    }

    private readonly List<Binding> _bindings = new List<Binding>();

    internal YarnMatchSpoolCapacityBadge Attach(RectTransform source, float cellSize)
    {
        RectTransform root = YarnMatchUiPrimitives.CreateChild("Spool Capacity", transform)
            .GetComponent<RectTransform>();
        root.sizeDelta = source.sizeDelta;
        Binding binding = new Binding { Source = source, Root = root };
        _bindings.Add(binding);
        Synchronize(binding);
        return new YarnMatchSpoolCapacityBadge(root, cellSize);
    }

    private void LateUpdate()
    {
        // One shared layer keeps numbers above chains without a separate Canvas per spool.
        for (int index = 0; index < _bindings.Count; index++)
        {
            Synchronize(_bindings[index]);
        }
    }

    private static void Synchronize(Binding binding)
    {
        bool visible = binding.Source.gameObject.activeSelf;
        if (binding.Root.gameObject.activeSelf != visible)
            binding.Root.gameObject.SetActive(visible);
        if (!visible) return;

        // Both roots share the pool's centered coordinate space. Only moving badges dirty the Canvas.
        if (binding.Root.anchoredPosition != binding.Source.anchoredPosition)
            binding.Root.anchoredPosition = binding.Source.anchoredPosition;
        if (binding.Root.localScale != binding.Source.localScale)
            binding.Root.localScale = binding.Source.localScale;
    }
}
