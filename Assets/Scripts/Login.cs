using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using Firebase.Database;
using TMPro;

/// <summary>
/// Name : Jaasper Lee
/// Description : Manages user login and registration using Firebase Authentication and Realtime Database.
/// Date : 10 November 2025
/// </summary>

public class Login : MonoBehaviour
{
    public static Login Instance { get; private set; }

    [Header("UI")]
    [SerializeField] public TMP_InputField usernameField;
    [SerializeField] public TMP_InputField emailField;
    [SerializeField] public TMP_InputField passwordField;
    [SerializeField] public TextMeshProUGUI messageText;

    public string nextSceneName;
    public bool requireUsernameMatch = true;
    private FirebaseAuth auth;
    private FirebaseUser user;
    private DatabaseReference db;
    private bool signInRequested = false;
    private bool authListening = false;
    private Coroutine messageCoroutine;

    // simple username validation: 3-24 chars, letters, digits, underscore, dash
    private static readonly Regex UsernameRegex = new Regex(@"^[a-zA-Z0-9_-]{3,24}$");

    /// Initializes Firebase dependencies and registers scene load callback.
    private async void Awake()
    {
        var status = await Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
        if (status == Firebase.DependencyStatus.Available)
        {
            auth = FirebaseAuth.DefaultInstance;
            db = FirebaseDatabase.DefaultInstance.RootReference;
            Debug.Log("Firebase initialized.");
        }
        else
        {
            Debug.LogError($"Could not resolve Firebase dependencies: {status}");
            LogMsg("Database not initialized.");
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// Cleans up listeners on destroy.
    private void OnDestroy()
    {
        StopAuthListening();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    /// Attempts to (re)bind UI fields after a scene loads.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (usernameField == null) usernameField = UnityEngine.Object.FindFirstObjectByType<TMP_InputField>();
        if (emailField == null) emailField = UnityEngine.Object.FindFirstObjectByType<TMP_InputField>();
        if (passwordField == null)
        {
            var inputs = UnityEngine.Object.FindObjectsOfType<TMP_InputField>();
            foreach (var i in inputs) if (i != usernameField && i != emailField) { passwordField = i; break; }
        }
        if (messageText == null) messageText = UnityEngine.Object.FindFirstObjectByType<TextMeshProUGUI>();
    }

    /// Responds to Firebase auth state changes and optionally loads the next scene.
    private void OnAuthStateChanged(object sender, EventArgs e)
    {
        if (auth == null) return;
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;
            user = auth.CurrentUser;
            Debug.Log($"Auth state changed. Signed in: {user != null}  uid: {user?.UserId} displayName:{user?.DisplayName}");
            if (signedIn)
            {
                if (signInRequested && !string.IsNullOrEmpty(nextSceneName))
                {
                    signInRequested = false;
                    SceneManager.LoadScene(nextSceneName);
                }
            }
            else
            {
                signInRequested = false;
            }
        }
    }

    public bool IsSignedIn => auth != null && auth.CurrentUser != null;

    /// Validates inputs and initiates sign-in with provided username/email/password.
    public void OnLoginButtonPressed()
    {
        string username = usernameField ? usernameField.text.Trim() : "";
        string email = emailField ? emailField.text.Trim() : "";
        string pass = passwordField ? passwordField.text : "";

        Debug.Log($"OnLoginButtonPressed: username='{username}', email='{email}', passLen={(pass?.Length ?? 0)}");

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            LogMsg("Please enter username, email and password.");
            return;
        }

        if (!UsernameRegex.IsMatch(username))
        {
            LogMsg("Username invalid. Use 3-24 letters/numbers/_/-.");
            return;
        }

        StartAuthListening();
        signInRequested = true;
        SignIn(username, email, pass);
    }

    /// Validates inputs and registers a new user if the username is available.
    public void OnRegisterButtonPressed()
    {
        string username = usernameField ? usernameField.text.Trim() : "";
        string email = emailField ? emailField.text.Trim() : "";
        string pass = passwordField ? passwordField.text : "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            LogMsg("Please enter username, email and password to register.");
            return;
        }

        if (!UsernameRegex.IsMatch(username))
        {
            LogMsg("Username invalid. Use 3-24 letters/numbers/_/-.");
            return;
        }

        if (db == null)
        {
            LogMsg("Database not initialized.");
            return;
        }

