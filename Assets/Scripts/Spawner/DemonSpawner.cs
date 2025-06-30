using System.Collections.Generic;
using UnityEngine;

public class DemonSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float minSpawnDistance = 15f; // Minimum distance from player
    [SerializeField] private float maxSpawnDistance = 25f; // Maximum distance from player
    [SerializeField] private float spawnInterval = 2f; // Time between spawns
    [SerializeField] private int maxDemonsAlive = 20; // Maximum number of demons alive at once

    [Header("Demon Settings")]
    [SerializeField] private List<DemonModel> demonTypes = new List<DemonModel>();
    [SerializeField] private GameObject demonPrefab; // Basic prefab to be used as template
    
    [Header("Object Pool Settings")]
    [SerializeField] private int initialPoolSize = 30; // Initial size of the object pool
    [SerializeField] private int maxPoolSize = 50; // Maximum size of the object pool

    // Tracking variables
    private float nextSpawnTime;
    private List<GameObject> activeDemonsList = new List<GameObject>();
    private Transform demonContainer; // Parent for spawned demons
    
    // Object Pool
    private Queue<GameObject> demonPool = new Queue<GameObject>();

    private void Awake()
    {
        // Try to find the player if not assigned
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("Player not found! Please assign the player transform or tag your player as 'Player'.");
            }
        }

        // Try to find main camera if not assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main camera not found! Please assign the camera reference.");
            }
        }

        // Create a container for all spawned demons
        demonContainer = new GameObject("Demons Container").transform;

        // Verify we have demon types
        if (demonTypes.Count == 0)
        {
            Debug.LogWarning("No demon types assigned to the spawner! Please assign DemonModel scriptable objects.");
        }
        
        // Initialize the object pool
        InitializeObjectPool();
    }
    
    private void InitializeObjectPool()
    {
        // Create initial pool of inactive demons
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject demon = Instantiate(demonPrefab, Vector3.zero, Quaternion.identity, demonContainer);
            
            // Make sure the demon has a DemonController
            DemonController controller = demon.GetComponent<DemonController>();
            if (controller == null)
            {
                controller = demon.AddComponent<DemonController>();
            }
            
            // Ensure the demon has an Animator component if the prefab doesn't already have one
            if (demon.GetComponent<Animator>() == null)
            {
                demon.AddComponent<Animator>();
            }
            
            // Set the demon to inactive and add to pool
            demon.SetActive(false);
            demonPool.Enqueue(demon);
        }
    }
    
    // Get a demon from the pool or create a new one if needed
    private GameObject GetDemonFromPool()
    {
        GameObject demon;
        
        if (demonPool.Count > 0)
        {
            // Get existing demon from pool
            demon = demonPool.Dequeue();
        }
        else if (activeDemonsList.Count + demonPool.Count < maxPoolSize)
        {
            // Create a new demon if we haven't reached max pool size
            demon = Instantiate(demonPrefab, Vector3.zero, Quaternion.identity, demonContainer);
        }
        else
        {
            // We've reached the maximum pool size, return null
            Debug.LogWarning("Maximum pool size reached. Cannot spawn more demons.");
            return null;
        }
        
        // Activate the demon
        demon.SetActive(true);
        return demon;
    }
    
    // Return a demon to the pool instead of destroying it
    public void ReturnDemonToPool(GameObject demon)
    {
        // Remove from active list
        activeDemonsList.Remove(demon);
        
        // Reset demon state
        DemonController controller = demon.GetComponent<DemonController>();
        if (controller != null)
        {
            controller.ResetState();
        }
        
        // Ensure animator is completely reset
        Animator animator = demon.GetComponent<Animator>();
        if (animator != null)
        {
            // Reset all parameters to defaults
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Bool)
                    animator.SetBool(param.name, false);
                else if (param.type == AnimatorControllerParameterType.Float)
                    animator.SetFloat(param.name, 0f);
                else if (param.type == AnimatorControllerParameterType.Int)
                    animator.SetInteger(param.name, 0);
            }
            
            // Reset any triggers
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Hit");
            animator.ResetTrigger("Death");
            
            // Force animation state to reset
            animator.Rebind();
            animator.Update(0f);
        }
        
        // Deactivate and return to pool
        demon.SetActive(false);
        demonPool.Enqueue(demon);
    }

    private void Start()
    {
        // Set initial spawn time
        nextSpawnTime = Time.time + spawnInterval;
    }

    private void Update()
    {
        // Clean up any null references in our active demons list
        CleanupDemonsList();

        // Check if it's time to spawn and we haven't reached the maximum
        if (Time.time >= nextSpawnTime && activeDemonsList.Count < maxDemonsAlive)
        {
            SpawnDemon();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void SpawnDemon()
    {
        // Make sure we have necessary references
        if (playerTransform == null || demonTypes.Count == 0 || demonPrefab == null)
        {
            return;
        }

        // Get spawn position outside of camera view
        Vector2 spawnPosition = GetSpawnPositionOutsideView();

        // Select a random demon type
        DemonModel selectedDemonType = demonTypes[Random.Range(0, demonTypes.Count)];

        // Get a demon from the pool instead of instantiating
        GameObject newDemon = GetDemonFromPool();
        
        // If we couldn't get a demon from the pool, exit
        if (newDemon == null)
        {
            return;
        }
        
        // Set position
        newDemon.transform.position = spawnPosition;

        // Configure the demon with the selected type
        DemonController demonController = newDemon.GetComponent<DemonController>();
        
        // Ensure Animator is properly reset before initializing
        Animator animator = newDemon.GetComponent<Animator>();
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
        
        // Initialize the demon
        demonController.Initialize(selectedDemonType, playerTransform);
        
        // Register the demon's death callback to return it to the pool
        demonController.OnDemonDeath += HandleDemonDeath;

        // Add to our active demons list
        activeDemonsList.Add(newDemon);
    }
    
    private void HandleDemonDeath(GameObject demon)
    {
        // Unregister the event to prevent memory leaks
        DemonController controller = demon.GetComponent<DemonController>();
        if (controller != null)
        {
            controller.OnDemonDeath -= HandleDemonDeath;
        }
        
        // Return the demon to the pool
        ReturnDemonToPool(demon);
    }

    private Vector2 GetSpawnPositionOutsideView()
    {
        // Calculate a position outside camera view but within our specified distance range

        // Get the camera bounds
        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        // Calculate a random angle
        float angle = Random.Range(0f, 2f * Mathf.PI);

        // Calculate distance, ensuring it's outside camera view
        float minDistFromCamera = Mathf.Max(minSpawnDistance, Mathf.Max(cameraWidth, cameraHeight) * 0.75f);
        float distance = Random.Range(minDistFromCamera, maxSpawnDistance);

        // Get position in world space
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        Vector2 spawnPosition = (Vector2)playerTransform.position + offset;

        return spawnPosition;
    }

    private void SetupBasicDemonVisual(GameObject demon, DemonModel demonModel)
    {
     
    }

    private void CleanupDemonsList()
    {
        // Remove any null entries from the list (from destroyed demons)
        activeDemonsList.RemoveAll(item => item == null);
    }
    
    // Call this on application quit or scene unload to clean up
    private void OnDestroy()
    {
        // Clear all callbacks to prevent memory leaks
        foreach (GameObject demon in activeDemonsList)
        {
            if (demon != null)
            {
                DemonController controller = demon.GetComponent<DemonController>();
                if (controller != null)
                {
                    controller.OnDemonDeath -= HandleDemonDeath;
                }
            }
        }
    }
}
