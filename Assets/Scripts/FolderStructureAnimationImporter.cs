using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
public class FolderStructureAnimationImporter : EditorWindow
{
    // Folder structure path
    private string rootFolderPath = "Assets/Sprites/Player";

    // Animation settings
    private float frameRate = 24f;
    private bool loopAnimations = true;
    private string animationsSavePath = "Assets/Animations";
    private string animationNamePrefix = ""; // e.g., "Run", "Walk", etc.
    private bool overwriteExistingAnimations = false;

    // Animator settings
    private AnimatorController existingController;
    private string stateName = "Movement";
    private string controllerName = "DirectionalMovementController";
    private string controllerSavePath = "Assets/Animations/Controllers";

    // Parameter names
    private string xParameterName = "MoveX";
    private string yParameterName = "MoveY";

    // Animation clips for each direction
    private Dictionary<string, List<AnimationClip>> directionClips = new Dictionary<string, List<AnimationClip>>();
    private Dictionary<string, AnimationClip> generatedClips = new Dictionary<string, AnimationClip>();

    // Expected folder names
    private readonly string[] directionFolderNames = new string[] {
         "Top",
         "TopRight",
         "RightTopMiddle",
         "RightLeft",
         "Right",
         "RightTop",
         "BottomRightMiddle",
         "BottomRight",
         "Bottom",
         "BottomLeft",
         "LeftBottomMiddle",
         "LeftBottom",
         "Left",
         "LeftTop",
         "TopLeftMiddle",
         "TopLeft"
     };

    // Direction mapping to positions in the blend tree
    private readonly Dictionary<string, Vector2> directionPositions = new Dictionary<string, Vector2>() {
         { "Top", new Vector2(0, 1) },                             // 0°
         { "TopRight", new Vector2(0.3827f, 0.9239f) },            // 22.5°
         { "RightTopMiddle", new Vector2(0.7071f, 0.7071f) },      // 45°
         { "RightLeft", new Vector2(0.9239f, -0.3827f) },           // 67.5°
         { "Right", new Vector2(1, 0) },                           // 90°
         { "RightTop", new Vector2(0.9239f, 0.3827f) },        // 112.5°
         { "BottomRightMiddle", new Vector2(0.7071f, -0.7071f) },  // 135°
         { "BottomRight", new Vector2(0.3827f, -0.9239f) },        // 157.5°
         { "Bottom", new Vector2(0, -1) },                         // 180°
         { "BottomLeft", new Vector2(-0.3827f, -0.9239f) },        // 202.5°
         { "LeftBottomMiddle", new Vector2(-0.7071f, -0.7071f) },  // 225°
         { "LeftBottom", new Vector2(-0.9239f, -0.3827f) },        // 247.5°
         { "Left", new Vector2(-1, 0) },                           // 270°
         { "LeftTop", new Vector2(-0.9239f, 0.3827f) },            // 292.5°
         { "TopLeftMiddle", new Vector2(-0.7071f, 0.7071f) },      // 315°
         { "TopLeft", new Vector2(-0.3827f, 0.9239f) }             // 337.5°
     };

