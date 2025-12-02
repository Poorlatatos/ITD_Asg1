using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Auth;
using UnityEngine.SceneManagement;

public class PetTracker : MonoBehaviour
{
    public static PetTracker Instance { get; private set; }

    public PetStatsComponent CurrentPet { get; private set; }

    private Queue<Action> mainThreadQueue = new Queue<Action>();
    private FirebaseAuth auth;
    private DatabaseReference db;

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

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void RegisterCurrentPet(PetStatsComponent pet)
    {
        CurrentPet = pet;
    }

    public void SaveCurrentPet()
    {
        if (CurrentPet == null)
        {
            Debug.LogError("No current pet registered to save.");
            return;
        }
        SavePetStats(CurrentPet.stats);
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

    private string GetUserIdOrGuest()
    {
        if (auth != null && auth.CurrentUser != null)
        {
            // prefer the user's displayName (username) if set, otherwise fallback to uid
            var name = auth.CurrentUser.DisplayName;
            if (!string.IsNullOrEmpty(name)) return name;
            return auth.CurrentUser.UserId;
        }
        // guest users are now stored under users/... as well
        return "guest_" + SystemInfo.deviceUniqueIdentifier;
    }


    public void SavePetStats(PetStats stats)
    {
        if (stats == null)
        {
            Debug.LogError("No stats provided to save.");
            return;
        }

        if (db == null)
        {
            Debug.LogError("Firebase Database not initialized.");
            return;
        }

        string json = JsonUtility.ToJson(stats);
        string uid = GetUserIdOrGuest();

        // write pet data under users/{uid}/petStats so users and pets are merged
        db.Child("users").Child(uid).Child("petStats").SetRawJsonValueAsync(json).ContinueWith(t =>
        {
            if (t.IsCanceled || t.IsFaulted) Debug.LogError("Write failed: " + t.Exception);
            else Debug.Log("Pet data saved for user: " + uid);
        });
    }
    public void LoadPetStatsFor(PetStatsComponent instance)
    {
        if (instance == null || db == null)
        {
            Debug.LogWarning("LoadPetStatsFor: missing instance or DB");
            return;
        }

        string uid = GetUserIdOrGuest();

        // read pet data from users/{uid}/petStats
        db.Child("users").Child(uid).Child("petStats").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogWarning("Load failed: " + task.Exception);
                return;
            }

            var snapshot = task.Result;
            if (snapshot == null || !snapshot.Exists)
            {
                Debug.Log("No saved pet data for user: " + uid);
                return;
            }

            string raw = snapshot.GetRawJsonValue();
            if (string.IsNullOrEmpty(raw))
            {
                Debug.Log("Empty saved JSON for user: " + uid);
                return;
            }

            Enqueue(() =>
            {
                try
                {
                    if (instance.stats == null) instance.stats = new PetStats();
                    JsonUtility.FromJsonOverwrite(raw, instance.stats);
                    Debug.Log($"Loaded pet stats for {uid}: happiness={instance.stats.petHappiness}");
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
            RegisterCurrentPet(petComp);
            LoadPetStatsFor(petComp);
            Debug.Log($"PetTracker: found pet in scene '{scene.name}', loading saved stats.");
        }
    }
}