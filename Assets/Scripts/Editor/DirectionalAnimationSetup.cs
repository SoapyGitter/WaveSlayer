using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;

[ExecuteInEditMode]
public class DirectionalAnimationSetup : MonoBehaviour
{
    [Header("Animation Clips Array")]
    [Tooltip("Array of 12 animation clips for directional movement")]
    public AnimationClip[] directionalClips = new AnimationClip[12];

    [Header("Auto Assignment")]
    [Tooltip("All animation clips to auto-assign based on naming convention")]
    public AnimationClip[] allClips;

    [Header("Animation Clip References")]
    [Tooltip("Place individual animation clips here for convenience")]
    public AnimationClip leftBottomClip;
    public AnimationClip bottomLeftClip;
    public AnimationClip bottomClip;
    public AnimationClip bottomRightClip;
    public AnimationClip rightBottomClip;
    public AnimationClip rightClip;
    public AnimationClip rightTopClip;
    public AnimationClip topRightClip;
    public AnimationClip topClip;
    public AnimationClip topLeftClip;
    public AnimationClip leftTopClip;
    public AnimationClip leftClip;

    [Header("Target Animator")]
    public Animator targetAnimator;

    [Header("Existing Controller")]
    [Tooltip("Existing animator controller to modify instead of creating a new one")]
    public AnimatorController existingController;
    
    [Tooltip("Name of the state in the controller to add/replace the blend tree")]
    public string stateName = "Movement";

    //[Header("Runtime Creation")]
    //// public bool createAtRuntime = false;
    //public string idleClipName = "Idle";

    [ContextMenu("Auto-Assign Clips By Naming Convention")]
    public void AutoAssignClips()
    {
        // Find or add a BlendTreeBuilder component
        BlendTreeBuilder builder = GetComponent<BlendTreeBuilder>();
        if (builder == null)
        {
            builder = gameObject.AddComponent<BlendTreeBuilder>();
        }

        // Set the clips in the BlendTreeBuilder
        builder.allDirectionalClips = allClips;
        
        // Auto-assign based on naming convention
        builder.AutoAssignClipsBySuffix();
        
        // Copy assigned clips back to our component
        leftBottomClip = builder.leftBottomClip;
        bottomLeftClip = builder.bottomLeftClip;
        bottomClip = builder.bottomClip;
        bottomRightClip = builder.bottomRightClip;
        rightBottomClip = builder.rightBottomClip;
        rightClip = builder.rightClip;
        rightTopClip = builder.rightTopClip;
        topRightClip = builder.topRightClip;
        topClip = builder.topClip;
        topLeftClip = builder.topLeftClip;
        leftTopClip = builder.leftTopClip;
        leftClip = builder.leftClip;
        
        // Update the directional clips array
        SetupClipArray();
    }

    [ContextMenu("Setup Clip Array from Individual References")]
    public void SetupClipArray()
    {
        if (directionalClips.Length != 12)
        {
            directionalClips = new AnimationClip[12];
        }

        // Assign clips in the correct order
        directionalClips[0] = leftBottomClip;    // Left-Bottom
        directionalClips[1] = bottomLeftClip;    // Bottom-Left
        directionalClips[2] = bottomClip;        // Bottom
        directionalClips[3] = bottomRightClip;   // Bottom-Right
        directionalClips[4] = rightBottomClip;   // Right-Bottom
        directionalClips[5] = rightClip;         // Right
        directionalClips[6] = rightTopClip;      // Right-Top
        directionalClips[7] = topRightClip;      // Top-Right
        directionalClips[8] = topClip;           // Top
        directionalClips[9] = topLeftClip;       // Top-Left
        directionalClips[10] = leftTopClip;      // Left-Top
        directionalClips[11] = leftClip;         // Left
        
        Debug.Log("Clip array has been set up from individual references.");
    }

//     [ContextMenu("Create Animator From References")]
//     public void CopyToBlendTreeBuilder()
//     {
//         // Find or add a BlendTreeBuilder component
//         BlendTreeBuilder builder = GetComponent<BlendTreeBuilder>();
//         if (builder == null)
//         {
//             builder = gameObject.AddComponent<BlendTreeBuilder>();
//         }

//         // Copy individual clips to the builder
//         builder.leftBottomClip = leftBottomClip;
//         builder.bottomLeftClip = bottomLeftClip;
//         builder.bottomClip = bottomClip;
//         builder.bottomRightClip = bottomRightClip;
//         builder.rightBottomClip = rightBottomClip;
//         builder.rightClip = rightClip;
//         builder.rightTopClip = rightTopClip;
//         builder.topRightClip = topRightClip;
//         builder.topClip = topClip;
//         builder.topLeftClip = topLeftClip;
//         builder.leftTopClip = leftTopClip;
//         builder.leftClip = leftClip;

//         // Create the blend tree
// #if UNITY_EDITOR
//         builder.CreateBlendTree();
// #endif
//     }

