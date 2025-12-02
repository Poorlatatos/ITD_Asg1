using UnityEngine;

public class FeedButton : MonoBehaviour
{
    public PetStatsComponent petToFeed;
    public float hungerAmount = 10f;
    public bool autoSave = false;

    [Header("Audio")]
    public AudioClip feedClip;
    // optional: assign an AudioSource in inspector, otherwise one will be added at runtime
    public AudioSource feedSource;

    private void Awake()
    {
        // ensure an AudioSource exists to play the clip
        if (feedSource == null)
        {
            feedSource = GetComponent<AudioSource>();
            if (feedSource == null)
                feedSource = gameObject.AddComponent<AudioSource>();
        }

        // make sure audio source is configured and enabled
        feedSource.playOnAwake = false;
        feedSource.loop = false;
        feedSource.enabled = true; // <-- ensure it's enabled to avoid "Can not play a disabled audio source"
    }

    // Hook this to your UI Button OnClick()
    public void OnFeedClicked()
    {
        PetStatsComponent target = null;

        // Prefer the active runtime pet registered by ImageTracker/PetTracker
        if (PetTracker.Instance != null && PetTracker.Instance.CurrentPet != null)
            target = PetTracker.Instance.CurrentPet;

        // If no runtime pet, consider the inspector-assigned target only if it's part of a loaded scene
        if (target == null && petToFeed != null)
        {
            var scene = petToFeed.gameObject.scene;
            if (scene.IsValid() && scene.isLoaded && petToFeed.gameObject.activeInHierarchy)
            {
                target = petToFeed;
            }
            else
            {
                // fallback: find any active PetStatsComponent in the scene
                target = UnityEngine.Object.FindFirstObjectByType<PetStatsComponent>();
            }
        }

        if (target == null)
        {
            Debug.LogError("FeedButton: no pet instance available to feed.");
            return;
        }

        if (target.stats == null)
        {
            Debug.LogError("FeedButton: target has no stats object.");
            return;
        }

        target.stats.petHunger = Mathf.Clamp(target.stats.petHunger + hungerAmount, 0f, 100f);
        Debug.Log($"Fed pet: hunger is now {target.stats.petHunger:F1}");

        // ensure PetTracker points at this runtime instance so UI updates
        if (PetTracker.Instance != null)
            PetTracker.Instance.RegisterCurrentPet(target);

        // play feeding sound: stop any currently playing feed clip and restart
        if (feedClip != null)
        {
            if (feedSource == null)
            {
                feedSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
                feedSource.playOnAwake = false;
                feedSource.loop = false;
            }

            if (!feedSource.enabled)
                feedSource.enabled = true;

            // stop any current playback and restart
            if (feedSource.isPlaying)
                feedSource.Stop();

            feedSource.PlayOneShot(feedClip);
        }
        else
        {
            Debug.LogWarning("FeedButton: no feedClip assigned.");
        }

        if (autoSave && PetTracker.Instance != null)
        {
            PetTracker.Instance.SavePetStats(target.stats);
            Debug.Log("FeedButton: autosaved pet stats.");
        }
    }
}