using UnityEngine;

public sealed class YarnMatchMusic : MonoBehaviour
{
    private const string EnabledKey = "YarnMatch.MusicEnabled";
    private const string VolumeKey = "YarnMatch.MusicVolume";
    private AudioSource _source;
    public bool MusicEnabled { get; private set; }
    public float Volume { get; private set; }

    private void Awake()
    {
        MusicEnabled = PlayerPrefs.GetInt(EnabledKey, 1) != 0;
        Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.45f));
        GameObject player = new GameObject("Background Music");
        player.transform.SetParent(transform, false);
        _source = player.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop = true;
        _source.spatialBlend = 0f;
        _source.volume = Volume;
        _source.mute = !MusicEnabled;
        _source.clip = Resources.Load<AudioClip>("YarnMatch/Audio/QuietStitches");
        _source.Play();
    }

    public void SetEnabled(bool value)
    {
        MusicEnabled = value;
        _source.mute = !value;
        PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetVolume(float value)
    {
        Volume = Mathf.Clamp01(value);
        _source.volume = Volume;
        PlayerPrefs.SetFloat(VolumeKey, Volume);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) PlayerPrefs.Save();
    }

    private void OnDestroy() => PlayerPrefs.Save();
}
