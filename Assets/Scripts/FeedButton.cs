using UnityEngine;

/// <summary>
/// Name : Jaasper Lee
/// Description : Script to allow user to feed their pet by clicking a button.
/// Date : 14 November 2025
/// </summary>

public class FeedButton : MonoBehaviour
{
    public PetStatsComponent petToFeed;
    public float hungerAmount = 10f;
    public bool autoSave = false;

    [Header("Audio")]
    public AudioClip feedClip;
    public AudioSource feedSource;

    /// Initializes the audio source used to play feeding sound effects.
    private void Awake()
    {
        if (feedSource == null)
        {
            feedSource = GetComponent<AudioSource>();
            if (feedSource == null)
                feedSource = gameObject.AddComponent<AudioSource>();
        }

        feedSource.playOnAwake = false;
        feedSource.loop = false;
        feedSource.enabled = true;
    }

    /// Handles the feed button click: updates hunger, plays SFX, and optionally autosaves.
    public void OnFeedClicked()
    {
        PetStatsComponent target = null;
        
        if (PetTracker.Instance != null && PetTracker.Instance.CurrentPet != null)
            target = PetTracker.Instance.CurrentPet;
        
        if (target == null && petToFeed != null)
        if (target == null && petToFeed != null)
        {
            var scene = petToFeed.gameObject.scene;
            if (scene.IsValid() && scene.isLoaded && petToFeed.gameObject.activeInHierarchy)
            {
                target = petToFeed;
            }
            else
            {
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