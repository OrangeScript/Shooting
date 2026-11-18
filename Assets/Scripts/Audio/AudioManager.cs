using System.Collections.Generic;
using UnityEngine;

public static class AudioManager
{
    private const string LibraryPath = "Audio/SoundEffects";
    private static readonly Dictionary<SoundEffect, double> nextPlayTimes = new();
    private static SoundEffectLibrary library;
    private static bool libraryLoaded;
    private static AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        library = null;
        libraryLoaded = false;
        source = null;
        nextPlayTimes.Clear();
    }

    public static void Play(SoundEffect effect)
    {
        if (!libraryLoaded)
        {
            library = Resources.Load<SoundEffectLibrary>(LibraryPath);
            libraryLoaded = true;
        }

        SoundEffectLibrary.Entry entry = library != null ? library.Get(effect) : null;
        if (entry == null || entry.Clip == null || entry.Volume <= 0f)
            return;

        if (source == null)
        {
            source = new GameObject("AudioManager").AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            nextPlayTimes.Clear();
        }

        double now = Time.unscaledTimeAsDouble;
        if (nextPlayTimes.TryGetValue(effect, out double nextPlayTime) && now < nextPlayTime)
            return;

        source.volume = library.MasterVolume;
        if (source.volume <= 0f)
            return;

        source.PlayOneShot(entry.Clip, entry.Volume);
        nextPlayTimes[effect] = now + entry.MinInterval;
    }
}
