using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TimeSkip : MonoBehaviour
{
    [Header("Skip Settings")]
    public float hoursToSkip = 8f;
    public float pauseDuration = 2f;
    public float minPercent = 10f;
    public float maxPercent = 20f;
    public bool autoSaveAfterSkip = true;

    [Header("Skip Visual")]
    public Image skipImage;
    public float imageDisplaySeconds = 2f;

    private Coroutine imageCoroutine;

    // Hook this method to your UI Button OnClick()
    public void OnTimeSkipButtonPressed()
    {
        // Shows the image
        if (skipImage != null)
        {
            if (imageCoroutine != null) StopCoroutine(imageCoroutine);
            imageCoroutine = StartCoroutine(ShowSkipImageCoroutine());
        }

        StartCoroutine(DoTimeSkip());
    }

    private IEnumerator ShowSkipImageCoroutine()
    {
        if (skipImage == null) yield break;
        skipImage.gameObject.SetActive(true);
        yield return new WaitForSeconds(imageDisplaySeconds);
        // Hide if still assigned
        if (skipImage != null) skipImage.gameObject.SetActive(false);
        imageCoroutine = null;
    }

    private IEnumerator DoTimeSkip()
    {
        // Finds the active pet that in currently in the scene
        PetStatsComponent pet = null;
        if (PetTracker.Instance != null && PetTracker.Instance.CurrentPet != null)
            pet = PetTracker.Instance.CurrentPet;
        else
            pet = FindFirstObjectByType<PetStatsComponent>();

        if (pet == null || pet.stats == null)
        {
            Debug.LogWarning("TimeSkip: no pet found to apply skip to.");
            yield break;
        }

        // Pause pet routines briefly to avoid double-applying ticks
        pet.PauseForSeconds(pauseDuration);

        // Compute random percent reduction between minPercent and maxPercent
        float percent = Random.Range(minPercent, maxPercent) * 0.01f;

        // Apply reduction to each stat
        pet.stats.petHunger = Mathf.Clamp(pet.stats.petHunger * (1f - percent), 0f, 100f);
        pet.stats.petHappiness = Mathf.Clamp(pet.stats.petHappiness * (1f - percent), 0f, 100f);
        pet.stats.petCleanliness = Mathf.Clamp(pet.stats.petCleanliness * (1f - percent), 0f, 100f);

        Debug.Log($"TimeSkip: skipped {hoursToSkip} hours. Stats reduced by {percent*100f:0.##}%");

        // Gives a small aging speed boost for sleeping
        float sleepBoostPercent = 0.5f;
        pet.ApplySleepAgeBoost(sleepBoostPercent);

        // Saves stats after skip
        if (autoSaveAfterSkip && PetTracker.Instance != null)
        {
            PetTracker.Instance.SavePetStats(pet.stats);
            Debug.Log("TimeSkip: pet stats saved after skip.");
        }

        yield return new WaitForSeconds(pauseDuration);
    }
    private void Awake()
    {
        // ensure skipImage is hidden at start
        if (skipImage != null) skipImage.gameObject.SetActive(false);
    }
}