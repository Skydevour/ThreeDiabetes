using UnityEngine;

public static class YarnMatchBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Launch()
    {
        if (Object.FindFirstObjectByType<YarnMatchGame>() != null)
        {
            return;
        }

        GameObject gameObject = new GameObject("Yarn Match Game");
        gameObject.AddComponent<YarnMatchGame>();
    }
}
