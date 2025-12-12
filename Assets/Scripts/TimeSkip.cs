using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Name : Jaasper Lee
/// Description : Allows the player to "skip time", reducing pet stats accordingly and giving them a temporary age boost.
/// Date : 26 November 2025
/// </summary>

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

    [Header("Cooldown")]
    [Tooltip("Minutes the player must wait between time skips.")]
    public float cooldownMinutes = 60f;
    [Tooltip("Optional Button to disable while on cooldown.")]
    public Button skipButton;
    [Tooltip("Text shown when the skip is on cooldown (will auto-hide).")]
    public TextMeshProUGUI cooldownMessageText;
    [Tooltip("How long the cooldown message is visible (seconds).")]
    public float cooldownMessageSeconds = 3f;

    private Coroutine imageCoroutine;
    private Coroutine cooldownCoroutine;
    private float lastUseTime = -Mathf.Infinity;

    /// Ensures skip UI elements are hidden and attempts to auto-find the skip button.
    private void Awake()
    {
        if (skipImage != null) skipImage.gameObject.SetActive(false);
        if (cooldownMessageText != null) cooldownMessageText.gameObject.SetActive(false);
        if (skipButton == null)
            skipButton = GetComponent<Button>();
    }
    /// Handles the time-skip button: enforces cooldown, shows visuals, and starts the skip coroutine.
    public void OnTimeSkipButtonPressed()
    {
        float cooldownSeconds = cooldownMinutes * 60f;
        float nextAvailable = lastUseTime + cooldownSeconds;
        if (Time.time < nextAvailable)
        {
            float remaining = nextAvailable - Time.time;
            ShowCooldownMessage(remaining);
            return;
        }

        lastUseTime = Time.time;
        if (skipButton != null) skipButton.interactable = false;
        if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
        cooldownCoroutine = StartCoroutine(CooldownCoroutine(cooldownSeconds));

        if (skipImage != null)
        {
            if (imageCoroutine != null) StopCoroutine(imageCoroutine);
            imageCoroutine = StartCoroutine(ShowSkipImageCoroutine());
        }

        StartCoroutine(DoTimeSkip());
    }

    /// Shows the skip image for a short duration.
    private IEnumerator ShowSkipImageCoroutine()
    {
        if (skipImage == null) yield break;
        skipImage.gameObject.SetActive(true);
        yield return new WaitForSeconds(imageDisplaySeconds);
        if (skipImage != null) skipImage.gameObject.SetActive(false);
        imageCoroutine = null;
    }

    /// Applies the time-skip effects to the active pet: reduces stats, applies age boost, and optionally saves.
    private IEnumerator DoTimeSkip()
    {
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

        pet.PauseForSeconds(pauseDuration);

        float percent = Random.Range(minPercent, maxPercent) * 0.01f;

        pet.stats.petHunger = Mathf.Clamp(pet.stats.petHunger * (1f - percent), 0f, 100f);
        pet.stats.petHappiness = Mathf.Clamp(pet.stats.petHappiness * (1f - percent), 0f, 100f);
        pet.stats.petCleanliness = Mathf.Clamp(pet.stats.petCleanliness * (1f - percent), 0f, 100f);

        Debug.Log($"TimeSkip: skipped {hoursToSkip} hours. Stats reduced by {percent*100f:0.##}%");

        float sleepBoostPercent = 0.5f;
        pet.ApplySleepAgeBoost(sleepBoostPercent);

        if (autoSaveAfterSkip && PetTracker.Instance != null)
        {
            PetTracker.Instance.SavePetStats(pet.stats);
            Debug.Log("TimeSkip: pet stats saved after skip.");
        }

        yield return new WaitForSeconds(pauseDuration);
    }

    /// Manages the cooldown period for the time-skip ability.
    private IEnumerator CooldownCoroutine(float seconds)
    {
        float end = Time.time + seconds;
        while (Time.time < end)
        {
            yield return null;
        }
        if (skipButton != null) skipButton.interactable = true;
        cooldownCoroutine = null;
    }

    private void ShowCooldownMessage(float remainingSeconds)
    {
        if (cooldownMessageText == null)
            return;

        int minutes = Mathf.FloorToInt(remainingSeconds / 60f);
        int seconds = Mathf.CeilToInt(remainingSeconds % 60f);
        string timeStr = minutes > 0 ? $"{minutes}m {seconds}s" : $"{seconds}s";
        cooldownMessageText.text = $"Time Skip available in {timeStr}";
        cooldownMessageText.gameObject.SetActive(true);

        StartCoroutine(HideCooldownMessageAfterDelay(cooldownMessageSeconds));
    }

    private IEnumerator HideCooldownMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (cooldownMessageText != null)
            cooldownMessageText.gameObject.SetActive(false);
    }
}