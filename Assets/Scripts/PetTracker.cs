using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Auth;
using UnityEngine.SceneManagement;

[Serializable]
public class GameSettings
{
    public float minHungerInterval = 10f;
    public float maxHungerInterval = 30f;
    public float minHungerDecrease = 1f;
    public float maxHungerDecrease = 5f;

    public float minHappinessInterval = 15f;
    public float maxHappinessInterval = 45f;
    public float minHappinessDecrease = 1f;
    public float maxHappinessDecrease = 4f;
}

public class PetTracker : MonoBehaviour
{
    public static PetTracker Instance { get; private set; }

    public PetStatsComponent CurrentPet { get; private set; }

    // Remote settings loaded from /gameSettings
    public GameSettings remoteGameSettings = new GameSettings();
    private bool gameSettingsLoaded = false;

    private Queue<Action> mainThreadQueue = new Queue<Action>();
    private FirebaseAuth auth;
    private DatabaseReference db;
    private DatabaseReference gameSettingsRef;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseDatabase.DefaultInstance?.RootReference;

        SubscribeGameSettings();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        UnsubscribeGameSettings();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        lock (mainThreadQueue)
        {
            while (mainThreadQueue.Count > 0)
            {
                try { mainThreadQueue.Dequeue().Invoke(); }
                catch (Exception e) { Debug.LogError("Queued action exception: " + e); }
            }
        }
    }

    private void Enqueue(Action a)
    {
        lock (mainThreadQueue) mainThreadQueue.Enqueue(a);
    }

    public void RegisterCurrentPet(PetStatsComponent pet)
    {
        CurrentPet = pet;
        if (gameSettingsLoaded && CurrentPet != null)
            ApplyGameSettingsTo(CurrentPet);
    }

    public void SavePetStats(PetStats stats)
    {
        if (stats == null) { Debug.LogError("No stats provided to save."); return; }
        if (db == null) { Debug.LogError("Firebase Database not initialized."); return; }

        string json = JsonUtility.ToJson(stats);
        string uid = GetUserIdOrGuest();
        db.Child("users").Child(uid).Child("petStats").SetRawJsonValueAsync(json).ContinueWith(t =>
        {
            if (t.IsCanceled || t.IsFaulted) Debug.LogError("Write failed: " + t.Exception);
            else Debug.Log("Pet data saved for user: " + uid);
        });
    }

    public void LoadPetStatsFor(PetStatsComponent instance)
    {
        if (instance == null || db == null) { Debug.LogWarning("LoadPetStatsFor: missing instance or DB"); return; }

        string uid = GetUserIdOrGuest();
        db.Child("users").Child(uid).Child("petStats").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted) { Debug.LogWarning("Load failed: " + task.Exception); return; }
            var snapshot = task.Result;
            if (snapshot == null || !snapshot.Exists) { Debug.Log("No saved pet data for user: " + uid); return; }

            string raw = snapshot.GetRawJsonValue();
            if (string.IsNullOrEmpty(raw)) { Debug.Log("Empty saved JSON for user: " + uid); return; }

            Enqueue(() =>
            {
                try
                {
                    if (instance.stats == null) instance.stats = new PetStats();
                    JsonUtility.FromJsonOverwrite(raw, instance.stats);
                    Debug.Log($"Loaded pet stats for {uid}: happiness={instance.stats.petHappiness}");

                    if (gameSettingsLoaded) ApplyGameSettingsTo(instance);
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to apply loaded stats: " + e);
                }
            });
        });
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var petComp = FindFirstObjectByType<PetStatsComponent>();
        if (petComp != null)
        {
            if (gameSettingsLoaded) ApplyGameSettingsTo(petComp);
            RegisterCurrentPet(petComp);
            LoadPetStatsFor(petComp);
            Debug.Log($"PetTracker: found pet in scene '{scene.name}', loading saved stats.");
        }
    }

    private string GetUserIdOrGuest()
    {
        if (auth != null && auth.CurrentUser != null) return auth.CurrentUser.UserId;
        return "guest_" + SystemInfo.deviceUniqueIdentifier;
    }

    // -- Remote gameSettings subscription and application --

    private void SubscribeGameSettings()
    {
        if (db == null) { Debug.Log("[PetTracker] DB not initialized - cannot subscribe to gameSettings."); return; }

        gameSettingsRef = db.Child("gameSettings");
        gameSettingsRef.ValueChanged += OnGameSettingsChanged;

        // initial load
        gameSettingsRef.GetValueAsync().ContinueWith(t =>
        {
            if (t.IsFaulted || t.IsCanceled) return;
            var snap = t.Result;
            if (snap != null && snap.Exists) Enqueue(() => ApplyRawSettingsJson(snap.GetRawJsonValue()));
        });
    }

    private void UnsubscribeGameSettings()
    {
        if (gameSettingsRef != null)
        {
            try { gameSettingsRef.ValueChanged -= OnGameSettingsChanged; } catch { }
            gameSettingsRef = null;
        }
    }

    private void OnGameSettingsChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) { Debug.LogWarning("[PetTracker] gameSettings ValueChanged error: " + args.DatabaseError.Message); return; }
        var snap = args.Snapshot;
        if (snap == null || !snap.Exists) { Debug.Log("[PetTracker] gameSettings snapshot empty - using defaults."); return; }
        Enqueue(() => ApplyRawSettingsJson(snap.GetRawJsonValue()));
    }

    private void ApplyRawSettingsJson(string raw)
    {
        if (string.IsNullOrEmpty(raw)) { Debug.Log("[PetTracker] empty gameSettings JSON"); return; }
        try
        {
            JsonUtility.FromJsonOverwrite(raw, remoteGameSettings);
            gameSettingsLoaded = true;
            Debug.Log("[PetTracker] Applied remote gameSettings.");
            if (CurrentPet != null) ApplyGameSettingsTo(CurrentPet);
        }
        catch (Exception e)
        {
            Debug.LogError("[PetTracker] Failed to parse/apply gameSettings: " + e + " raw: " + raw);
        }
    }

    private void ApplyGameSettingsTo(PetStatsComponent pet)
    {
        if (pet == null || !gameSettingsLoaded) return;

        pet.minHungerInterval = remoteGameSettings.minHungerInterval;
        pet.maxHungerInterval = remoteGameSettings.maxHungerInterval;
        pet.minHungerDecrease = remoteGameSettings.minHungerDecrease;
        pet.maxHungerDecrease = remoteGameSettings.maxHungerDecrease;

        pet.minHappinessInterval = remoteGameSettings.minHappinessInterval;
        pet.maxHappinessInterval = remoteGameSettings.maxHappinessInterval;
        pet.minHappinessDecrease = remoteGameSettings.minHappinessDecrease;
        pet.maxHappinessDecrease = remoteGameSettings.maxHappinessDecrease;

    }
}