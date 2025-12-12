using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Name : Waine Low
/// Description : Tracks images in AR and spawns associated prefabs, with options to keep pets loaded and play jump SFX.
/// Date : 9 November 2025
/// </summary>
public class ImageTracker : MonoBehaviour
{
    [SerializeField]
    private ARTrackedImageManager trackedImageManager;

    [SerializeField]
    private GameObject[] placeablePrefabs;

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

    /// Subscribes to tracked image changes and prepares spawned prefabs; caches the AR camera.
    private void Start()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.AddListener(OnImageChanged);
            SetupPrefabs();
        }

        arCamera = Camera.main;
    }

    /// Instantiates and caches prefabs used for tracked images; keeps them inactive initially.
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

    /// Handles added/updated/removed tracked images by updating associated instances.
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

    /// Updates the spawned instance state for a tracked image (attach/detach, save stats as configured).
    void UpdateImage(ARTrackedImage trackedImage)
    {
        if(trackedImage != null)
        {
            var key = trackedImage.referenceImage.name;
            var instance = spawnedPrefabs.ContainsKey(key) ? spawnedPrefabs[key] : null;
            if (trackedImage.trackingState == TrackingState.Limited || trackedImage.trackingState == TrackingState.None)
            {
                if (keepPetLoadedWhenLost && instance != null)
                {
                    if (autoSaveOnLost)
                    {
                        var petComp = instance.GetComponentInChildren<PetStatsComponent>();
                        if (petComp != null && PetTracker.Instance != null && petComp.stats != null)
                        {
                            PetTracker.Instance.SavePetStats(petComp.stats);
                            Debug.Log("Auto-saved pet stats on tracking lost.");
                        }
                    }

                    instance.transform.SetParent(null);
                    instance.transform.position = trackedImage.transform.position;
                    instance.transform.rotation = trackedImage.transform.rotation;
                }
                else if (instance != null)
                {
                    if (autoSaveOnLost)
                    {
                        var petComp = instance.GetComponentInChildren<PetStatsComponent>();
                        if (petComp != null && PetTracker.Instance != null && petComp.stats != null)
                        {
                            PetTracker.Instance.SavePetStats(petComp.stats);
                            Debug.Log("Auto-saved pet stats before deactivating instance.");
                        }
                    }

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

                    var petComp = instance.GetComponentInChildren<PetStatsComponent>();
                    if (petComp != null && PetTracker.Instance != null)
                    {
                        PetTracker.Instance.LoadPetStatsFor(petComp);
                        PetTracker.Instance.RegisterCurrentPet(petComp);
                    }
                }
            }
        }
    }

    /// Detects pointer/touch and triggers jump on tapped spawned instances.
    private void Update()
    {
        if (arCamera == null) return;

        Vector2? screenPos = null;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        if (Input.GetMouseButtonDown(0))
        {
            screenPos = Input.mousePosition;
        }
#endif

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

    /// Performs a simple jump animation for the target and increases happiness, playing SFX if applicable.
    private IEnumerator Jump(Transform target)
    {
        if (target == null) yield break;
        if (jumpingObjects.Contains(target)) yield break;

        jumpingObjects.Add(target);

        float duration = 0.5f;
        float height = 0.2f;
        Vector3 start = target.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float y = Mathf.Sin(t * Mathf.PI) * height;
            target.localPosition = start + new Vector3(0f, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localPosition = start;

        var petComp = target.GetComponentInChildren<PetStatsComponent>();
        if (petComp != null && petComp.stats != null)
        {
            float prevHappiness = petComp.stats.petHappiness;
            float newHappiness = Mathf.Min(100f, prevHappiness + 5f);
            petComp.stats.petHappiness = newHappiness;
            Debug.Log($"Pet happiness increased to {petComp.stats.petHappiness}");

            if (newHappiness > prevHappiness && jumpSFX != null && jumpSFX.Length > 0)
            {
                AudioClip clip = null;
                for (int attempts = 0; attempts < 4; attempts++)
                {
                    var candidate = jumpSFX[UnityEngine.Random.Range(0, jumpSFX.Length)];
                    if (candidate != null) { clip = candidate; break; }
                }
                if (clip == null)
                {
                    foreach (var c in jumpSFX) { if (c != null) { clip = c; break; } }
                }

                if (clip != null)
                {
                    AudioSource.PlayClipAtPoint(clip, target.position, jumpSFXVolume);
                }
            }
        }

        jumpingObjects.Remove(target);
    }
}