using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

internal static class YarnMatchPatternResources
{
    private static Task _loading;
    private static YarnMatchPatternTemplate[] _templates;
    internal static IReadOnlyList<YarnMatchPatternTemplate> Templates => _templates;

    internal static Task LoadAsync()
    {
        if (_loading == null || _loading.IsFaulted) _loading = LoadPackAsync();
        return _loading;
    }

    private static async Task LoadPackAsync()
    {
        ResourceRequest request = Resources.LoadAsync<TextAsset>("YarnMatch/Patterns/PatternTemplates");
        while (!request.isDone) await Task.Yield();
        TextAsset asset = request.asset as TextAsset;
        if (asset == null) throw new InvalidOperationException("The local pattern pack is missing.");
        string json = asset.text;
        try
        {
            // Unity object access stays on the main thread; JSON contains only plain serializable data.
            _templates = await Task.Run(() =>
            {
                var templates = JsonUtility.FromJson<YarnMatchPatternPack>(json).patterns;
                foreach (var template in templates) template.Prepare();
                return templates;
            });
        }
        finally
        {
            Resources.UnloadAsset(asset);
        }
    }
}
