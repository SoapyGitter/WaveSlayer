using System.Collections.Generic;
using UnityEngine;

public class DynamicMapGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera playerCamera;

    [Header("Prefab Settings")]
    [Tooltip("Basic prefabs to spawn - for simple use cases")]
    [SerializeField] private GameObject[] prefabsToSpawn;
    [Tooltip("Advanced prefab configuration - overrides simple prefabs if provided")]
    [SerializeField] private PrefabSpawnProfile[] spawnProfiles;
    [SerializeField] private bool useRandomPrefabs = true;
    
    [Header("Spawn Settings")]
    [SerializeField] private float enableDistanceFromPlayer = 20f;
    [SerializeField] private float disableDistanceFromPlayer = 30f;
    [SerializeField] private float minDistanceBetweenPrefabs = 5f;
    [SerializeField] private int maxPrefabsToSpawn = 100;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(100f, 100f);
    
    [Header("Optimization Settings")]
    [SerializeField] private bool disableWhenNotVisible = true;
    [SerializeField] private float visibilityCheckInterval = 0.5f;
    [SerializeField] private LayerMask visibilityLayerMask;
    [SerializeField] private float updateInterval = 0.2f;

    // Containers for spawned objects
    private List<MapObject> allSpawnedObjects = new List<MapObject>();
    private List<MapObject> activeObjects = new List<MapObject>();
    private Dictionary<Vector2Int, bool> occupiedCells = new Dictionary<Vector2Int, bool>();
    private float gridCellSize;
    
    private float lastUpdateTime;
    private float lastVisibilityCheckTime;
    private bool initialized = false;
    
    // Track spawned objects by group
    private Dictionary<string, int> spawnCountByGroup = new Dictionary<string, int>();
    
    // Class to track spawned objects
    [System.Serializable]
    private class MapObject
    {
        public GameObject gameObject;
        public Vector3 position;
        public bool isActive;
        public bool isEnabled;
        public Renderer[] renderers;
        public Collider[] colliders;
        public string groupId;
        
        public MapObject(GameObject obj, Vector3 pos, string group = "default")
        {
            gameObject = obj;
            position = pos;
            isActive = true;
            isEnabled = true;
            renderers = obj.GetComponentsInChildren<Renderer>();
            colliders = obj.GetComponentsInChildren<Collider>();
            groupId = group;
        }
        
        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
            gameObject.SetActive(enabled);
        }
    }

    private void Start()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerBase>()?.transform;
            if (player == null)
            {
                Debug.LogError("DynamicMapGenerator: Player not assigned and couldn't be found!");
                enabled = false;
                return;
            }
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("DynamicMapGenerator: Player camera not assigned and couldn't be found!");
                enabled = false;
                return;
            }
        }

        gridCellSize = minDistanceBetweenPrefabs;
        initialized = true;
        
        // Initialize spawn count dictionary
        InitializeSpawnGroups();
        
        // Generate the full map and disable objects
        GenerateFullMap();
    }
    
    private void InitializeSpawnGroups()
    {
        spawnCountByGroup.Clear();
        
        // Add default group for simple prefabs
        spawnCountByGroup["default"] = 0;
        
        // Add groups from profiles
        if (spawnProfiles != null)
        {
            foreach (var profile in spawnProfiles)
            {
                if (profile != null)
                {
                    foreach (var group in profile.prefabGroups)
                    {
                        string groupKey = profile.name + "_" + group.groupName;
                        if (!spawnCountByGroup.ContainsKey(groupKey))
                        {
                            spawnCountByGroup[groupKey] = 0;
                        }
                    }
                }
            }
        }
    }

    private void Update()
    {
        if (!initialized) return;
        
        // Check if it's time to update map
        if (Time.time - lastUpdateTime > updateInterval)
        {
            UpdateObjectStates();
            lastUpdateTime = Time.time;
        }
        
        // Check if it's time to update visibility
        if (disableWhenNotVisible && Time.time - lastVisibilityCheckTime > visibilityCheckInterval)
        {
            UpdateObjectVisibility();
            lastVisibilityCheckTime = Time.time;
        }
    }

    private void GenerateFullMap()
    {
        // Clear any existing objects
        ClearAllObjects();
        
        // Calculate area to cover
        float spawnRadius = disableDistanceFromPlayer * 1.2f; // Spawn beyond the disable distance
        
        // Get player position
        Vector3 playerPos = player.position;
        
        // Calculate grid size based on minDistanceBetweenPrefabs
        int gridRadius = Mathf.CeilToInt(spawnRadius / minDistanceBetweenPrefabs);
        Vector2Int gridCenter = WorldToGridPosition(playerPos);
        
        // Generate objects in a grid centered on player
        int objectsSpawned = 0;
        
        // Create objects in grid pattern with some randomization
        for (int x = -gridRadius; x <= gridRadius && objectsSpawned < maxPrefabsToSpawn; x++)
        {
            for (int y = -gridRadius; y <= gridRadius && objectsSpawned < maxPrefabsToSpawn; y++)
            {
                // Convert grid position to world position with small randomization
                float randomOffsetX = Random.Range(-minDistanceBetweenPrefabs * 0.3f, minDistanceBetweenPrefabs * 0.3f);
                float randomOffsetY = Random.Range(-minDistanceBetweenPrefabs * 0.3f, minDistanceBetweenPrefabs * 0.3f);
                
                Vector3 spawnPos = new Vector3(
                    (gridCenter.x + x) * minDistanceBetweenPrefabs + randomOffsetX,
                    (gridCenter.y + y) * minDistanceBetweenPrefabs + randomOffsetY,
                    0
                );
                
                // Calculate distance from player
                float distanceToPlayer = Vector3.Distance(spawnPos, playerPos);
                
                // Skip if too close to player or outside desired radius
                if (distanceToPlayer < minDistanceBetweenPrefabs * 2 || distanceToPlayer > spawnRadius)
                    continue;
                    
                // Check if position is available
                if (CanSpawnAt(spawnPos))
                {
                    if (SpawnObjectAtPosition(spawnPos))
                    {
                        objectsSpawned++;
                    }
                }
            }
        }
        
        // Initialize object states
        UpdateObjectStates();
    }

    private void UpdateObjectStates()
    {
        if (player == null) return;
        
        Vector3 playerPos = player.position;
        
        // Clear active objects list
        activeObjects.Clear();
        
        // Update object enabled/disabled state based on distance
        foreach (MapObject mapObj in allSpawnedObjects)
        {
            float distanceToPlayer = Vector3.Distance(mapObj.position, playerPos);
            bool shouldBeEnabled = distanceToPlayer <= disableDistanceFromPlayer;
            
            // If object state needs to change
            if (mapObj.isEnabled != shouldBeEnabled)
            {
                mapObj.SetEnabled(shouldBeEnabled);
            }
            
            // If object is enabled, add to active objects list
            if (shouldBeEnabled)
            {
                activeObjects.Add(mapObj);
            }
        }
    }

    private bool SpawnObjectAtPosition(Vector3 position)
    {
        // Try to use spawn profiles first if available
        if (spawnProfiles != null && spawnProfiles.Length > 0)
        {
            return SpawnFromProfiles(position);
        }
        
        // Fall back to simple prefab spawning
        if (prefabsToSpawn != null && prefabsToSpawn.Length > 0)
        {
            GameObject newObj = SpawnBasicObject(position);
            if (newObj != null)
            {
                MapObject mapObject = new MapObject(newObj, position, "default");
                allSpawnedObjects.Add(mapObject);
                
                // Mark cell as occupied
                Vector2Int cellPos = WorldToGridPosition(position);
                occupiedCells[cellPos] = true;
                
                // Update group count
                spawnCountByGroup["default"]++;
                
                return true;
            }
        }
        
        return false;
    }
    
    private bool SpawnFromProfiles(Vector3 position)
    {
        List<PrefabSpawnProfile.PrefabGroup> eligibleGroups = new List<PrefabSpawnProfile.PrefabGroup>();
        List<string> eligibleGroupIds = new List<string>();
        
        // Collect eligible groups that haven't reached their max instances
        foreach (var profile in spawnProfiles)
        {
            if (profile == null) continue;
            
            foreach (var group in profile.prefabGroups)
            {
                if (group.prefabs == null || group.prefabs.Length == 0) continue;
                
                string groupKey = profile.name + "_" + group.groupName;
                
                // Only consider this group if it hasn't reached max instances
                if (!spawnCountByGroup.ContainsKey(groupKey) || spawnCountByGroup[groupKey] < group.maxInstances)
                {
                    // Check probability to include this group
                    float randomValue = Random.value;
                    float effectiveProbability = group.spawnProbability * profile.globalSpawnChance;
                    
                    if (randomValue <= effectiveProbability)
                    {
                        eligibleGroups.Add(group);
                        eligibleGroupIds.Add(groupKey);
                    }
                }
            }
        }
        
        // No eligible groups to spawn from
        if (eligibleGroups.Count == 0)
            return false;
        
        // Select random group from eligible ones
        int randomGroupIndex = Random.Range(0, eligibleGroups.Count);
        var selectedGroup = eligibleGroups[randomGroupIndex];
        string selectedGroupId = eligibleGroupIds[randomGroupIndex];
        
        // Find the profile that contains this group
        PrefabSpawnProfile parentProfile = null;
        foreach (var profile in spawnProfiles)
        {
            if (profile != null && selectedGroupId.StartsWith(profile.name + "_"))
            {
                parentProfile = profile;
                break;
            }
        }
        
        if (selectedGroup.prefabs.Length == 0)
            return false;
        
        // Choose which prefab to spawn
        GameObject prefabToSpawn;
        bool useRandom = parentProfile != null ? parentProfile.useRandomPrefabsInGroups : useRandomPrefabs;
        
        if (useRandom)
        {
            int randomIndex = Random.Range(0, selectedGroup.prefabs.Length);
            prefabToSpawn = selectedGroup.prefabs[randomIndex];
        }
        else
        {
            prefabToSpawn = selectedGroup.prefabs[0];
        }
        
        if (prefabToSpawn == null)
            return false;
            
        // Instantiate with rotation
        Quaternion rotation = Quaternion.identity;
        Vector3 scale = prefabToSpawn.transform.localScale;
        
        if (parentProfile != null && parentProfile.applyTransformSettings)
        {
            // Apply rotation
            if (selectedGroup.randomizeRotation)
            {
                float randomRotation = Random.Range(selectedGroup.minRotation, selectedGroup.maxRotation);
                rotation = Quaternion.Euler(0, 0, randomRotation);
            }
            
            // Apply scale
            if (selectedGroup.randomizeScale)
            {
                float randomScale = Random.Range(selectedGroup.scaleRange.x, selectedGroup.scaleRange.y);
                scale *= randomScale;
            }
        }
        
        // Spawn the object
        GameObject newObj = Instantiate(prefabToSpawn, position, rotation);
        newObj.transform.localScale = scale;
        
        // Set layer if specified
        if (selectedGroup.customLayer != 0)
        {
            newObj.layer = selectedGroup.customLayer;
        }
        
        // Add to tracking systems
        MapObject mapObject = new MapObject(newObj, position, selectedGroupId);
        allSpawnedObjects.Add(mapObject);
        
        // Mark cell as occupied
        Vector2Int cellPos = WorldToGridPosition(position);
        occupiedCells[cellPos] = true;
        
        // Update group counts
        if (!spawnCountByGroup.ContainsKey(selectedGroupId))
        {
            spawnCountByGroup[selectedGroupId] = 0;
        }
        spawnCountByGroup[selectedGroupId]++;
        
        return true;
    }
    
    private GameObject SpawnBasicObject(Vector3 position)
    {
        if (prefabsToSpawn == null || prefabsToSpawn.Length == 0)
            return null;
            
        // Choose a prefab to spawn
        GameObject prefabToSpawn;
        if (useRandomPrefabs)
        {
            int randomIndex = Random.Range(0, prefabsToSpawn.Length);
            prefabToSpawn = prefabsToSpawn[randomIndex];
        }
        else
        {
            prefabToSpawn = prefabsToSpawn[0];
        }
        
        if (prefabToSpawn == null)
            return null;
            
        // Instantiate the object with random rotation
        return Instantiate(prefabToSpawn, position, Quaternion.Euler(0, 0, Random.Range(0, 360f)));
    }
    
    private void UpdateObjectVisibility()
    {
        if (player == null || playerCamera == null) return;
        
        Plane[] frustrumPlanes = GeometryUtility.CalculateFrustumPlanes(playerCamera);
        
        // Only check visibility on active objects
        foreach (MapObject mapObj in activeObjects)
        {
            if (mapObj.gameObject == null || !mapObj.isEnabled) continue;
            
            bool isVisible = false;
            
            // Distance-based check - always enable if close enough
            float distanceToPlayer = Vector3.Distance(mapObj.position, player.position);
            if (distanceToPlayer < playerCamera.orthographicSize * 1.5f)
            {
                isVisible = true;
            }
            else
            {
                // Check if any renderer is visible in camera frustrum
                foreach (Renderer renderer in mapObj.renderers)
                {
                    if (renderer != null && GeometryUtility.TestPlanesAABB(frustrumPlanes, renderer.bounds))
                    {
                        isVisible = true;
                        break;
                    }
                }
            }
            
            // Update object active state if changed
            if (isVisible != mapObj.isActive)
            {
                mapObj.isActive = isVisible;
                
                // Enable/disable renderers and colliders instead of the whole GameObject
                // This is more efficient than activating/deactivating the entire GameObject
                foreach (Renderer renderer in mapObj.renderers)
                {
                    if (renderer != null)
                        renderer.enabled = isVisible;
                }
                
                foreach (Collider collider in mapObj.colliders)
                {
                    if (collider != null)
                        collider.enabled = isVisible;
                }
            }
        }
    }
    
    private bool CanSpawnAt(Vector3 worldPosition)
    {
        // Convert to grid position
        Vector2Int cellPos = WorldToGridPosition(worldPosition);
        
        // Check if cell is already occupied
        if (occupiedCells.ContainsKey(cellPos) && occupiedCells[cellPos])
            return false;
            
        // Check minimum distance to other objects
        foreach (MapObject obj in allSpawnedObjects)
        {
            if (Vector3.Distance(obj.position, worldPosition) < minDistanceBetweenPrefabs)
                return false;
        }
        
        return true;
    }
    
    private Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / gridCellSize);
        int y = Mathf.FloorToInt(worldPosition.y / gridCellSize);
        return new Vector2Int(x, y);
    }
    
    private void ClearAllObjects()
    {
        foreach (MapObject obj in allSpawnedObjects)
        {
            if (obj.gameObject != null)
            {
                Destroy(obj.gameObject);
            }
        }
        
        allSpawnedObjects.Clear();
        activeObjects.Clear();
        occupiedCells.Clear();
        
        // Reset spawn counts - create a temporary list of keys to avoid collection modification issues
        List<string> keys = new List<string>(spawnCountByGroup.Keys);
        foreach (var key in keys)
        {
            spawnCountByGroup[key] = 0;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || player == null) return;
        
        // Draw enable distance
        Gizmos.color = Color.green;
        DrawCircle(player.position, enableDistanceFromPlayer, 32);
        
        // Draw disable distance
        Gizmos.color = Color.red;
        DrawCircle(player.position, disableDistanceFromPlayer, 32);
    }
    
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angle = 0f;
        Vector3 prevPos = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
        
        for (int i = 0; i < segments + 1; i++)
        {
            angle = (i * 2 * Mathf.PI) / segments;
            Vector3 newPos = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPos, newPos);
            prevPos = newPos;
        }
    }
} 