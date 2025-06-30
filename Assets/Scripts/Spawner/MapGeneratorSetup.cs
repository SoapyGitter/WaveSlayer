using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[ExecuteInEditMode]
public class MapGeneratorSetup : MonoBehaviour
{
    [Header("Performance Settings")]
    [SerializeField] private PerformancePreset performancePreset = PerformancePreset.Balanced;
    
    [Header("Map Size Settings")]
    [SerializeField] private float playerVisibilityDistance = 15f;
    
    [Header("Generator References")]
    [SerializeField] private DynamicMapGenerator mapGenerator;
    
    public enum PerformancePreset
    {
        HighPerformance,
        Balanced,
        HighQuality
    }
    
    private void OnValidate()
    {
        UpdateSettings();
    }
    
    private void UpdateSettings()
    {
        if (mapGenerator == null)
        {
            mapGenerator = GetComponent<DynamicMapGenerator>();
            if (mapGenerator == null)
            {
                mapGenerator = gameObject.AddComponent<DynamicMapGenerator>();
            }
        }
        
        // Get serialized object to modify properties
        SerializedObject serializedMapGenerator = new SerializedObject(mapGenerator);
        
        // Apply performance preset
        switch (performancePreset)
        {
            case PerformancePreset.HighPerformance:
                serializedMapGenerator.FindProperty("maxPrefabsToSpawn").intValue = 50;
                serializedMapGenerator.FindProperty("enableDistanceFromPlayer").floatValue = playerVisibilityDistance * 1.1f;
                serializedMapGenerator.FindProperty("disableDistanceFromPlayer").floatValue = playerVisibilityDistance * 1.5f;
                serializedMapGenerator.FindProperty("minDistanceBetweenPrefabs").floatValue = 8f;
                serializedMapGenerator.FindProperty("updateInterval").floatValue = 0.3f;
                serializedMapGenerator.FindProperty("visibilityCheckInterval").floatValue = 0.5f;
                break;
                
            case PerformancePreset.Balanced:
                serializedMapGenerator.FindProperty("maxPrefabsToSpawn").intValue = 100;
                serializedMapGenerator.FindProperty("enableDistanceFromPlayer").floatValue = playerVisibilityDistance * 1.2f;
                serializedMapGenerator.FindProperty("disableDistanceFromPlayer").floatValue = playerVisibilityDistance * 1.8f;
                serializedMapGenerator.FindProperty("minDistanceBetweenPrefabs").floatValue = 5f;
                serializedMapGenerator.FindProperty("updateInterval").floatValue = 0.2f;
                serializedMapGenerator.FindProperty("visibilityCheckInterval").floatValue = 0.3f;
                break;
                
            case PerformancePreset.HighQuality:
                serializedMapGenerator.FindProperty("maxPrefabsToSpawn").intValue = 200;
                serializedMapGenerator.FindProperty("enableDistanceFromPlayer").floatValue = playerVisibilityDistance * 1.3f;
                serializedMapGenerator.FindProperty("disableDistanceFromPlayer").floatValue = playerVisibilityDistance * 2.2f;
                serializedMapGenerator.FindProperty("minDistanceBetweenPrefabs").floatValue = 3f;
                serializedMapGenerator.FindProperty("updateInterval").floatValue = 0.1f;
                serializedMapGenerator.FindProperty("visibilityCheckInterval").floatValue = 0.2f;
                break;
        }
        
        // Apply changes
        serializedMapGenerator.ApplyModifiedProperties();
    }
    
    [ContextMenu("Apply Settings Now")]
    public void ApplySettingsNow()
    {
        UpdateSettings();
        Debug.Log("Map generator settings applied with " + performancePreset.ToString() + " preset.");
    }
}

// Custom editor for MapGeneratorSetup
[CustomEditor(typeof(MapGeneratorSetup))]
public class MapGeneratorSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        MapGeneratorSetup mgSetup = (MapGeneratorSetup)target;
        
        if (GUILayout.Button("Apply Settings"))
        {
            mgSetup.ApplySettingsNow();
        }
        
        if (GUILayout.Button("Create New Prefab Profile"))
        {
            // Create asset
            PrefabSpawnProfile newProfile = CreateInstance<PrefabSpawnProfile>();
            
            // Save it to the Assets/Scriptable Objects folder
            string path = "Assets/Scripts/Scriptable Objects/New Prefab Spawn Profile.asset";
            
            // Ensure directories exist
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            AssetDatabase.CreateAsset(newProfile, path);
            AssetDatabase.SaveAssets();
            
            // Focus on the new asset
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newProfile;
            
            Debug.Log("Created new Prefab Spawn Profile at " + path);
        }
    }
}
#endif 