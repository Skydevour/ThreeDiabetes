using UnityEngine;

public sealed class YarnMatchAudio : MonoBehaviour
{
    private AudioSource _source;
    private AudioClip _click;
    private AudioClip _collect;
    private AudioClip _hit;
    private AudioClip _tunnel;
    private AudioClip _start;
    private AudioClip _win;
    private AudioClip _fail;

    private void Awake()
    {
        _source = gameObject.GetComponent<AudioSource>();
        if (_source == null)
        {
            _source = gameObject.AddComponent<AudioSource>();
        }

        _source.playOnAwake = false;
        _source.loop = false;
        _source.volume = 0.34f;
        BuildClips();
    }

    public void PlayClick() { Play(_click, 0.72f); }
    public void PlayCollect() { Play(_collect, 0.92f); }
    public void PlayRackHit() { Play(_hit, 0.75f); }
    public void PlayTunnel() { Play(_tunnel, 0.72f); }
    public void PlayStart() { Play(_start, 0.88f); }
    public void PlayWin() { Play(_win, 1f); }
    public void PlayFail() { Play(_fail, 0.9f); }

    private void BuildClips()
    {
        _click = CreateSweep("Yarn Click", 0.055f, 760f, 570f, 0.18f);
        _collect = CreateSweep("Yarn Collect", 0.14f, 440f, 690f, 0.22f);
        _hit = CreateSweep("Yarn Rack Hit", 0.12f, 180f, 260f, 0.20f);
        _tunnel = CreateSweep("Yarn Tunnel", 0.18f, 280f, 520f, 0.16f);
        _start = CreateChord("Yarn Start", 0.34f, new[] { 392f, 523f, 659f }, 0.16f);
        _win = CreateChord("Yarn Win", 0.72f, new[] { 523f, 659f, 784f, 1047f }, 0.18f);
        _fail = CreateChord("Yarn Fail", 0.46f, new[] { 392f, 311f, 233f }, 0.16f);
    }

    private void Play(AudioClip clip, float volume)
    {
        if (_source != null && clip != null)
        {
            _source.PlayOneShot(clip, volume);
        }
    }

    private static AudioClip CreateSweep(string name, float duration, float startFrequency, float endFrequency, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * sampleRate));
        float[] samples = new float[sampleCount];
        float phase = 0f;
        for (int index = 0; index < sampleCount; index++)
        {
            float progress = index / (float)sampleCount;
            float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
            phase += Mathf.PI * 2f * frequency / sampleRate;
            float envelope = Mathf.Min(1f, progress * 20f) * Mathf.Min(1f, (1f - progress) * 18f);
            samples[index] = Mathf.Sin(phase) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateChord(string name, float duration, float[] frequencies, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * sampleRate));
        float[] samples = new float[sampleCount];
        for (int index = 0; index < sampleCount; index++)
        {
            float progress = index / (float)sampleCount;
            float envelope = Mathf.Min(1f, progress * 12f) * Mathf.Min(1f, (1f - progress) * 5f);
            float value = 0f;
            for (int frequencyIndex = 0; frequencyIndex < frequencies.Length; frequencyIndex++)
            {
                value += Mathf.Sin(Mathf.PI * 2f * frequencies[frequencyIndex] * index / sampleRate);
            }
            samples[index] = value / frequencies.Length * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
