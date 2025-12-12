using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Name : Waine Low
/// Description : Script to update floating bars for pet stats.
/// Date : 13 November 2025
/// </summary>

public class FloatingBar : MonoBehaviour
{
    [SerializeField] private Slider hungerSlider;
    [SerializeField] private Slider happinessSlider;
    [SerializeField] private Slider cleanlinessSlider;

    private void Update()
    {
        var pet = PetTracker.Instance?.CurrentPet;
        if (pet != null && pet.stats != null)
        {
            if (hungerSlider) hungerSlider.value = Mathf.Clamp01(pet.stats.petHunger / 100f);
            if (happinessSlider) happinessSlider.value = Mathf.Clamp01(pet.stats.petHappiness / 100f);
            if (cleanlinessSlider) cleanlinessSlider.value = Mathf.Clamp01(pet.stats.petCleanliness / 100f);
        }
        else
        {
            if (hungerSlider) hungerSlider.value = 0f;
            if (happinessSlider) happinessSlider.value = 0f;
            if (cleanlinessSlider) cleanlinessSlider.value = 0f;
        }
    }
}