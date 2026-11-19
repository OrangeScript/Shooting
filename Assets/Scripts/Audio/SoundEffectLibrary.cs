using System;
using UnityEngine;

public enum SoundEffect
{
    [InspectorName("玩家开火")] PlayerShot = 0,
    [InspectorName("敌人受击")] EnemyHit = 1,
    [InspectorName("敌人死亡")] EnemyDeath = 2,
    [InspectorName("玩家受伤")] PlayerHurt = 3,
    [InspectorName("玩家死亡")] PlayerDeath = 4,
    [InspectorName("拾取道具")] ItemPickup = 5,
    [InspectorName("爆炸")] Boom = 6,
    [InspectorName("闪电")] Lightning = 7,
    [InspectorName("剑")] Sword = 8,
    [InspectorName("传奇道具获取")] Legendary = 9,
}

public sealed class SoundEffectLibrary : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [SerializeField] private SoundEffect effect;
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Min(0f), Tooltip("同一种音效两次播放之间的最短秒数。")]
        private float minInterval;

        public SoundEffect Effect => effect;
        public AudioClip Clip => clip;
        public float Volume => volume;
        public float MinInterval => minInterval;
    }

    [SerializeField, Range(0f, 1f), Tooltip("所有音效的总音量。")]
    private float masterVolume = 1f;
    [SerializeField] private Entry[] entries = Array.Empty<Entry>();

    public float MasterVolume => masterVolume;

    public Entry Get(SoundEffect effect)
    {
        foreach (Entry entry in entries)
        {
            if (entry != null && entry.Effect == effect)
                return entry;
        }

        return null;
    }
}
