using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BlendTreeBuilder : MonoBehaviour
{
    [Header("Animation Clips")]
    [Tooltip("Left-Bottom Direction Animation")]
    public AnimationClip leftBottomClip;

    [Tooltip("Bottom-Left Direction Animation")]
    public AnimationClip bottomLeftClip;

    [Tooltip("Bottom Direction Animation")]
    public AnimationClip bottomClip;

    [Tooltip("Bottom-Right Direction Animation")]
    public AnimationClip bottomRightClip;

    [Tooltip("Right-Bottom Direction Animation")]
    public AnimationClip rightBottomClip;

    [Tooltip("Right Direction Animation")]
    public AnimationClip rightClip;

    [Tooltip("Right-Top Direction Animation")]
    public AnimationClip rightTopClip;

    [Tooltip("Top-Right Direction Animation")]
    public AnimationClip topRightClip;

    [Tooltip("Top Direction Animation")]
    public AnimationClip topClip;

    [Tooltip("Top-Left Direction Animation")]
    public AnimationClip topLeftClip;

    [Tooltip("Left-Top Direction Animation")]
    public AnimationClip leftTopClip;

    [Tooltip("Left Direction Animation")]
    public AnimationClip leftClip;

    [Header("Auto Assignment")]
    [Tooltip("All animation clips to automatically assign based on suffix (_TR, _T, _TL, etc.)")]
    public AnimationClip[] allDirectionalClips;

    [Header("Output Settings")]
    [Tooltip("Name for the created controller asset")]
    public string controllerName = "DirectionalMovementController";

    [Tooltip("Folder path where the controller will be saved (Assets/...)")]
    public string savePath = "Assets/Animations/Controllers";

    [Header("Existing Controller")]
    [Tooltip("Existing animator controller to modify instead of creating a new one")]
    public AnimatorController existingController;

    [Tooltip("Name of the state in the controller to add/replace the blend tree")]
    public string stateName = "Movement";

    [Header("Blend Tree Parameters")]
    [Tooltip("Parameter name for X axis movement")]
    public string xParameterName = "MoveX";

    [Tooltip("Parameter name for Y axis movement")]
    public string yParameterName = "MoveY";

    [ContextMenu("Auto-Assign Clips By Suffix")]
    public void AutoAssignClipsBySuffix()
    {
        if (allDirectionalClips == null || allDirectionalClips.Length == 0)
        {
            Debug.LogError("No clips assigned to allDirectionalClips array!");
            return;
        }

        // Clear existing clip assignments
        ClearClipAssignments();

        // Process each clip and assign based on suffix
        foreach (AnimationClip clip in allDirectionalClips)
        {
            if (clip == null) continue;

            string clipName = clip.name;

            // Check for each direction suffix
            if (clipName.EndsWith("_LB") || clipName.EndsWith("_LeftBottom"))
                leftBottomClip = clip;
            else if (clipName.EndsWith("_BL") || clipName.EndsWith("_BottomLeft"))
                bottomLeftClip = clip;
            else if (clipName.EndsWith("_B") || clipName.EndsWith("_Bottom"))
                bottomClip = clip;
            else if (clipName.EndsWith("_BR") || clipName.EndsWith("_BottomRight"))
                bottomRightClip = clip;
            else if (clipName.EndsWith("_RB") || clipName.EndsWith("_RightBottom"))
                rightBottomClip = clip;
            else if (clipName.EndsWith("_R") || clipName.EndsWith("_Right"))
                rightClip = clip;
            else if (clipName.EndsWith("_RT") || clipName.EndsWith("_RightTop"))
                rightTopClip = clip;
            else if (clipName.EndsWith("_TR") || clipName.EndsWith("_TopRight"))
                topRightClip = clip;
            else if (clipName.EndsWith("_T") || clipName.EndsWith("_Top"))
                topClip = clip;
            else if (clipName.EndsWith("_TL") || clipName.EndsWith("_TopLeft"))
                topLeftClip = clip;
            else if (clipName.EndsWith("_LT") || clipName.EndsWith("_LeftTop"))
                leftTopClip = clip;
            else if (clipName.EndsWith("_L") || clipName.EndsWith("_Left"))
                leftClip = clip;
        }

        // Report any unassigned clips
        ReportUnassignedClips();

        Debug.Log("Clips auto-assigned based on suffixes.");
    }

    private void ClearClipAssignments()
    {
        leftBottomClip = null;
        bottomLeftClip = null;
        bottomClip = null;
        bottomRightClip = null;
        rightBottomClip = null;
        rightClip = null;
        rightTopClip = null;
        topRightClip = null;
        topClip = null;
        topLeftClip = null;
        leftTopClip = null;
        leftClip = null;
    }

    private void ReportUnassignedClips()
    {
        List<string> unassigned = new List<string>();
        
        if (leftBottomClip == null) unassigned.Add("LeftBottom");
        if (bottomLeftClip == null) unassigned.Add("BottomLeft");
        if (bottomClip == null) unassigned.Add("Bottom");
        if (bottomRightClip == null) unassigned.Add("BottomRight");
        if (rightBottomClip == null) unassigned.Add("RightBottom");
        if (rightClip == null) unassigned.Add("Right");
        if (rightTopClip == null) unassigned.Add("RightTop");
        if (topRightClip == null) unassigned.Add("TopRight");
        if (topClip == null) unassigned.Add("Top");
        if (topLeftClip == null) unassigned.Add("TopLeft");
        if (leftTopClip == null) unassigned.Add("LeftTop");
        if (leftClip == null) unassigned.Add("Left");
        
        if (unassigned.Count > 0)
        {
            Debug.LogWarning("The following clips could not be auto-assigned: " + string.Join(", ", unassigned) +
                            "\nPlease ensure your clips have the correct suffixes (_TR, _BL, etc.)");
        }
    }

    [ContextMenu("Create Blend Tree")]
    public void CreateBlendTree()
    {
#if UNITY_EDITOR
        // Validate all clips are assigned
        if (!ValidateClips())
        {
            Debug.LogError("All animation clips must be assigned!");
            return;
        }

        // Create controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath($"{savePath}/{controllerName}.controller");

        // Add parameters
        controller.AddParameter(xParameterName, AnimatorControllerParameterType.Float);
        controller.AddParameter(yParameterName, AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

        // Setup layers
        AnimatorControllerLayer layer = controller.layers[0];
        layer.name = "Base Layer";

        // Create states
        AnimatorState idleState = layer.stateMachine.AddState("Idle");
        AnimatorState movementState = layer.stateMachine.AddState("Movement");

        // Create transitions between idle and movement
        AnimatorStateTransition idleToMovement = idleState.AddTransition(movementState);
        idleToMovement.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        idleToMovement.duration = 0.1f;

        AnimatorStateTransition movementToIdle = movementState.AddTransition(idleState);
        movementToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        movementToIdle.duration = 0.1f;

        // Create blend tree for movement
        BlendTree blendTree = new BlendTree();
        blendTree.name = "DirectionalMovement";
        blendTree.blendType = BlendTreeType.FreeformDirectional2D;
        blendTree.blendParameter = xParameterName;
        blendTree.blendParameterY = yParameterName;

        // Add motion fields to blend tree
        AddDirectionalMotionField(blendTree, leftBottomClip, -0.5f, -0.866f);    // Left-Bottom
        AddDirectionalMotionField(blendTree, bottomLeftClip, -0.866f, -0.5f);    // Bottom-Left
        AddDirectionalMotionField(blendTree, bottomClip, 0, -1);                 // Bottom
        AddDirectionalMotionField(blendTree, bottomRightClip, 0.866f, -0.5f);    // Bottom-Right
        AddDirectionalMotionField(blendTree, rightBottomClip, 0.5f, -0.866f);    // Right-Bottom
        AddDirectionalMotionField(blendTree, rightClip, 1, 0);                   // Right
        AddDirectionalMotionField(blendTree, rightTopClip, 0.5f, 0.866f);        // Right-Top
        AddDirectionalMotionField(blendTree, topRightClip, 0.866f, 0.5f);        // Top-Right
        AddDirectionalMotionField(blendTree, topClip, 0, 1);                     // Top
        AddDirectionalMotionField(blendTree, topLeftClip, -0.866f, 0.5f);        // Top-Left
        AddDirectionalMotionField(blendTree, leftTopClip, -0.5f, 0.866f);        // Left-Top
        AddDirectionalMotionField(blendTree, leftClip, -1, 0);                   // Left

        // Assign blend tree to movement state
        movementState.motion = blendTree;

        // Set default state
        layer.stateMachine.defaultState = idleState;

        // Save the controller
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log($"Blend Tree created successfully at {savePath}/{controllerName}.controller");
#else
        Debug.LogWarning("This function only works in the Unity Editor.");
#endif
    }

    [ContextMenu("Modify Existing Animator")]
    public void ModifyExistingAnimator()
    {
#if UNITY_EDITOR
        // Validate all clips are assigned
        if (!ValidateClips())
        {
            Debug.LogError("All animation clips must be assigned!");
            return;
        }

        // Check if existing controller is assigned
        if (existingController == null)
        {
            Debug.LogError("No existing animator controller assigned!");
            return;
        }

        // Ensure the parameters exist
        AddParameterIfMissing(existingController, xParameterName, AnimatorControllerParameterType.Float);
        AddParameterIfMissing(existingController, yParameterName, AnimatorControllerParameterType.Float);
        AddParameterIfMissing(existingController, "IsMoving", AnimatorControllerParameterType.Bool);

        // Find or create the movement state
        AnimatorControllerLayer baseLayer = existingController.layers[0];
        AnimatorState movementState = null;

        // Find state by name or create it
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
        }

        // Create blend tree
        BlendTree blendTree = new BlendTree();
        blendTree.name = "DirectionalMovement";
        blendTree.blendType = BlendTreeType.FreeformDirectional2D;
        blendTree.blendParameter = xParameterName;
        blendTree.blendParameterY = yParameterName;
        
        // Add motion fields to blend tree
        AddDirectionalMotionField(blendTree, leftBottomClip, -0.5f, -0.866f);    // Left-Bottom
        AddDirectionalMotionField(blendTree, bottomLeftClip, -0.866f, -0.5f);    // Bottom-Left
        AddDirectionalMotionField(blendTree, bottomClip, 0, -1);                 // Bottom
        AddDirectionalMotionField(blendTree, bottomRightClip, 0.866f, -0.5f);    // Bottom-Right
        AddDirectionalMotionField(blendTree, rightBottomClip, 0.5f, -0.866f);    // Right-Bottom
        AddDirectionalMotionField(blendTree, rightClip, 1, 0);                   // Right
        AddDirectionalMotionField(blendTree, rightTopClip, 0.5f, 0.866f);        // Right-Top
        AddDirectionalMotionField(blendTree, topRightClip, 0.866f, 0.5f);        // Top-Right
        AddDirectionalMotionField(blendTree, topClip, 0, 1);                     // Top
        AddDirectionalMotionField(blendTree, topLeftClip, -0.866f, 0.5f);        // Top-Left
        AddDirectionalMotionField(blendTree, leftTopClip, -0.5f, 0.866f);        // Left-Top
        AddDirectionalMotionField(blendTree, leftClip, -1, 0);                   // Left
        
        // Assign blend tree to movement state
        movementState.motion = blendTree;
        
        // Save the controller
        EditorUtility.SetDirty(existingController);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Blend Tree added/updated in state '{stateName}' of existing controller.");
#else
        Debug.LogWarning("This function only works in the Unity Editor.");
#endif
    }

#if UNITY_EDITOR
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

    private void AddDirectionalMotionField(BlendTree blendTree, AnimationClip clip, float posX, float posY)
    {
        blendTree.AddChild(clip, new Vector2(posX, posY));
    }

    private bool ValidateClips()
    {
        return leftBottomClip != null &&
               bottomLeftClip != null &&
               bottomClip != null &&
               bottomRightClip != null &&
               rightBottomClip != null &&
               rightClip != null &&
               rightTopClip != null &&
               topRightClip != null &&
               topClip != null &&
               topLeftClip != null &&
               leftTopClip != null &&
               leftClip != null;
    }
#endif

    // Runtime creation method (can be used in a build)
    public RuntimeAnimatorController CreateRuntimeBlendTree(
        AnimationClip[] directionalClips,
        string idleClipName = "Idle")
    {
        if (directionalClips.Length != 12)
        {
            Debug.LogError("Exactly 12 directional clips are required!");
            return null;
        }

        // Create controller
        AnimatorController controller = new AnimatorController();

        // Add parameters
        controller.AddParameter(xParameterName, AnimatorControllerParameterType.Float);
        controller.AddParameter(yParameterName, AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

        // Setup layers
        AnimatorControllerLayer layer = new AnimatorControllerLayer();
        layer.name = "Base Layer";
        layer.stateMachine = new AnimatorStateMachine();

        // Replace default layers
        controller.layers = new AnimatorControllerLayer[] { layer };

        // Create states
        AnimatorState idleState = layer.stateMachine.AddState("Idle");
        AnimatorState movementState = layer.stateMachine.AddState("Movement");

        // Create transitions between idle and movement
        AnimatorStateTransition idleToMovement = idleState.AddTransition(movementState);
        idleToMovement.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        idleToMovement.duration = 0.1f;

        AnimatorStateTransition movementToIdle = movementState.AddTransition(idleState);
        movementToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        movementToIdle.duration = 0.1f;

        // Create blend tree for movement
        BlendTree blendTree = new BlendTree();
        blendTree.name = "DirectionalMovement";
        blendTree.blendType = BlendTreeType.FreeformDirectional2D;
        blendTree.blendParameter = xParameterName;
        blendTree.blendParameterY = yParameterName;

        // Position values for all 12 directions on a circle
        Vector2[] positions = new Vector2[]
        {
            new Vector2(-0.5f, -0.866f),  // Left-Bottom
            new Vector2(-0.866f, -0.5f),  // Bottom-Left
            new Vector2(0, -1),           // Bottom
            new Vector2(0.866f, -0.5f),   // Bottom-Right
            new Vector2(0.5f, -0.866f),   // Right-Bottom
            new Vector2(1, 0),            // Right
            new Vector2(0.5f, 0.866f),    // Right-Top
            new Vector2(0.866f, 0.5f),    // Top-Right
            new Vector2(0, 1),            // Top
            new Vector2(-0.866f, 0.5f),   // Top-Left
            new Vector2(-0.5f, 0.866f),   // Left-Top
            new Vector2(-1, 0)            // Left
        };

        // Add all motion fields
        for (int i = 0; i < 12; i++)
        {
            blendTree.AddChild(directionalClips[i], positions[i]);
        }

        // Assign blend tree to movement state
        movementState.motion = blendTree;

        // Find an idle animation if one exists
        AnimationClip idleClip = Resources.Load<AnimationClip>(idleClipName);
        if (idleClip != null)
        {
            idleState.motion = idleClip;
        }

        // Set default state
        layer.stateMachine.defaultState = idleState;

        return controller;
    }
}