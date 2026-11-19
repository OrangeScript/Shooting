using UnityEngine;
using UnityEngine.Playables;

public sealed class LegendaryPickupCutscene : MonoBehaviour
{
    [SerializeField] private PlayableDirector director;

    public void Play()
    {
        if (director == null || director.state == PlayState.Playing)
            return;

        director.time = 0d;
        director.Play();
    }
}