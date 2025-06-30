using UnityEngine;

[CreateAssetMenu(fileName = "New Prefab Spawn Profile", menuName = "Wave Slayer/Prefab Spawn Profile")]
public class PrefabSpawnProfile : ScriptableObject
{
    [System.Serializable]
    public class PrefabGroup
    {
        public string groupName = "Default Group";
        public GameObject[] prefabs = new GameObject[0];
        [Range(0, 1)] public float spawnProbability = 1f;
        public int maxInstances = 100;
        [Tooltip("How densely packed this prefab type can be")]
        public float minDistanceBetweenSameType = 10f;
        [Tooltip("How far this prefab type should stay from other types")]
        public float minDistanceFromOtherTypes = 5f;
        [Tooltip("Optional custom layer for these prefabs")]
        public int customLayer = 0;
        [Tooltip("Randomize rotation when spawning")]
        public bool randomizeRotation = true;
        [Range(0, 360)]
        public float minRotation = 0f;
        [Range(0, 360)]
        public float maxRotation = 360f;
        [Tooltip("Random scale variation")]
        public bool randomizeScale = false;
        public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
        
        [Header("Advanced")]
        [Tooltip("If true, these prefabs will be treated as a single large grid cell")]
        public bool treatAsSingleCell = false;
    }
    
    [Header("Prefab Groups")]
    public PrefabGroup[] prefabGroups = new PrefabGroup[0];
    
    [Header("Global Spawn Settings")]
    [Tooltip("Master chance multiplier for all prefabs in this profile")]
    [Range(0, 1)]
    public float globalSpawnChance = 1.0f;
    
    [Tooltip("If true, all prefab groups will use random selection")]
    public bool useRandomPrefabsInGroups = true;
    
    [Tooltip("If false, rotation and scale settings per group will be ignored")]
    public bool applyTransformSettings = true;
} 