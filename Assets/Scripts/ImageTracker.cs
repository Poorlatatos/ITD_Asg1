using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Name : Jaasper Lee
/// Description : Tracks images in AR and spawns associated prefabs, with options to keep pets loaded and play jump SFX.
/// Date : 18 November 2025
/// </summary>
public class ImageTracker : MonoBehaviour
{
    [SerializeField]
    private ARTrackedImageManager trackedImageManager;

    [SerializeField]
    private GameObject[] placeablePrefabs;

    // New: if true, keep the pet instance active in the scene when tracking is lost
    [SerializeField]
    private bool keepPetLoadedWhenLost = true;

    [SerializeField]
    private bool autoSaveOnLost = true;

    [Header("Jump SFX")]
    [Tooltip("List of sound effects to pick one from when the pet jumps and happiness increases.")]
    [SerializeField]
    private AudioClip[] jumpSFX;
    [Range(0f,1f)]
    [SerializeField]
    private float jumpSFXVolume = 1f;

    private Dictionary<string, GameObject> spawnedPrefabs = new Dictionary<string, GameObject>();

    private Dictionary<GameObject, GameObject> spawnedObjects = new Dictionary<GameObject, GameObject>();

    private string[] someArray = new string[]{"Image1", "Image2", "Image3"};
    private Camera arCamera;
    private HashSet<Transform> jumpingObjects = new HashSet<Transform>();

