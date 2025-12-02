using UnityEngine;
public class SaveButton : MonoBehaviour
{
    // optional inspector assignment — if left null, SaveButton will try to save the currently active runtime pet
    public PetStatsComponent petToSave; // assign the pet instance (prefab instance) in the inspector

    // Hook this to your UI Button OnClick()
    public void OnSaveClicked()
    {
        PetStatsComponent target = petToSave;

        if (target == null && PetTracker.Instance != null)
        {
            target = PetTracker.Instance.CurrentPet;
        }

        if (target == null)
        {
            Debug.LogError("SaveButton: no pet instance available to save.");
            return;
        }

        var saver = PetTracker.Instance;
        if (saver == null)
        {
            Debug.LogError("SaveButton: no PetTracker found in scene.");
            return;
        }

        saver.SavePetStats(target.stats);
    }
}