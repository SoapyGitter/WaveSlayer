using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(DirectionalAnimationSetup))]
public class DirectionalAnimationSetupEditor : Editor
{
    private bool showCreateButtons = false;
    private bool showModifyButtons = true;

    public override void OnInspectorGUI()
    {
        DirectionalAnimationSetup setup = (DirectionalAnimationSetup)target;

        // Draw the default inspector
        DrawDefaultInspector();

        // Add space before buttons
        EditorGUILayout.Space(10);
        
        // Create New Controller section
        //showCreateButtons = EditorGUILayout.Foldout(showCreateButtons, "Create New Controller", true, EditorStyles.foldoutHeader);
        
        //if (showCreateButtons)
        //{
        //    // Section header
        //    EditorGUILayout.LabelField("Create New Controller", EditorStyles.boldLabel);
            
        //    // Add buttons in a horizontal layout
        //    EditorGUILayout.BeginHorizontal();
            
        //    // Auto-assign button
        //    if (GUILayout.Button("Auto-Assign Clips", GUILayout.Height(30)))
        //    {
        //        setup.AutoAssignClips();
        //    }
            
        //    // Create blend tree button
        //    if (GUILayout.Button("Create New Blend Tree", GUILayout.Height(30)))
        //    {
        //        setup.CopyToBlendTreeBuilder();
        //    }
            
        //    EditorGUILayout.EndHorizontal();
            
        //    // Add a single button that does both operations
        //    if (GUILayout.Button("Auto-Assign AND Create New Blend Tree", GUILayout.Height(40)))
        //    {
        //        setup.AutoAssignAndCreateAnimator();
        //    }
        //}
        
        // Add space before Modify section
        EditorGUILayout.Space(10);
        
        // Modify Existing Controller section
        showModifyButtons = EditorGUILayout.Foldout(showModifyButtons, "Modify Existing Controller", true, EditorStyles.foldoutHeader);
        
        if (showModifyButtons)
        {
            EditorGUILayout.LabelField("Modify Existing Controller", EditorStyles.boldLabel);
            
            if (setup.existingController == null)
            {
                EditorGUILayout.HelpBox("Assign an existing animator controller in the 'Existing Controller' field above.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                
                // Button to just auto-assign clips
                if (GUILayout.Button("Auto-Assign Clips", GUILayout.Height(30)))
                {
                    setup.AutoAssignClips();
                }
                
                // Button to modify the existing controller
                if (GUILayout.Button("Modify Existing Controller", GUILayout.Height(30)))
                {
                    setup.ModifyExistingAnimator();
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Button to do both operations
                if (GUILayout.Button("Auto-Assign AND Modify Existing Controller", GUILayout.Height(40)))
                {
                    setup.AutoAssignAndModifyAnimator();
                }
                
                // Info box about what's happening
                EditorGUILayout.HelpBox(
                    $"This will modify the '{setup.existingController.name}' controller by adding or updating the '{setup.stateName}' state with a blend tree using your clips.",
                    MessageType.Info);
            }
        }
        
        // Add space and a section for utilities
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
        
        // Add button to update the clip array
        if (GUILayout.Button("Update Clip Array from References"))
        {
            setup.SetupClipArray();
        }
        
        // Show warning/info messages based on setup state
        EditorGUILayout.Space(5);
        
        if (setup.allClips != null && setup.allClips.Length > 0)
        {
            EditorGUILayout.HelpBox(
                "Animation clips will be auto-assigned based on naming conventions:\n" +
                "_TR, _TopRight, _T, _Top, etc.", 
                MessageType.Info);
        }
        else if (AreIndividualClipsAssigned(setup) == false)
        {
            EditorGUILayout.HelpBox(
                "Please either assign individual clips or use the auto-assignment feature by adding clips to the 'All Clips' array.", 
                MessageType.Warning);
        }
    }
    
    private bool AreIndividualClipsAssigned(DirectionalAnimationSetup setup)
    {
        return setup.leftBottomClip != null && 
               setup.bottomLeftClip != null && 
               setup.bottomClip != null && 
               setup.bottomRightClip != null && 
               setup.rightBottomClip != null && 
               setup.rightClip != null && 
               setup.rightTopClip != null && 
               setup.topRightClip != null && 
               setup.topClip != null && 
               setup.topLeftClip != null && 
               setup.leftTopClip != null && 
               setup.leftClip != null;
    }
}
#endif 