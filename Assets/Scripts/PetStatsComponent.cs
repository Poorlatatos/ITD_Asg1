using System.Collections;
using UnityEngine;

public class PetStatsComponent : MonoBehaviour
{
    public PetStats stats = new PetStats
    {
        petAge = 1,
        petHappiness = 50f,
        petHunger = 50f,
        petCleanliness = 50f
    };

    private float pauseEndTime = 0f;

    [Header("Hunger Settings (runtime)")]
    public float minHungerInterval = 10f;
    public float maxHungerInterval = 30f;
    public float minHungerDecrease = 1f;
    public float maxHungerDecrease = 5f;

    // Overtime tick settings for stats in runtime
    [Header("Happiness Settings (runtime)")]
    public float minHappinessInterval = 15f;
    public float maxHappinessInterval = 45f;
    public float minHappinessDecrease = 1f;
    public float maxHappinessDecrease = 4f;

    // Aging settings
    [Header("Aging Settings")]
    public float minutesPerAge = 5f; // default every 5 minutes
    [Range(0.1f, 1f)]
    public float boostMultiplier = 0.5f; // twice as fast when boosted
    [Range(0f, 100f)]
    public float boostThresholdPercent = 90f;
    public bool autoSaveOnAge = true;

    // Cumulative percent acceleration applied when player sleeps (e.g. 0.5 per sleep)
    [Tooltip("Cumulative percent acceleration to age speed (10 = 10% faster). Applied as a percent reduction to aging interval.")]
    public float ageAccelerationPercent = 0f;
    private Coroutine hungerRoutine;
    private Coroutine happinessRoutine;
    private Coroutine agingRoutine;
    private void OnEnable()
    {
        // start the random hunger coroutine for runtime instances
        hungerRoutine = StartCoroutine(HungerRoutine());
        // start the random happiness coroutine
        happinessRoutine = StartCoroutine(HappinessRoutine());
        // start aging coroutine
        agingRoutine = StartCoroutine(AgeRoutine());
    }

    private void OnDisable()
    {
        if (hungerRoutine != null) StopCoroutine(hungerRoutine);
        if (happinessRoutine != null) StopCoroutine(happinessRoutine);
        if (agingRoutine != null) StopCoroutine(agingRoutine);
    }

    // Public API: pause automatic tick coroutines for a number of real-time seconds
    public void PauseForSeconds(float seconds)
    {
        pauseEndTime = Mathf.Max(pauseEndTime, Time.time + Mathf.Max(0f, seconds));
    }

    private IEnumerator HungerRoutine()
    {
        while (true)
        {
            // if paused, wait until pause ends
            while (Time.time < pauseEndTime)
                yield return null;

            float wait = Random.Range(minHungerInterval, maxHungerInterval);
            yield return new WaitForSeconds(wait);

            // check pause again before applying
            if (Time.time < pauseEndTime) continue;

            if (stats == null) continue;

            float decrease = Random.Range(minHungerDecrease, maxHungerDecrease);
            stats.petHunger = Mathf.Clamp(stats.petHunger - decrease, 0f, 100f);

            Debug.Log($"[PetStatsComponent] Hunger decreased by {decrease:F1}, now {stats.petHunger:F1}");
        }
    }

    private IEnumerator HappinessRoutine()
    {
        while (true)
        {
            while (Time.time < pauseEndTime)
                yield return null;

            float wait = Random.Range(minHappinessInterval, maxHappinessInterval);
            yield return new WaitForSeconds(wait);

            if (Time.time < pauseEndTime) continue;

            if (stats == null) continue;

            float decrease = Random.Range(minHappinessDecrease, maxHappinessDecrease);
            stats.petHappiness = Mathf.Clamp(stats.petHappiness - decrease, 0f, 100f);

            Debug.Log($"[PetStatsComponent] Happiness decreased by {decrease:F1}, now {stats.petHappiness:F1}");
        }
    }

    public void ApplySleepAgeBoost(float percent)
    {
        // accumulate, clamp to avoid negative or absurd large values (cap at 90% faster -> interval *0.1)
        ageAccelerationPercent = Mathf.Clamp(ageAccelerationPercent + percent, 0f, 90f);
        Debug.Log($"[PetStatsComponent] Applied sleep age boost +{percent}%, total acceleration now {ageAccelerationPercent}%");

        // optional: persist immediately
        if (autoSaveOnAge == false && PetTracker.Instance != null)
        {
            PetTracker.Instance.SavePetStats(stats);
        }
    }
    private IEnumerator AgeRoutine()
    {
        if (stats == null) yield break;

        while (true)
        {
            while (Time.time < pauseEndTime)
                yield return null;

            float baseSeconds = Mathf.Max(10f, minutesPerAge * 60f);
            bool boosted = (stats.petCleanliness >= boostThresholdPercent
                        && stats.petHunger >= boostThresholdPercent
                        && stats.petHappiness >= boostThresholdPercent);

            float interval = boosted ? baseSeconds * boostMultiplier : baseSeconds;

            // APPLY cumulative sleep acceleration: reduce interval by ageAccelerationPercent%
            float accelMultiplier = Mathf.Clamp01(1f - (ageAccelerationPercent * 0.01f));
            accelMultiplier = Mathf.Max(accelMultiplier, 0.1f); // never below 0.1 to keep sane timing
            interval *= accelMultiplier;

            yield return new WaitForSeconds(interval);

            if (Time.time < pauseEndTime) continue;
            if (stats == null) yield break;

            stats.petAge = stats.petAge + 1;
            Debug.Log($"[PetStatsComponent] Pet aged to {stats.petAge} (boosted={boosted}, accel={ageAccelerationPercent}%)");

            if (autoSaveOnAge && PetTracker.Instance != null)
            {
                PetTracker.Instance.SavePetStats(stats);
                Debug.Log("[PetStatsComponent] Auto-saved pet stats after aging.");
            }
        }
    }
}