using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Database;

/// <summary>
/// Name : Jaasper Lee
/// Description : Manages user settings, including loading/saving to Firebase and local cache.
/// Date : 5 December 2025
/// </summary>

[Serializable]
public class UserPrefs
{
    public float bgmVolume = 1f;
    public float sfxVolume = 1f;
}

public class UserSettings : MonoBehaviour
{
    [Header("UI")]
    public Slider bgmSlider;
    public Slider sfxSlider;
    public TextMeshProUGUI feedbackText;

    [Header("Behaviour")]
    public float saveDebounceSeconds = 0.6f;

    private UserPrefs prefs = new UserPrefs();
    private Coroutine debounceCoroutine;

    private void Awake()
    {
        // Hook sliders
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);

        // Load cached local prefs immediately so UI is responsive
        LoadFromLocal();
    }
    private void ClearFeedback()
    {
        if (feedbackText != null)
            feedbackText.text = "";
    }
    private void OnEnable()
    {
        var auth = FirebaseAuth.DefaultInstance;
        if (auth != null) auth.StateChanged += OnAuthStateChanged;
        // If already signed in, attempt load from Firebase
        TryLoadForCurrentUser();
    }

    private void OnDisable()
    {
        var auth = FirebaseAuth.DefaultInstance;
        if (auth != null) auth.StateChanged -= OnAuthStateChanged;
    }

    private void OnAuthStateChanged(object sender, EventArgs e)
    {
        TryLoadForCurrentUser();
    }

    private void OnBgmChanged(float v)
    {
        prefs.bgmVolume = v;
        PlayerPrefs.SetFloat("prefs.bgmVolume", v);
        DebouncedSave();
    }

    private void OnSfxChanged(float v)
    {
        prefs.sfxVolume = v;
        PlayerPrefs.SetFloat("prefs.sfxVolume", v);
        DebouncedSave();
    }

    public void DebouncedSave()
    {
        if (debounceCoroutine != null) StopCoroutine(debounceCoroutine);
        debounceCoroutine = StartCoroutine(DebounceAndSave());
    }

    private System.Collections.IEnumerator DebounceAndSave()
    {
        yield return new WaitForSeconds(saveDebounceSeconds);
        SaveToFirebase();
        debounceCoroutine = null;
    }

    public void SaveToFirebase()
    {
        var db = FirebaseDatabase.DefaultInstance;
        var auth = FirebaseAuth.DefaultInstance;

        // Always cache locally first
        PlayerPrefs.SetFloat("prefs.bgmVolume", prefs.bgmVolume);
        PlayerPrefs.SetFloat("prefs.sfxVolume", prefs.sfxVolume);
        PlayerPrefs.Save();

        if (db == null)
        {
            SetFeedback("Saved locally (offline).");
            Debug.Log("[UserSettings] Firebase DB not initialized - saved locally.");
            return;
        }

        string uid = GetUidOrGuest(auth);
        string json = JsonUtility.ToJson(prefs);

        db.RootReference.Child("users").Child(uid).Child("settings").SetRawJsonValueAsync(json)
            .ContinueWith(t =>
            {
                if (t.IsFaulted || t.IsCanceled)
                {
                    Debug.LogError("[UserSettings] Save failed: " + t.Exception);
                    SetFeedbackOnMainThread("Save failed.");
                }
                else
                {
                    Debug.Log("[UserSettings] Settings saved to Firebase.");
                    SetFeedbackOnMainThread("Settings saved.");
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void TryLoadForCurrentUser()
    {
        var auth = FirebaseAuth.DefaultInstance;
        if (auth != null && auth.CurrentUser != null)
        {
            LoadFromFirebase();
        }
        else
        {
            // not signed in -> use local
            LoadFromLocal();
        }
    }

    public async void LoadFromFirebase()
    {
        var db = FirebaseDatabase.DefaultInstance;
        var auth = FirebaseAuth.DefaultInstance;
        if (db == null || auth == null || auth.CurrentUser == null)
        {
            Debug.Log("[UserSettings] DB/auth not ready -> loading local settings.");
            LoadFromLocal();
            return;
        }

        string uid = GetUidOrGuest(auth);
        Debug.Log($"[UserSettings] Starting remote load for uid={uid}");

        try
        {
            var snapshot = await db.RootReference.Child("users").Child(uid).Child("settings").GetValueAsync();
            if (snapshot == null || !snapshot.Exists)
            {
                Debug.Log("[UserSettings] No remote settings found; using local.");
                LoadFromLocal();
                return;
            }

            string raw = snapshot.GetRawJsonValue();
            if (string.IsNullOrEmpty(raw))
            {
                Debug.Log("[UserSettings] Remote settings empty; using local.");
                LoadFromLocal();
                return;
            }

            UnityMainThreadApply(raw);
            Debug.Log("[UserSettings] Remote settings applied.");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[UserSettings] Remote load failed: " + e);
            LoadFromLocal();
        }
    }

    // helper to apply loaded JSON on main thread (continue on Unity context)
    private void UnityMainThreadApply(string rawJson)
    {
        // If called from Firebase thread, schedule on main thread using TaskScheduler
        try
        {
            JsonUtility.FromJsonOverwrite(rawJson, prefs);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[UserSettings] FromJsonOverwrite failed: " + e + " raw: " + rawJson);
            LoadFromLocal();
            return;
        }

        // apply to UI
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(prefs.bgmVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(prefs.sfxVolume);

        // cache locally
        PlayerPrefs.SetFloat("prefs.bgmVolume", prefs.bgmVolume);
        PlayerPrefs.SetFloat("prefs.sfxVolume", prefs.sfxVolume);
        PlayerPrefs.Save();

        SetFeedbackOnMainThread("Settings loaded.");
        Debug.Log("[UserSettings] Settings applied from Firebase.");
    }

    private void LoadFromLocal()
    {
        prefs.bgmVolume = PlayerPrefs.GetFloat("prefs.bgmVolume", prefs.bgmVolume);
        prefs.sfxVolume = PlayerPrefs.GetFloat("prefs.sfxVolume", prefs.sfxVolume);

        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(prefs.bgmVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(prefs.sfxVolume);

        SetFeedback("Loaded local settings.");
    }

    private string GetUidOrGuest(FirebaseAuth auth)
    {
        if (auth != null && auth.CurrentUser != null)
            return auth.CurrentUser.UserId;
        return "guest_" + SystemInfo.deviceUniqueIdentifier;
    }

    // UI feedback helpers
    private void SetFeedback(string msg)
    {
        if (feedbackText != null)
        {
            feedbackText.text = msg;
            CancelInvoke(nameof(ClearFeedback));
            Invoke(nameof(ClearFeedback), 2f);
        }
    }

    private void SetFeedbackOnMainThread(string msg)
    {
        // Called from background thread continuations; schedule on main thread
        // Use Unity's synchronization by posting via coroutine
        StartCoroutine(SetFeedbackNextFrame(msg));
    }

    private System.Collections.IEnumerator SetFeedbackNextFrame(string msg)
    {
        yield return null;
        SetFeedback(msg);
    }
}