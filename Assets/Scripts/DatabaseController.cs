using UnityEngine;
using Firebase.Database;
using System.Collections.Generic;
using Firebase.Extensions;
using Unity.VisualScripting;
using TMPro;
using Firebase.Auth;
using System;

public class DatabaseController : MonoBehaviour
{
    /*
    public TMP_InputField EmailInput;
    public TMP_InputField PasswordInput;

    public void SignOut()
    {
        FirebaseAuth.DefaultInstance.SignOut();
    }

    public void SignUp()
    {
        var signupTask = FirebaseAuth.DefaultInstance.CreateUserWithEmailAndPasswordAsync(EmailInput.text, PasswordInput.text);
        signupTask.ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.Log("Can't sign in due to error!!!");
            }

            if (task.IsCompleted)
            {
                Debug.Log($"User logged in, id: {task.Result.User.UserId}");

                // Code to create user profile in database
            }
        });
    }

    public void SignIn()
    {
        var signInTask = FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(EmailInput.text, PasswordInput.text);
        signInTask.ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.Log("Can't sign in due to error!!!");
            }

            if (task.IsCompleted)
            {
                Debug.Log($"User logged in, id: {task.Result.User.UserId}");

                // Code to load the user profile
            }
        });

        Debug.Log("Hahahahaha");
    }

    // Auth event handling example
    private void OnAuthStateChanged(object sender, EventArgs e)
    {
        Debug.Log("Auth state changed!");

        if (FirebaseAuth.DefaultInstance.CurrentUser == null)
        {
            Debug.Log("User is not logged in!");
        }
        else
        {
            Debug.Log($"User logged in: {FirebaseAuth.DefaultInstance.CurrentUser.UserId}");
        }
    }

    void Start()
    {
        // Auth event handling example
        FirebaseAuth.DefaultInstance.StateChanged += OnAuthStateChanged;


        var db = FirebaseDatabase.DefaultInstance.RootReference;

        // Change a single value using SetValueAsync
        db.Child("players").Child("detach8").Child("score").SetValueAsync(9999);

        // Update using UpdateChildrenAsync
        // This is used to bulk update multiple values
        Dictionary<string, object> data = new Dictionary<string, object>();
        data["name"] = "Some awesome guy";
        data["score"] = 1234;
        db.Child("players").Child("detach8").UpdateChildrenAsync(data);

        // Delete the player "detach8"
        db.Child("players").Child("detach8").RemoveValueAsync();

        // Retrieve
        var retrieveTask = db.Child("players").Child("somenonexistentplayer").GetValueAsync();

        retrieveTask.ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.Log("Error loading player!");
                return;
            }

            if (task.IsCompleted)
            {
                if (!task.Result.Exists)
                {
                    Debug.Log("Invalid player id!");
                    return;
                }

                string json = task.Result.GetRawJsonValue();
                Debug.Log(json);

                // Deserialize JSON data back to Player object
                Player p = JsonUtility.FromJson<Player>(json);
                Debug.Log($"Player loaded: {p.name}");
            }
        });

        Debug.Log("Hehehehehehh");*/


        /***
         * BEFORE CRUD
         *
        Player justin = new Player("detach8", "Justin");
        justin.items.Add(new Item("sword", 2));

        Player steve = new Player("steviewonder", "Steve from Minecraft");
        steve.items.Add(new Item("pickaxe", 1));

        Player alex = new Player("alexinwonderland", "Alex from Minecraft");
        alex.items.Add(new Item("shovel", 1));

        string justinJson = JsonUtility.ToJson(justin, true);
        string steveJson = JsonUtility.ToJson(steve);

        Debug.Log(justinJson);
        Debug.Log(steveJson);

        db.Child("players").Child(justin.playerId).SetRawJsonValueAsync(justinJson);
        db.Child("players").Child(steve.playerId).SetRawJsonValueAsync(steveJson);


        var newReference = db.Child("players").Push();

        Debug.Log($"The key is: {newReference.Key}");

        alex.playerId = newReference.Key; // Store the new key
        string alexJson = JsonUtility.ToJson(alex);

        newReference.SetRawJsonValueAsync(alexJson);
        */
    }

    // Update is called once per frame
