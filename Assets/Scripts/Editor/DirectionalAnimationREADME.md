# Directional Animation Blend Tree Builder

This tool allows you to easily create a 12-direction blend tree for character movement animations.

## Features

- Create a 12-directional blend tree animator controller
- Support for both editor-time and runtime creation
- Visual debugging of animation directions in the scene view
- Support for all animation directions: Left, Right, Top, Bottom, and all diagonals
- **NEW**: Auto-assignment of clips based on naming conventions

## Setup Instructions

### Basic Setup

1. Create an empty GameObject in your scene
2. Add the `DirectionalAnimationSetup` component to the GameObject
3. Assign your 12 directional animation clips to the corresponding fields
4. Click "Setup Clip Array from Individual References" in the context menu
5. Click "Create Animator From References" to create the animator controller

### Auto-Assignment Setup (NEW)

1. Create an empty GameObject in your scene
2. Add the `DirectionalAnimationSetup` component to the GameObject
3. Add all your directional animations to the "All Clips" array
4. Ensure your animation clips use naming conventions like:
   - `Walk_TR` or `Walk_TopRight` for top-right animations
   - `Attack_LB` or `Attack_LeftBottom` for left-bottom animations
   - See the "Naming Conventions" section below for all supported suffixes
5. Click "Auto-Assign Clips By Naming Convention" in the context menu
6. Click "Create Animator From References" to create the animator controller
7. Or simply click "Auto-Assign and Create Animator" to do both in one step

### Runtime Creation

1. Set the `createAtRuntime` flag to true in the `DirectionalAnimationSetup` component
2. Assign a target animator that will receive the blend tree
3. Either manually assign clips or use the auto-assignment feature
4. Optionally specify an idle animation name (it will try to load this from Resources)
5. Play the game, and the animator controller will be created and assigned at runtime

## Naming Conventions for Auto-Assignment

The auto-assignment feature supports the following naming conventions:

| Direction | Supported Suffixes |
|-----------|-------------------|
| Left-Bottom | `_LB`, `_LeftBottom` |
| Bottom-Left | `_BL`, `_BottomLeft` |
| Bottom | `_B`, `_Bottom` |
| Bottom-Right | `_BR`, `_BottomRight` |
| Right-Bottom | `_RB`, `_RightBottom` |
| Right | `_R`, `_Right` |
| Right-Top | `_RT`, `_RightTop` |
| Top-Right | `_TR`, `_TopRight` |
| Top | `_T`, `_Top` |
| Top-Left | `_TL`, `_TopLeft` |
| Left-Top | `_LT`, `_LeftTop` |
| Left | `_L`, `_Left` |

Examples:
- `Walk_TR` -> Top-Right animation
- `Attack_LB` -> Left-Bottom animation
- `Idle_T` -> Top animation
- `Run_Right` -> Right animation

## Animation Direction Order

The animations should be assigned in the following order:

1. `leftBottomClip` - Character moving to the left and slightly downward
2. `bottomLeftClip` - Character moving downward and slightly to the left
3. `bottomClip` - Character moving straight down
4. `bottomRightClip` - Character moving downward and slightly to the right
5. `rightBottomClip` - Character moving to the right and slightly downward
6. `rightClip` - Character moving straight to the right
7. `rightTopClip` - Character moving to the right and slightly upward
8. `topRightClip` - Character moving upward and slightly to the right
9. `topClip` - Character moving straight up
10. `topLeftClip` - Character moving upward and slightly to the left
11. `leftTopClip` - Character moving to the left and slightly upward
12. `leftClip` - Character moving straight to the left

## How to Use the Blend Tree in Your Game

1. Ensure your character has an Animator component
2. Assign the created animator controller to your character
3. In your movement script, set the following animator parameters:
   - `IsMoving` (bool) - Set to true when the character is moving
   - `MoveX` (float) - Set to the X direction of movement (-1 to 1)
   - `MoveY` (float) - Set to the Y direction of movement (-1 to 1)

Example script snippet:

```csharp
private Animator animator;
private Vector2 moveDirection;

void Start()
{
    animator = GetComponent<Animator>();
}

void Update()
{
    // Get input (example using Unity's Input System)
    float h = Input.GetAxisRaw("Horizontal");
    float v = Input.GetAxisRaw("Vertical");
    
    // Normalize for diagonal movement
    moveDirection = new Vector2(h, v);
    if (moveDirection.magnitude > 1)
        moveDirection.Normalize();
    
    // Update animator parameters
    bool isMoving = moveDirection.magnitude > 0.1f;
    animator.SetBool("IsMoving", isMoving);
    
    if (isMoving)
    {
        animator.SetFloat("MoveX", moveDirection.x);
        animator.SetFloat("MoveY", moveDirection.y);
    }
}
```

## Advanced Customization

You can modify the `BlendTreeBuilder` script if you need to:

- Change parameter names
- Adjust blend tree type
- Modify the position values for each direction
- Add additional states or transitions to the controller

## Visualization

The `DirectionalAnimationSetup` component includes a gizmo visualization that shows:

- A circle representing the blend positions
- Red dots indicating each direction position
- Labels for each direction

This helps you visualize how the 12 directions are mapped in 2D space.

## Technical Details

The blend tree uses a SimpleDirectional2D blend type with the following positions for each direction:

- Left-Bottom: (-0.5, -0.866)
- Bottom-Left: (-0.866, -0.5)
- Bottom: (0, -1)
- Bottom-Right: (0.866, -0.5)
- Right-Bottom: (0.5, -0.866)
- Right: (1, 0)
- Right-Top: (0.5, 0.866)
- Top-Right: (0.866, 0.5)
- Top: (0, 1)
- Top-Left: (-0.866, 0.5)
- Left-Top: (-0.5, 0.866)
- Left: (-1, 0)

These positions form a circle and ensure smooth blending between animations. 