    private void Start()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.AddListener(OnImageChanged);
            SetupPrefabs();
        }

        // cache AR camera (typically the MainCamera)
        arCamera = Camera.main;
    }

    void SetupPrefabs()
    {
        foreach (GameObject prefab in placeablePrefabs)
        {
            GameObject newPrefab = Instantiate(prefab);
            newPrefab.name = prefab.name;
            newPrefab.SetActive(false);
            spawnedPrefabs.Add(prefab.name, newPrefab);
            spawnedObjects.Add(newPrefab, prefab);
        }
    }

    void OnImageChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            UpdateImage(trackedImage);
        }

        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            UpdateImage(trackedImage);
        }

        foreach (KeyValuePair<TrackableId, ARTrackedImage> lostObj in eventArgs.removed)
        {
            UpdateImage(lostObj.Value);
        }
    }

    void UpdateImage(ARTrackedImage trackedImage)
    {
        if(trackedImage != null)
        {
            var key = trackedImage.referenceImage.name;
            var instance = spawnedPrefabs.ContainsKey(key) ? spawnedPrefabs[key] : null;
            if (trackedImage.trackingState == TrackingState.Limited || trackedImage.trackingState == TrackingState.None)
            {
                // If configured to keep the pet loaded, detach and keep it active at the last known world pose.
                if (keepPetLoadedWhenLost && instance != null)
                {
                    // Auto-save runtime stats before detaching (so we save the in-game state, not the prefab)
                    if (autoSaveOnLost)
                    {
                        var petComp = instance.GetComponentInChildren<PetStatsComponent>();
                        if (petComp != null && PetTracker.Instance != null && petComp.stats != null)
                        {
                            PetTracker.Instance.SavePetStats(petComp.stats);
                            Debug.Log("Auto-saved pet stats on tracking lost.");
                        }
                    }

                    // detach from trackedImage so it stays in world space
                    instance.transform.SetParent(null);
                    // copy the last known pose so it doesn't snap elsewhere
                    instance.transform.position = trackedImage.transform.position;
                    instance.transform.rotation = trackedImage.transform.rotation;
                    // leave active so the pet remains visible and interactive
                }
                else if (instance != null)
                {
                    // If configured to auto-save, write before disabling
                    if (autoSaveOnLost)
                    {
                        var petComp = instance.GetComponentInChildren<PetStatsComponent>();
                        if (petComp != null && PetTracker.Instance != null && petComp.stats != null)
                        {
                            PetTracker.Instance.SavePetStats(petComp.stats);
                            Debug.Log("Auto-saved pet stats before deactivating instance.");
                        }
                    }

                    // default behaviour: disable and detach
                    instance.transform.SetParent(null);
                    instance.SetActive(false);
                }
            }
            else if (trackedImage.trackingState == TrackingState.Tracking)
            {
                if(instance != null && instance.transform.parent != trackedImage.transform)
                {
                    Debug.Log("Enabling associated content: " + instance.name);
                    instance.transform.SetParent(trackedImage.transform);
                    instance.transform.localPosition = spawnedObjects[instance].transform.localPosition;
                    instance.transform.localRotation = spawnedObjects[instance].transform.localRotation;

                    instance.SetActive(true);

                    // If the spawned instance has a PetStatsComponent, request load from Firebase
                    var petComp = instance.GetComponentInChildren<PetStatsComponent>();
                    if (petComp != null && PetTracker.Instance != null)
                    {
                        PetTracker.Instance.LoadPetStatsFor(petComp);
                        // register the runtime instance so UI/save uses the in-game data
                        PetTracker.Instance.RegisterCurrentPet(petComp);
                    }
                }
            }
        }
    }

    // New: handle touch -> raycast -> jump
    private void Update()
    {
        if (arCamera == null) return;

        Vector2? screenPos = null;

        // Desktop / Editor: mouse left click
        #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        if (Input.GetMouseButtonDown(0))
        {
            screenPos = Input.mousePosition;
        }
        #endif

        // Mobile: touch
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                screenPos = touch.position;
            }
        }

        if (!screenPos.HasValue) return;

        Ray ray = arCamera.ScreenPointToRay(screenPos.Value);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObj = hit.collider.gameObject;

            // check if the hit object is one of our spawned prefabs or a child
            foreach (var kv in spawnedPrefabs)
            {
                GameObject spawnedInstance = kv.Value;
                if (spawnedInstance == null) continue;

                if (hitObj == spawnedInstance || hitObj.transform.IsChildOf(spawnedInstance.transform) || hitObj.transform.root == spawnedInstance.transform)
                {
                    StartCoroutine(Jump(spawnedInstance.transform));
                    break;
                }
            }
        }
    }

    private IEnumerator Jump(Transform target)
    {
        if (target == null) yield break;
        if (jumpingObjects.Contains(target)) yield break;

        jumpingObjects.Add(target);

        float duration = 0.5f; // total up+down time
        float height = 0.2f;   // jump height in local units
        Vector3 start = target.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration; // 0..1
            float y = Mathf.Sin(t * Mathf.PI) * height; // smooth up then down
            target.localPosition = start + new Vector3(0f, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localPosition = start;

        // Increase pet happiness if the prefab has a PetStats component
        var petComp = target.GetComponentInChildren<PetStatsComponent>();
        if (petComp != null && petComp.stats != null)
        {
            float prevHappiness = petComp.stats.petHappiness;
            float newHappiness = Mathf.Min(100f, prevHappiness + 5f);
            petComp.stats.petHappiness = newHappiness;
            Debug.Log($"Pet happiness increased to {petComp.stats.petHappiness}");

            // Play a single random SFX from the list when happiness actually increases
            if (newHappiness > prevHappiness && jumpSFX != null && jumpSFX.Length > 0)
            {
                AudioClip clip = null;
                // choose a random non-null clip
                for (int attempts = 0; attempts < 4; attempts++)
                {
                    var candidate = jumpSFX[UnityEngine.Random.Range(0, jumpSFX.Length)];
                    if (candidate != null) { clip = candidate; break; }
                }
                // fallback to first non-null if above failed
                if (clip == null)
                {
                    foreach (var c in jumpSFX) { if (c != null) { clip = c; break; } }
                }

                if (clip != null)
                {
                    // play at the pet's world position as a one-shot
                    AudioSource.PlayClipAtPoint(clip, target.position, jumpSFXVolume);
                }
            }
        }

        jumpingObjects.Remove(target);
    }
}