    [ContextMenu("Modify Existing Animator")]
    public void ModifyExistingAnimator()
    {
#if UNITY_EDITOR
        if (existingController == null)
        {
            Debug.LogError("No existing animator controller assigned!");
            return;
        }

        // Find or add a BlendTreeBuilder component
        BlendTreeBuilder builder = GetComponent<BlendTreeBuilder>();
        if (builder == null)
        {
            builder = gameObject.AddComponent<BlendTreeBuilder>();
        }

        // Copy individual clips to the builder
        builder.leftBottomClip = leftBottomClip;
        builder.bottomLeftClip = bottomLeftClip;
        builder.bottomClip = bottomClip;
        builder.bottomRightClip = bottomRightClip;
        builder.rightBottomClip = rightBottomClip;
        builder.rightClip = rightClip;
        builder.rightTopClip = rightTopClip;
        builder.topRightClip = topRightClip;
        builder.topClip = topClip;
        builder.topLeftClip = topLeftClip;
        builder.leftTopClip = leftTopClip;
        builder.leftClip = leftClip;

        // Set the existing controller info
        builder.existingController = existingController;
        builder.stateName = stateName;

        // Modify the existing controller
        builder.ModifyExistingAnimator();
#endif
    }

    // [ContextMenu("Auto-Assign and Create Animator")]
    // public void AutoAssignAndCreateAnimator()
    // {
    //     AutoAssignClips();
    //     CopyToBlendTreeBuilder();
    // }

    [ContextMenu("Auto-Assign and Modify Existing Animator")]
    public void AutoAssignAndModifyAnimator()
    {
        AutoAssignClips();
        ModifyExistingAnimator();
    }

    // private void Start()
    // {
    //     if (createAtRuntime && targetAnimator != null)
    //     {
    //         if (allClips != null && allClips.Length > 0)
    //         {
    //             // Try to auto-assign first
    //             AutoAssignClips();
    //         }
            
    //         if (!AllClipsAssigned())
    //         {
    //             Debug.LogError("Not all clips are assigned. Cannot create runtime animator.");
    //             return;
    //         }

    //         // Make sure the array is set up
    //         if (directionalClips.Length != 12)
    //         {
    //             SetupClipArray();
    //         }

    //         // Get or add a BlendTreeBuilder
    //         BlendTreeBuilder builder = GetComponent<BlendTreeBuilder>();
    //         if (builder == null)
    //         {
    //             builder = gameObject.AddComponent<BlendTreeBuilder>();
    //         }

    //         // Create the controller at runtime
    //         RuntimeAnimatorController controller = builder.CreateRuntimeBlendTree(directionalClips, idleClipName);
            
    //         // Assign to target animator
    //         if (controller != null)
    //         {
    //             targetAnimator.runtimeAnimatorController = controller;
    //             Debug.Log("Runtime animator controller created and assigned successfully.");
    //         }
    //     }
    // }

    private bool AllClipsAssigned()
    {
        if (directionalClips.Length != 12)
            return false;

        foreach (AnimationClip clip in directionalClips)
        {
            if (clip == null)
                return false;
        }

        return true;
    }

    // Helper method to visualize the blend tree positions
    private void OnDrawGizmosSelected()
    {
        // Draw a circle to represent the blend positions
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 1f);

        // Draw positions for each direction
        Vector3 center = transform.position;
        float radius = 1f;

        // Positions for each direction
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

        string[] labels = new string[]
        {
            "Left-Bottom",
            "Bottom-Left",
            "Bottom",
            "Bottom-Right",
            "Right-Bottom",
            "Right",
            "Right-Top",
            "Top-Right",
            "Top",
            "Top-Left",
            "Left-Top",
            "Left"
        };

        // Draw a point and label for each position
        for (int i = 0; i < positions.Length; i++)
        {
            Vector3 pos = center + new Vector3(positions[i].x * radius, positions[i].y * radius, 0);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(pos, 0.05f);
            
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(pos, labels[i]);
#endif
        }
    }
} 