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

    [Header("Cleanliness Decay")]
    [Tooltip("How many cleanliness points are lost per minute.")]
    public float cleanlinessDecayPerMinute = 1f;
    [Tooltip("How often (seconds) to apply decay ticks.")]
    public float decayTickInterval = 5f;

    [Header("Dirt Particles")]
    [Tooltip("Particle system to play when pet is dirty. If null the script will try to find a ParticleSystem child.")]
    public ParticleSystem dirtParticles;
    [Tooltip("Start showing particles when cleanliness <= this percent (0-100).")]
    public float dirtParticlesStartPercent = 30f;
    [Tooltip("Stop showing particles when cleanliness >= this percent (0-100).")]
    public float dirtParticlesStopPercent = 50f;

    private Coroutine cleanlinessRoutine;
    private bool dirtPlaying = false;
    private float lastCleanliness = -1f;
    private void OnEnable()
    {
        // Random hunger coroutine for runtime instances
        hungerRoutine = StartCoroutine(HungerRoutine());
        // Random happiness coroutine
        happinessRoutine = StartCoroutine(HappinessRoutine());
        // Aging coroutine
        agingRoutine = StartCoroutine(AgeRoutine());

        // Cleanliness decay
        cleanlinessRoutine = StartCoroutine(CleanlinessRoutine());

        // initialize particle visibility immediately
        if (stats != null)
        {
            lastCleanliness = stats.petCleanliness;
            HandleDirtParticles();
        }
    }

    private void OnDisable()
    {
        if (hungerRoutine != null) StopCoroutine(hungerRoutine);
        if (happinessRoutine != null) StopCoroutine(happinessRoutine);
        if (agingRoutine != null) StopCoroutine(agingRoutine);
        if (cleanlinessRoutine != null) StopCoroutine(cleanlinessRoutine);
    }
    private void Update()
    {
        // React immediately to cleanliness changes made elsewhere
        if (stats != null)
        {
            if (Mathf.Abs(stats.petCleanliness - lastCleanliness) > 0.0001f)
            {
                lastCleanliness = stats.petCleanliness;
                HandleDirtParticles();
                if (PetTracker.Instance != null)
                    PetTracker.Instance.RegisterCurrentPet(this);
            }
        }
    }

    private void OnValidate()
    {
        dirtParticlesStartPercent = Mathf.Clamp(dirtParticlesStartPercent, 0f, 100f);
        dirtParticlesStopPercent = Mathf.Clamp(dirtParticlesStopPercent, 0f, 100f);
        if (dirtParticlesStartPercent > dirtParticlesStopPercent)
        {

            float t = dirtParticlesStartPercent;
            dirtParticlesStartPercent = dirtParticlesStopPercent;
            dirtParticlesStopPercent = t;
        }
    }

    // Pause automatic tick coroutines for a number of real-time seconds
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
    private IEnumerator CleanlinessRoutine()
    {
        while (true)
        {
            while (Time.time < pauseEndTime)
                yield return null;

            yield return new WaitForSeconds(Mathf.Max(0.1f, decayTickInterval));

            if (Time.time < pauseEndTime) continue;
            if (stats == null) continue;

            // apply decay
            float perSecond = cleanlinessDecayPerMinute / 60f;
            float delta = perSecond * decayTickInterval;
            stats.petCleanliness = Mathf.Clamp(stats.petCleanliness - delta, 0f, 100f);

            Debug.Log($"[PetStatsComponent] Cleanliness decayed by {delta:F2}, now {stats.petCleanliness:F1}");

            // notify tracker/UI
            if (PetTracker.Instance != null)
                PetTracker.Instance.RegisterCurrentPet(this);

            HandleDirtParticles();
        }
    }

    private void HandleDirtParticles()
    {
        if (dirtParticles == null)
            dirtParticles = GetComponentInChildren<ParticleSystem>();

        if (dirtParticles == null) return;

        // prefer toggling emission and GameObject to fully hide/show particles
        var emission = dirtParticles.emission;

        if (!dirtPlaying && stats.petCleanliness <= dirtParticlesStartPercent)
        {
            dirtParticles.gameObject.SetActive(true);
            emission.enabled = true;
            if (!dirtParticles.isPlaying) dirtParticles.Play();
            dirtPlaying = true;
        }
        else if (dirtPlaying && stats.petCleanliness >= dirtParticlesStopPercent)
        {
            // stop emitting and hide the particle object
            emission.enabled = false;
            dirtParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            dirtParticles.gameObject.SetActive(false);
            dirtPlaying = false;
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

            // If any tracked stat is zero, reverse aging until age reaches 0.
            bool anyStatZero = stats.petHappiness <= 0f || stats.petHunger <= 0f || stats.petCleanliness <= 0f;
            if (anyStatZero)
            {
                if (stats.petAge > 0)
                {
                    stats.petAge = Mathf.Max(0, stats.petAge - 1);
                    Debug.Log($"[PetStatsComponent] Reverse aging: age decreased to {stats.petAge} due to a stat at zero.");

                    if (autoSaveOnAge && PetTracker.Instance != null)
                    {
                        PetTracker.Instance.SavePetStats(stats);
                        Debug.Log("[PetStatsComponent] Auto-saved pet stats after reverse aging.");
                    }
                }
                else
                {
                    // When age is already 0, it'll do nothing 
                }
            }
            else
            {
                // Normal forward aging
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
}