    [MenuItem("Tools/Directional Animation/Import From Folder Structure")]
    public static void ShowWindow()
    {
        GetWindow<FolderStructureAnimationImporter>("Animation Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Animation Importer from Folder Structure", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        GUILayout.Label("Folder Settings", EditorStyles.boldLabel);
        rootFolderPath = EditorGUILayout.TextField("Root Folder Path", rootFolderPath);

        if (GUILayout.Button("Browse...", GUILayout.Width(100)))
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select Root Folder", "Assets", "");
            if (!string.IsNullOrEmpty(folderPath))
            {
                // Convert absolute path to project relative path
                if (folderPath.StartsWith(Application.dataPath))
                {
                    rootFolderPath = "Assets" + folderPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Path", "Please select a folder inside your Unity project.", "OK");
                }
            }
        }

        EditorGUILayout.Space(10);
        GUILayout.Label("Animation Settings", EditorStyles.boldLabel);

        frameRate = EditorGUILayout.FloatField("Frame Rate", frameRate);
        loopAnimations = EditorGUILayout.Toggle("Loop Animations", loopAnimations);
        animationNamePrefix = EditorGUILayout.TextField("Animation Name Prefix", animationNamePrefix);
        animationsSavePath = EditorGUILayout.TextField("Animations Save Path", animationsSavePath);
        overwriteExistingAnimations = EditorGUILayout.Toggle("Overwrite Existing Animations", overwriteExistingAnimations);

        EditorGUILayout.Space(10);
        GUILayout.Label("Animator Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Create New or Modify Existing");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create New Controller", GUILayout.Height(30)))
        {
            existingController = null;
        }

        existingController = (AnimatorController)EditorGUILayout.ObjectField(
            existingController, typeof(AnimatorController), false, GUILayout.Height(30));
        EditorGUILayout.EndHorizontal();

        if (existingController == null)
        {
            controllerSavePath = EditorGUILayout.TextField("Controller Save Path", controllerSavePath);
            controllerName = EditorGUILayout.TextField("Controller Name", controllerName);
        }

        stateName = EditorGUILayout.TextField("State Name", stateName);
        xParameterName = EditorGUILayout.TextField("X Parameter Name", xParameterName);
        yParameterName = EditorGUILayout.TextField("Y Parameter Name", yParameterName);

        EditorGUILayout.Space(20);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Create Animations and Blend Tree", GUILayout.Height(40)))
        {
            CreateAnimationsAndBlendTree();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);

        if (directionClips.Count > 0)
        {
            GUILayout.Label("Found Animation Clips:", EditorStyles.boldLabel);

            foreach (var direction in directionClips.Keys)
            {
                EditorGUILayout.LabelField($"{direction}: {directionClips[direction].Count} clips");
            }
        }
    }

    private void CreateAnimationsAndBlendTree()
    {
        // Clear previous data
        directionClips.Clear();
        generatedClips.Clear();

        // Validate the root folder exists
        if (!AssetDatabase.IsValidFolder(rootFolderPath))
        {
            EditorUtility.DisplayDialog("Error", $"The folder {rootFolderPath} does not exist.", "OK");
            return;
        }

        // Initialize dictionary for all directions
        foreach (string direction in directionFolderNames)
        {
            directionClips[direction] = new List<AnimationClip>();
        }

        // Create the animations save folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(animationsSavePath))
        {
            string[] pathParts = animationsSavePath.Split('/');
            string currentPath = pathParts[0];

            for (int i = 1; i < pathParts.Length; i++)
            {
                string nextPathPart = pathParts[i];
                string testPath = $"{currentPath}/{nextPathPart}";

                if (!AssetDatabase.IsValidFolder(testPath))
                {
                    AssetDatabase.CreateFolder(currentPath, nextPathPart);
                }

                currentPath = testPath;
            }
        }

        // Scan subfolders and create animations
        string[] subfolders = AssetDatabase.GetSubFolders(rootFolderPath);
        bool createdSomeAnimations = false;

        foreach (string subfolder in subfolders)
        {
            string folderName = Path.GetFileName(subfolder);

            // Check if this is a direction folder
            if (directionClips.ContainsKey(folderName))
            {
                // Find existing animation clips in this folder
                string[] existingAnimGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { subfolder });
                foreach (string guid in existingAnimGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);

                    if (clip != null)
                    {
                        directionClips[folderName].Add(clip);
                        Debug.Log($"Found existing clip: {clip.name} in {folderName}");
                    }
                }

                // Create new animation from sprite sequence in the folder
                string clipName = string.IsNullOrEmpty(animationNamePrefix)
                    ? folderName
                    : $"{animationNamePrefix}_{folderName}";

                string savePath = $"{animationsSavePath}/{clipName}.anim";

                // Check if animation already exists
                bool animExists = File.Exists(savePath);
                AnimationClip existingAnim = null;

                if (animExists)
                {
                    existingAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>(savePath);
                    if (existingAnim != null && !overwriteExistingAnimations)
                    {
                        Debug.Log($"Using existing animation at {savePath}");
                        directionClips[folderName].Add(existingAnim);
                        generatedClips[folderName] = existingAnim;
                        continue;
                    }
                }

                // Find sprites in this direction folder
                string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { subfolder });