        db.Child("users").Child(username).GetValueAsync().ContinueWith(t =>
        {
            if (t.IsCanceled || t.IsFaulted)
            {
                LogMsg("Could not verify username availability.");
                return;
            }

            var snap = t.Result;
            if (snap != null && snap.Exists)
            {
                LogMsg("Username already taken. Choose another.");
                return;
            }

            auth.CreateUserWithEmailAndPasswordAsync(email, pass).ContinueWith(createTask =>
            {
                if (createTask.IsCanceled || createTask.IsFaulted)
                {
                    var msg = createTask.Exception?.Flatten().Message ?? "Unknown error";
                    Debug.LogWarning("Register failed: " + msg);
                    LogMsg("Registration failed: please check details and try again.");
                    return;
                }

                var result = createTask.Result;
                var newUser = result.User;
                if (newUser == null)
                {
                    LogMsg("Registration failed: no user returned.");
                    return;
                }

                var profile = new UserProfile { DisplayName = username };
                newUser.UpdateUserProfileAsync(profile).ContinueWith(updateTask =>
                {
                    if (updateTask.IsCanceled || updateTask.IsFaulted)
                    {
                        Debug.LogWarning("Failed to set display name: " + updateTask.Exception?.Flatten().Message);
                    }

                    var uidKey = newUser.UserId;
                    db.Child("users").Child(uidKey).Child("username").SetValueAsync(username);
                    db.Child("users").Child(uidKey).Child("email").SetValueAsync(email);
                    db.Child("users").Child(uidKey).Child("uid").SetValueAsync(uidKey);
                    try { auth.SignOut(); } catch (Exception ex) { Debug.LogWarning("SignOut after register failed: " + ex.Message); }

                    user = null;
                    LogMsg("Registration successful. Please log in to continue.");
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    /// Starts listening to auth state changes.
    public void StartAuthListening()
    {
        if (auth == null) auth = FirebaseAuth.DefaultInstance;
        if (authListening || auth == null) return;
        auth.StateChanged += OnAuthStateChanged;
        authListening = true;
    }

    /// Stops listening to auth state changes.
    public void StopAuthListening()
    {
        if (!authListening || auth == null) return;
        auth.StateChanged -= OnAuthStateChanged;
        authListening = false;
    }

    /// Signs in with email/password and validates provided username against the account's display name when required.
    public void SignIn(string username, string email, string password)
    {
        if (auth == null) { LogMsg("Auth not initialized"); return; }

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                var msg = task.Exception?.Flatten().Message ?? "Unknown error";
                Debug.LogWarning("Login failed: " + msg);
                LogMsg("Login failed: check username/email/password and try again.");
                return;
            }

            var result = task.Result;
            var signedInUser = result.User;
            if (signedInUser == null)
            {
                LogMsg("Login failed: no user returned.");
                return;
            }

            Debug.Log($"SignIn success: uid={signedInUser.UserId} displayName='{signedInUser.DisplayName}'");

            
            if (requireUsernameMatch)
            {
                var registeredName = signedInUser.DisplayName ?? "";
                if (!string.Equals(registeredName, username, StringComparison.Ordinal))
                {
                    try { auth.SignOut(); } catch { }
                    LogMsg("Username does not match account. Please check username/email.");
                    return;
                }
            }

            user = signedInUser;
            LogMsg("Login successful");

            
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                Debug.Log($"Loading scene '{nextSceneName}' after sign in.");
                SceneManager.LoadScene(nextSceneName);
                signInRequested = false;
            }
            
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    /// Signs out the current user.
    public void SignOut()
    {
        if (auth == null) return;
        auth.SignOut();
        user = null;
        signInRequested = false;
        LogMsg("Signed out");
    }

    /// Displays a temporary message in the UI or logs it to console when UI is absent.
    private IEnumerator FlashMessageCoroutine(string s, float duration)
    {
        if (messageText != null)
        {
            messageText.text = s;
        }
        else
        {
            Debug.Log(s);
        }

        yield return new WaitForSeconds(duration);

        if (messageText != null)
            messageText.text = "";
    }

    /// Logs a message and shows it in the UI if available.
    private void LogMsg(string s, float duration = 3f)
    {
        Debug.Log(s);

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null;
        }

        if (messageText != null)
            messageCoroutine = StartCoroutine(FlashMessageCoroutine(s, duration));
    }
}