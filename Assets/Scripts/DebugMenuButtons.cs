using UnityEngine;

/// <summary>
/// Name : Jaasper Lee
/// Description : Script to provide debug menu buttons to set pet stats quickly.
/// Date : 27 November 2025
/// </summary>

public class DebugMenuButtons : MonoBehaviour
{
    private PetStatsComponent FindActivePet()
    {
        if (PetTracker.Instance != null && PetTracker.Instance.CurrentPet != null)
            return PetTracker.Instance.CurrentPet;

        return UnityEngine.Object.FindFirstObjectByType<PetStatsComponent>();
    }

    /// Set all tracked stats to 50%.
    public void SetStatsHalf()
    {
        var pet = FindActivePet();
        if (pet == null || pet.stats == null)
        {
            Debug.LogWarning("DebugMenuButtons: no pet found to modify.");
            return;
        }

        pet.stats.petHunger = 50f;
        pet.stats.petHappiness = 50f;
        pet.stats.petCleanliness = 50f;

        if (PetTracker.Instance != null)
            PetTracker.Instance.RegisterCurrentPet(pet);

        Debug.Log("DebugMenuButtons: set pet stats to half (50%).");
    }

    /// Set all tracked stats to 100%.
    public void SetStatsFull()
    {
        var pet = FindActivePet();
        if (pet == null || pet.stats == null)
        {
            Debug.LogWarning("DebugMenuButtons: no pet found to modify.");
            return;
        }

        pet.stats.petHunger = 100f;
        pet.stats.petHappiness = 100f;
        pet.stats.petCleanliness = 100f;

        if (PetTracker.Instance != null)
            PetTracker.Instance.RegisterCurrentPet(pet);

        Debug.Log("DebugMenuButtons: set pet stats to full (100%).");
    }

    /// Set all tracked stats to zero.
    public void SetStatsZero()
    {
        var pet = FindActivePet();
        if (pet == null || pet.stats == null)
        {
            Debug.LogWarning("DebugMenuButtons: no pet found to modify.");
            return;
        }

        pet.stats.petHunger = 0f;
        pet.stats.petHappiness = 0f;
        pet.stats.petCleanliness = 0f;

        if (PetTracker.Instance != null)
            PetTracker.Instance.RegisterCurrentPet(pet);

        Debug.Log("DebugMenuButtons: set pet stats to zero (0%).");
    }
}