                if (spriteGuids.Length > 0)
                {
                    // Sort sprites by name
                    List<Sprite> sprites = new List<Sprite>();
                    foreach (string guid in spriteGuids)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                        if (sprite != null)
                        {
                            sprites.Add(sprite);
                        }
                    }

                    // Sort sprites by name
                    sprites = sprites.OrderBy(s => s.name).ToList();

                    // Create or update animation clip
                    AnimationClip animClip = existingAnim != null && overwriteExistingAnimations
                        ? existingAnim
                        : new AnimationClip();

                    animClip.frameRate = frameRate;

                    // Clear any existing keys
                    EditorCurveBinding spriteBinding = new EditorCurveBinding();
                    spriteBinding.type = typeof(SpriteRenderer);
                    spriteBinding.path = "";
                    spriteBinding.propertyName = "m_Sprite";

                    AnimationUtility.SetObjectReferenceCurve(animClip, spriteBinding, null);

                    // Create keyframes
                    ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
                    for (int i = 0; i < sprites.Count; i++)
                    {
                        keyframes[i] = new ObjectReferenceKeyframe();
                        keyframes[i].time = i / frameRate;
                        keyframes[i].value = sprites[i];
                    }

                    // Set animation settings
                    AnimationUtility.SetObjectReferenceCurve(animClip, spriteBinding, keyframes);

                    // Set looping
                    if (loopAnimations)
                    {
                        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(animClip);
                        settings.loopTime = true;
                        AnimationUtility.SetAnimationClipSettings(animClip, settings);
                    }

                    // Create or update asset
                    if (existingAnim != null && overwriteExistingAnimations)
                    {
                        EditorUtility.SetDirty(animClip);
                        Debug.Log($"Updated existing animation: {savePath}");
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(animClip, savePath);
                        Debug.Log($"Created new animation: {savePath}");
                    }

