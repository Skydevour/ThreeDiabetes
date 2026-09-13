using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

internal static class YarnMatchLevelStore
{
    private const int FormatVersion = 1;
    private const int CachedChapterLimit = 2;
    private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
    private static readonly Dictionary<int, ChapterData> Cache = new Dictionary<int, ChapterData>();
    private static readonly List<int> Recent = new List<int>();
    private static string _directory;

    internal static void Initialize(string persistentPath)
    {
        // Resolve Unity's persistentDataPath on the main thread, before file workers run.
        string directory = Path.Combine(persistentPath, "YarnMatchLevels");
        if (_directory == directory) return;
        _directory = directory;
        Cache.Clear();
        Recent.Clear();
    }

    internal static async Task EnsureLevelsAsync(int highestUnlocked)
    {
        int count = YarnMatchLevelCatalog.GetChapterIndex(highestUnlocked) + 1;
        var missing = await Task.Run(() =>
        {
            var chapters = new List<int>();
            for (int chapter = 1; chapter <= count; chapter++)
                if (!File.Exists(ChapterPath(chapter))) chapters.Add(chapter);
            return chapters;
        });
        foreach (int chapter in missing) await LoadChapterAsync(chapter);
    }

    internal static async Task<YarnMatchLevelSnapshot> GetAsync(int level, bool special = false)
    {
        int chapter = special ? 0 : YarnMatchLevelCatalog.GetChapterIndex(level) + 1;
        ChapterData data = await LoadChapterAsync(chapter);
        return data.Levels[special ? 0 : (level - 1) % YarnMatchLevelCatalog.LevelsPerChapter];
    }

    private static async Task<ChapterData> LoadChapterAsync(int chapter)
    {
        // Callers are on the Unity thread; a cached replay need not wait for a new chapter.
        if (Cache.TryGetValue(chapter, out ChapterData cached))
        {
            Touch(chapter);
            return cached;
        }
        await Gate.WaitAsync();
        try
        {
            if (!Cache.TryGetValue(chapter, out ChapterData data))
            {
                string path = ChapterPath(chapter);
                bool exists = await Task.Run(() => File.Exists(path));
                // Replays do not load the art pack or rerun any generation rules.
                if (!exists && chapter != 0) await YarnMatchPatternResources.LoadAsync();
                data = await Task.Run(() => exists ? Read(path, chapter) : GenerateAndSave(path, chapter));
                Cache.Add(chapter, data);
            }
            Touch(chapter);
            return data;
        }
        finally
        {
            Gate.Release();
        }
    }

    private static void Touch(int chapter)
    {
        Recent.Remove(chapter);
        Recent.Add(chapter);
        if (Recent.Count <= CachedChapterLimit) return;
        Cache.Remove(Recent[0]);
        Recent.RemoveAt(0);
    }

    private static ChapterData Read(string path, int chapter)
    {
        var data = JsonUtility.FromJson<ChapterData>(File.ReadAllText(path));
        int expected = chapter == 0 ? 1 : YarnMatchLevelCatalog.LevelsPerChapter;
        if (data == null || data.Version != FormatVersion || data.Chapter != chapter
            || data.Levels == null || data.Levels.Length != expected)
            throw new InvalidDataException("Unsupported or incomplete saved chapter: " + path);
        return data;
    }

    private static ChapterData GenerateAndSave(string path, int chapter)
    {
        int count = chapter == 0 ? 1 : YarnMatchLevelCatalog.LevelsPerChapter;
        var data = new ChapterData { Version = FormatVersion, Chapter = chapter, Levels = new YarnMatchLevelSnapshot[count] };
        for (int i = 0; i < count; i++)
        {
            YarnMatchLevelConfig config = chapter == 0 ? YarnMatchLevelCatalog.GenerateSpecial()
                : YarnMatchLevelCatalog.Generate((chapter - 1) * YarnMatchLevelCatalog.LevelsPerChapter + i + 1);
            data.Levels[i] = YarnMatchLevelSnapshot.Generate(config);
        }
        Directory.CreateDirectory(_directory);
        // Only complete chapters become visible. No live round ever writes this file.
        string temporary = path + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(stream))
        {
            writer.Write(JsonUtility.ToJson(data));
            writer.Flush();
            stream.Flush(true);
        }
        File.Move(temporary, path);
        return data;
    }

    private static string ChapterPath(int chapter) => Path.Combine(_directory,
        chapter == 0 ? "special.json" : "chapter-" + chapter.ToString("D4") + ".json");

    [Serializable]
    private sealed class ChapterData
    {
        public int Version;
        public int Chapter;
        public YarnMatchLevelSnapshot[] Levels;
    }
}