                    directionClips[folderName].Add(animClip);
                    generatedClips[folderName] = animClip;
                    createdSomeAnimations = true;
                }
                else
                {
                    Debug.LogWarning($"No sprites found in folder {folderName}");
                }
            }
        }

        if (createdSomeAnimations)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // Check if we found or created clips for all directions
        bool allDirectionsHaveClips = true;
        foreach (string direction in directionFolderNames)
        {
            if (directionClips[direction].Count == 0)
            {
                Debug.LogWarning($"No animation clips found or created for direction: {direction}");
                allDirectionsHaveClips = false;
            }
        }

        if (!allDirectionsHaveClips)
        {
            if (!EditorUtility.DisplayDialog("Missing Clips",
                "Some directions don't have animation clips. Continue anyway?", "Continue", "Cancel"))
            {
                return;
            }
        }

        // Create or modify the animator controller
        if (existingController == null)
        {
            CreateNewAnimatorController();
        }
        else
        {
            ModifyExistingAnimatorController();
        }
    }

    private void CreateNewAnimatorController()
    {
        // Ensure the save path exists
        if (!AssetDatabase.IsValidFolder(controllerSavePath))
        {
            // Try to create the folder path
            string[] pathParts = controllerSavePath.Split('/');
            string currentPath = pathParts[0];

            for (int i = 1; i < pathParts.Length; i++)
            {
                string nextPathPart = pathParts[i];
                string testPath = $"{currentPath}/{nextPathPart}";

                if (!AssetDatabase.IsValidFolder(testPath))
                {
                    AssetDatabase.CreateFolder(currentPath, nextPathPart);
                }

                currentPath = testPath;
            }
        }

        // Create the controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(
            $"{controllerSavePath}/{controllerName}.controller");

        // Add parameters
        controller.AddParameter(xParameterName, AnimatorControllerParameterType.Float);
        controller.AddParameter(yParameterName, AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

        // Setup layers
        AnimatorControllerLayer layer = controller.layers[0];
        layer.name = "Base Layer";

        // Create states
        AnimatorState idleState = layer.stateMachine.AddState("Idle");
        AnimatorState movementState = layer.stateMachine.AddState(stateName);

        // Create transitions between idle and movement
        AnimatorStateTransition idleToMovement = idleState.AddTransition(movementState);
        idleToMovement.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        idleToMovement.duration = 0.1f;

        AnimatorStateTransition movementToIdle = movementState.AddTransition(idleState);
        movementToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        movementToIdle.duration = 0.1f;

        // Create blend tree for movement
        BlendTree blendTree = CreateDirectionalBlendTree();

        // Assign blend tree to movement state
        movementState.motion = blendTree;

        // Set default state
        layer.stateMachine.defaultState = idleState;

        // Save the controller
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created new animator controller at {controllerSavePath}/{controllerName}.controller");
    }

    private void ModifyExistingAnimatorController()
    {
        // Ensure the parameters exist
        AddParameterIfMissing(existingController, xParameterName, AnimatorControllerParameterType.Float);
        AddParameterIfMissing(existingController, yParameterName, AnimatorControllerParameterType.Float);
        AddParameterIfMissing(existingController, "IsMoving", AnimatorControllerParameterType.Bool);

        // Find or create the movement state
        AnimatorControllerLayer baseLayer = existingController.layers[0];
        AnimatorState movementState = null;

        // Find state by name
        foreach (ChildAnimatorState state in baseLayer.stateMachine.states)
        {
            if (state.state.name == stateName)
            {
                movementState = state.state;
                break;
            }
        }

        // If state doesn't exist, create it
        if (movementState == null)
        {
            movementState = baseLayer.stateMachine.AddState(stateName);
            Debug.Log($"Created new state '{stateName}' in controller.");

            // Also create transitions if this is a new state
            if (baseLayer.stateMachine.defaultState != null)
            {
                AnimatorState idleState = baseLayer.stateMachine.defaultState;

                // Create transitions between idle and movement
                AnimatorStateTransition idleToMovement = idleState.AddTransition(movementState);
                idleToMovement.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
                idleToMovement.duration = 0.1f;

                AnimatorStateTransition movementToIdle = movementState.AddTransition(idleState);
                movementToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
                movementToIdle.duration = 0.1f;
            }
        }

        // Create blend tree
        BlendTree blendTree = CreateDirectionalBlendTree();

        // Assign blend tree to movement state
        movementState.motion = blendTree;

        // Save the controller
        EditorUtility.SetDirty(existingController);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Updated existing animator controller '{existingController.name}' with directional blend tree.");
    }

    private BlendTree CreateDirectionalBlendTree()
    {
        BlendTree blendTree = new BlendTree();
        blendTree.name = "DirectionalMovement";
        blendTree.blendType = BlendTreeType.SimpleDirectional2D;
        blendTree.blendParameter = xParameterName;
        blendTree.blendParameterY = yParameterName;

        // Prefer generated clips first, then any other clips found
        foreach (string direction in directionFolderNames)
        {
            AnimationClip clipToUse = null;

            // Try to use the clip we generated
            if (generatedClips.ContainsKey(direction) && generatedClips[direction] != null)
            {
                clipToUse = generatedClips[direction];
            }
            // Otherwise use any clip we found for this direction
            else if (directionClips[direction].Count > 0)
            {
                clipToUse = directionClips[direction][0];
            }

            if (clipToUse != null)
            {
                Vector2 position = directionPositions[direction];
                blendTree.AddChild(clipToUse, position);
                Debug.Log($"Added {direction} animation ({clipToUse.name}) at position {position}");
            }
        }

        // Save the blend tree as an asset to ensure it persists after Unity restarts
        string blendTreePath = $"{animationsSavePath}/BlendTrees";
        if (!AssetDatabase.IsValidFolder(blendTreePath))
        {
            string parentFolder = Path.GetDirectoryName(blendTreePath).Replace('\\', '/');
            string folderName = Path.GetFileName(blendTreePath);
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }

        string assetPath = $"{blendTreePath}/DirectionalMovement_{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}.asset";
        AssetDatabase.CreateAsset(blendTree, assetPath);
        AssetDatabase.SaveAssets();

        return blendTree;
    }

    private void AddParameterIfMissing(AnimatorController controller, string paramName, AnimatorControllerParameterType paramType)
    {
        // Check if parameter already exists
        foreach (AnimatorControllerParameter param in controller.parameters)
        {
            if (param.name == paramName && param.type == paramType)
            {
                return; // Parameter already exists
            }
        }

        // Add parameter if it doesn't exist
        controller.AddParameter(paramName, paramType);
        Debug.Log($"Added missing parameter '{paramName}' to controller.");
    }
}
#endif