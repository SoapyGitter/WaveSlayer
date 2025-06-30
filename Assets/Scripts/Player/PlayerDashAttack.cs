using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // For URP post-processing

[RequireComponent(typeof(AudioSource))] // Ensure AudioSource component is present
public class PlayerDashAttack : MonoBehaviour
{

    private float nextAutoDashTime = 0f; // When to perform the next automatic dash

    [Header("Effects")]
    [SerializeField] public GameObject dashEffectPrefab;
    [SerializeField] public GameObject hitEffectPrefab;
    [SerializeField] public TrailRenderer dashTrail;

    [Header("Audio")]
    [SerializeField] private AudioClip dashSound; // Sound effect for dashing
    private AudioSource audioSource; // AudioSource component

    [Header("Detection Settings")]
    [SerializeField] private LayerMask enemyLayerMask; // Layer mask for enemy detection
    [SerializeField] private LayerMask obstacleLayerMask; // Layer mask for obstacle detection
    [SerializeField] private float backupDetectionRadius = 10f; // Backup detection radius for physics overlap checks
    [SerializeField] private bool useAdditionalPhysicsCheck = true; // Whether to use additional physics checks to ensure enemies are detected

    [Header("Slow Motion")]
    [SerializeField] private bool useSlowMotionWhenIdle = true; // Enable slow motion when player is not moving
    [SerializeField] private float idleSlowMotionTimeScale = 0.9f; // Time scale when player is idle
    
    [Header("Slow Motion Visual Effects")]
    [SerializeField] private Volume postProcessingVolume; // Reference to post-processing volume
    [SerializeField] private float maxBlurAmount = 5f; // Maximum blur intensity
    [SerializeField] private float blurTransitionSpeed = 5f; // Speed of blur transition
    [SerializeField] private Color vignetteColor = new Color(0.3f, 0.5f, 1f); // Slight blue tint for vignette

    // Cached collection for triggers instead of Physics2D queries
    private HashSet<Transform> enemiesInRange = new HashSet<Transform>(); // Track enemies in detection radius
    private List<Transform> cachedEnemiesInPath = new List<Transform>(20); // Pre-allocate capacity
    private HashSet<Transform> cachedHitEnemies = new HashSet<Transform>();
    
    // Cached arrays for physics checks
    private Collider2D[] overlapResults = new Collider2D[20]; // Buffer for physics overlap results

    // Cached component to avoid allocations
    private CircleCollider2D detectionCollider;

    // Post-processing effect components
    private DepthOfField depthOfField;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;
    
    // Slow motion state
    private float originalTimeScale;
    private float slowMotionEndTime; // Time when slow motion should end
    private bool isIdleSlowMotionActive = false; // Track if idle slow motion is active
    private float targetBlurAmount = 0f; // Target blur amount
    private float currentBlurAmount = 0f; // Current blur amount
    private float targetVignetteIntensity = 0f; // Target vignette intensity
    private float targetChromaticIntensity = 0f; // Target chromatic aberration intensity

    [Header("Directional Dash")]
    [SerializeField] private float directionAngleThreshold = 30f; // Angle threshold for directional dashing
    [SerializeField] private float minMovementSpeed = 0.1f; // Minimum speed to consider player as moving
    [SerializeField] private float pathWidth = 1.5f; // Width of the dash path for detecting enemies
    [SerializeField] private bool showDebugGizmos = true; // Toggle to show/hide debug gizmos
    [SerializeField] private bool enableDirectionalDash = true; // New: Control directional dash independently

    // References
    private Rigidbody2D rb;
    private PlayerBase playerBase;
    private PlayerModel playerModel;
    private Animator animator; // Reference to the animator component

    // State
    private bool isDashing = false;
    private bool canDash = true;
    private float lastDashTime;
    private Vector2 dashDirection;
    private Transform targetEnemy;
    private bool isDirectionalDash = false; // Track if dash was initiated by movement direction
    private float enemyScanFrequency = 0.2f; // How often to scan for enemies using physics (seconds)
    private float lastEnemyScanTime = 0f; // Last time we did a physics scan for enemies

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerBase = GetComponent<PlayerBase>();
        animator = GetComponent<Animator>(); // Get animator reference
        detectionCollider = GetComponent<CircleCollider2D>();
        audioSource = GetComponent<AudioSource>(); // Get AudioSource component

        if (playerBase != null)
        {
            playerModel = playerBase.GetPlayerModel();
        }

        if (dashTrail != null)
        {
            dashTrail.enabled = false;
        }

        detectionCollider.radius = playerModel != null ? playerModel.dashRadius : 5f;
        
        // Initialize post-processing effects
        InitializePostProcessingEffects();
        
        // Set enemy layer mask if not set
        if (enemyLayerMask == 0)
        {
            enemyLayerMask = LayerMask.GetMask("Enemy");
        }
        if (obstacleLayerMask == 0) // Default obstacle layer if not set
        {
            obstacleLayerMask = LayerMask.GetMask("Default"); // Or "Obstacles" or whatever your layer is named
        }
    }

    private void OnDestroy()
    {
        // Always restore time scale when object is destroyed, regardless of idle state
        isIdleSlowMotionActive = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        
        // Reset any post-processing effects
        ResetPostProcessingEffects();
    }

    // Trigger event when an enemy enters the detection radius
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            // Skip destroyed enemies
            DemonController demonController = other.GetComponent<DemonController>();
            if (demonController == null || !demonController.IsDead)
            {
                enemiesInRange.Add(other.transform);
            }
        }
    }

    // Trigger event when an enemy exits the detection radius
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            enemiesInRange.Remove(other.transform);
        }
    }

    private void Update()
    {
        // Ensure the radius is set correctly each frame in case it changes
        if (playerModel != null && detectionCollider != null)
        {
            detectionCollider.radius = playerModel.dashRadius;
        }

        // Handle dash cooldown
        if (!canDash && Time.time > lastDashTime + playerModel.dashCooldown)
        {
            canDash = true;
        }

        // Handle idle slow motion
        if (useSlowMotionWhenIdle)
        {
            HandleIdleSlowMotion();
        }

        // Update post-processing effects for slow motion
        UpdateSlowMotionEffects();

        // Periodically check for enemies using physics to supplement trigger detection
        if (useAdditionalPhysicsCheck && Time.time > lastEnemyScanTime + enemyScanFrequency)
        {
            ScanForEnemiesUsingPhysics();
            lastEnemyScanTime = Time.time;
        }

        // Split the directional dash detection from automatic dash
        bool shouldAutoDash = playerModel.useAutomaticDash && canDash && !isDashing && Time.time >= nextAutoDashTime;
        bool isMoving = rb.linearVelocity.magnitude > minMovementSpeed;

        // Handle directional dash independent of automatic dash setting
        if (enableDirectionalDash && canDash && !isDashing && isMoving)
        {
            // Only look for directional targets if we're moving
            Transform directionalTarget = FindEnemyInMovementDirection();

            // If we found a target in our movement direction, dash at it
            if (directionalTarget != null)
            {
                // Check for obstacles before directional dash
                if (!IsObstacleBetween(transform.position, directionalTarget.position))
                {
                    StartDash(directionalTarget, true); // This is a directional dash
                    nextAutoDashTime = Time.time + playerModel.automaticDashInterval;
                    // We've handled the dash, so don't need to check automatic dash
                    shouldAutoDash = false;
                }
                else
                {
                    // Obstacle found, try again soon
                    nextAutoDashTime = Time.time + 0.2f; // Or some other logic
                    shouldAutoDash = false; // Don't attempt automatic dash immediately
                }
            }
        }

        // Automatic dash logic (now separate from directional dash)
        if (shouldAutoDash)
        {
            Transform targetEnemy = null;

            // For automatic dashing, if we didn't find a directional target, get the closest enemy
            targetEnemy = FindNearestEnemy();

            if (targetEnemy != null)
            {
                // Check for obstacles before automatic dash
                if (!IsObstacleBetween(transform.position, targetEnemy.position))
                {
                    StartDash(targetEnemy, false); // This is not a directional dash
                    nextAutoDashTime = Time.time + playerModel.automaticDashInterval;
                }
                else
                {
                    // Obstacle found, try again soon
                    nextAutoDashTime = Time.time + 0.2f;
                }
            }
        }

        // Safety check for slow motion - if we're stuck in slow motion, restore it
        if (Time.timeScale < 1f && Time.time > slowMotionEndTime && slowMotionEndTime > 0 && !isIdleSlowMotionActive)
        {
            RestoreTimeScaleImmediate();
        }
    }

    private void OnDashActionPerformed(InputAction.CallbackContext context)
    {
        if (canDash && !isDashing)
        {
            Transform nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                // Check for obstacles before manual dash
                if (!IsObstacleBetween(transform.position, nearestEnemy.position))
                {
                    // Manual dash is never direction-based
                    StartDash(nearestEnemy, false);
                }
            }
        }
    }

    private Transform FindEnemyInMovementDirection()
    {
        // Get player movement direction
        Vector2 movementDirection = rb.linearVelocity.normalized;

        float closestDistance = float.MaxValue;
        Transform closestDirectionalEnemy = null;

        // Use the tracked enemies from triggers instead of Physics2D.OverlapCircleNonAlloc
        foreach (Transform enemyTransform in enemiesInRange)
        {
            // Skip destroyed enemies
            DemonController demonController = enemyTransform.GetComponent<DemonController>();
            if (demonController != null && demonController.IsDead) continue;

            // Calculate direction to enemy
            Vector2 directionToEnemy = (enemyTransform.position - transform.position).normalized;

            // Calculate angle between movement direction and direction to enemy
            float angle = Vector2.Angle(movementDirection, directionToEnemy);

            // Calculate dot product to determine if player is moving toward or away from enemy
            float dotProduct = Vector2.Dot(movementDirection, directionToEnemy);

            // Check if enemy is within the angle threshold AND player is moving toward them (not away)
            if (angle <= directionAngleThreshold && dotProduct > 0)
            {
                float distance = Vector2.Distance(transform.position, enemyTransform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestDirectionalEnemy = enemyTransform;
                }
            }
        }

        return closestDirectionalEnemy;
    }

    private Transform FindNearestEnemy()
    {
        // If no enemies in range, do an immediate physics check
        if (enemiesInRange.Count == 0 && useAdditionalPhysicsCheck)
        {
            ScanForEnemiesUsingPhysics();
        }
        
        float closestDistance = float.MaxValue;
        Transform closestEnemy = null;

        foreach (Transform enemyTransform in enemiesInRange)
        {
            // Skip null or destroyed enemies
            if (enemyTransform == null) continue;
            
            // Skip dead enemies
            DemonController demonController = enemyTransform.GetComponent<DemonController>();
            if (demonController != null && demonController.IsDead) continue;

            float distance = Vector2.Distance(transform.position, enemyTransform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemyTransform;
            }
        }

        return closestEnemy;
    }

    private void StartDash(Transform target)
    {
        // Keep this overload for backwards compatibility with any existing code
        StartDash(target, false);
    }

    private void StartDash(Transform target, bool isDirectional)
    {
        // Null check to prevent errors
        if (target == null) return;

        // Check for obstacles before starting the dash
        if (IsObstacleBetween(transform.position, target.position))
        {
            // Optionally, provide feedback that dash was blocked
            Debug.Log("Dash blocked by obstacle.");
            return; // Do not dash if obstacle is present
        }
        
        // Start the dash
        targetEnemy = target;
        isDirectionalDash = isDirectional; // Set the flag to track dash type

        // Calculate dash direction
        Vector2 direction = (target.position - transform.position).normalized;

        // Trigger dash animation
        animator.SetBool("Dash", true);

        // Start the dash using async method
        PerformDashAsync().Forget();
    }
    private async UniTaskVoid PerformDashAsync()
    {
        isDashing = true;
        canDash = false;
        lastDashTime = Time.time;

        // Check if target still exists
        if (targetEnemy == null)
        {
            // Target was destroyed, find a new closest enemy
            targetEnemy = FindNearestEnemy();
            
            // If still null, cancel dash
            if (targetEnemy == null)
            {
                isDashing = false;
                canDash = true;
                animator.SetBool("Dash", false);
                return;
            }
        }

        // Calculate dash direction
        dashDirection = (targetEnemy.position - transform.position).normalized;

        // Store original velocity to restore after dash
        Vector2 originalVelocity = rb.linearVelocity;

        // Disable player movement control during dash
        if (playerBase != null)
        {
            playerBase.SetMovementEnabled(false);
        }

        // Show dash effect
        if (dashEffectPrefab != null)
        {
            Instantiate(dashEffectPrefab, transform.position, Quaternion.identity);
        }

        // Enable dash trail if available
        if (dashTrail != null)
        {
            dashTrail.enabled = true;
        }

        // Calculate dash distance to stop when reaching target
        float dashDistance = Vector2.Distance(transform.position, targetEnemy.position) + playerModel.dashOffset;
        dashDistance += dashDistance / 10; // Add a small buffer to ensure we reach/pass the target slightly
        
        // Calculate expected dash duration for animation and sound
        float expectedDashDuration = dashDistance / playerModel.dashSpeed;

        // Play dash sound and adjust pitch to match duration
        if (audioSource != null && dashSound != null)
        {
            if (expectedDashDuration > 0 && dashSound.length > 0)
            {
                audioSource.pitch = dashSound.length / expectedDashDuration;
            }
            else
            {
                audioSource.pitch = 1f; // Play at normal speed if duration is zero or sound length is zero
            }
            audioSource.PlayOneShot(dashSound);
        }

        // Apply dash force
        rb.linearVelocity = dashDirection * playerModel.dashSpeed;

        // For directional attacks, find all enemies in the dash path
        cachedEnemiesInPath.Clear(); // Clear the list instead of creating a new one
        if (isDirectionalDash)
        {
            FindEnemiesInDashPath(cachedEnemiesInPath); // Pass the list to be filled
        }

        // Track if we've hit the target yet
        bool hasHitTarget = false;
        // Track which enemies we've already hit to prevent hitting the same enemy multiple times
        cachedHitEnemies.Clear(); // Clear the set instead of creating a new one
        
        float distanceTraveled = 0f;
        Vector2 startPosition = transform.position;

        // Get the dash animation clip to determine its default duration
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        float defaultAnimationDuration = 0;
        if (clipInfo.Length > 0)
        {
            defaultAnimationDuration = clipInfo[0].clip.length;
        }
        else
        {
            // Fallback if we can't get the current animation clip
            // Try to find the dash animation clip by name
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name.Contains("Dash"))
                {
                    defaultAnimationDuration = clip.length;
                    break;
                }
            }
            
            // If still no clip found, use a reasonable default
            if (defaultAnimationDuration <= 0)
            {
                defaultAnimationDuration = 0.5f;
            }
        }
        
        // Calculate animation speed adjustment to match dash duration
        if (defaultAnimationDuration > 0)
        {
            float animSpeedMultiplier = defaultAnimationDuration / expectedDashDuration;
            animator.SetFloat("DashSpeed", animSpeedMultiplier);
        }
        
        // Trigger dash animation
        animator.SetBool("Dash", true);

        // Create a smaller trigger collider for hit detection during dash
        CircleCollider2D hitDetectionCollider = gameObject.AddComponent<CircleCollider2D>();
        hitDetectionCollider.isTrigger = true;
        hitDetectionCollider.radius = 0.5f;

        // Track collisions during dash
        HashSet<Transform> collidedEnemies = new HashSet<Transform>();

        // Dash until we reach the target
        while (distanceTraveled < dashDistance)
        {
            // Check if target still exists
            if (targetEnemy == null)
            {
                break; // Exit dash loop if target is gone
            }

            // Obstacle check during dash
            // Cast a short ray in the direction of the dash to detect obstacles
            float obstacleCheckDistance = 0.2f; // How far ahead to check for obstacles
            RaycastHit2D obstacleHit = Physics2D.Raycast(transform.position, dashDirection, obstacleCheckDistance, obstacleLayerMask);

            if (obstacleHit.collider != null)
            {
                // Ensure we are not hitting ourselves or the intended target if it's somehow on the obstacle layer
                if (obstacleHit.transform != transform && obstacleHit.transform != targetEnemy)
                {
                    Debug.Log("Dash interrupted by obstacle collision during dash.");
                    rb.linearVelocity = Vector2.zero; // Stop movement immediately
                    break; // Exit dash loop
                }
            }
            
            // Maintain dash velocity
            rb.linearVelocity = dashDirection * playerModel.dashSpeed;

            // Check for enemies to damage
            if (isDirectionalDash)
            {
                // For directional dashes, check for all enemies in our path that we pass through
                foreach (Transform enemy in cachedEnemiesInPath)
                {
                    // Skip null enemies
                    if (enemy == null) continue;
                    
                    // Skip enemies we've already hit
                    if (cachedHitEnemies.Contains(enemy)) continue;

                    // Check if we've passed or reached this enemy
                    Vector2 enemyDirection = (enemy.position - transform.position);
                    float enemyDistance = enemyDirection.magnitude;

                    // If we're close to the enemy or we've passed it along our dash path
                    if (enemyDistance < 1f || Vector2.Dot(dashDirection, enemyDirection.normalized) < 0)
                    {
                        DamageEnemy(enemy.gameObject);
                        cachedHitEnemies.Add(enemy);
                    }
                }
            }
            // For standard dashes, only check for the target enemy
            else if (!hasHitTarget && targetEnemy != null)
            {
                // Check for collision with target enemy
                float distanceToTarget = Vector2.Distance(transform.position, targetEnemy.position);
                if (distanceToTarget < 1f)
                {
                    DamageEnemy(targetEnemy.gameObject);
                    hasHitTarget = true;
                }
            }

            // Update distance traveled
            distanceTraveled = Vector2.Distance(startPosition, rb.position);

            // Wait for next frame
            await UniTask.DelayFrame(1);
        }

        // Clean up the temporary hit detection collider
        Destroy(hitDetectionCollider);

        // Slow down after dash
        rb.linearVelocity = originalVelocity * 0.5f;

        // Disable dash trail
        if (dashTrail != null)
        {
            dashTrail.enabled = false;
        }

        // Re-enable player movement
        if (playerBase != null)
        {
            playerBase.SetMovementEnabled(true);
        }

        // Reset animation speed and turn off dash animation
        animator.SetFloat("DashSpeed", 1f);
        animator.SetBool("Dash", false);

        // Reset audio pitch after dash
        if (audioSource != null)
        {
            audioSource.pitch = 1f;
        }

        // Ensure we hit the target enemy if we somehow missed it during the dash
        if (isDirectionalDash && targetEnemy != null && !cachedHitEnemies.Contains(targetEnemy))
        {
            DamageEnemy(targetEnemy.gameObject);
        }
        else if (!isDirectionalDash && targetEnemy != null && !hasHitTarget) // Also ensure target hit for non-directional
        {
            DamageEnemy(targetEnemy.gameObject);
        }

        // Dash complete
        isDashing = false;
    }

    private void DamageEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        
        DemonController demonController = enemy.GetComponent<DemonController>();
        if (demonController != null)
        {
            // Apply damage
            demonController.TakeDamage(playerModel.Damage);

            // Show hit effect
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, enemy.transform.position, Quaternion.identity);
            }

            // Apply knockback to enemy
            Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                enemyRb.AddForce(dashDirection * 5f, ForceMode2D.Impulse);
            }
        }
    }

    // Method to immediately restore time scale
    private void RestoreTimeScaleImmediate()
    {
        // Don't restore if idle slow motion is active
        if (isIdleSlowMotionActive) return;

        Time.timeScale = 1f; // Reset to default time scale (usually 1)
        Time.fixedDeltaTime = 0.02f; // Reset to default fixed time step
        slowMotionEndTime = 0; // Reset end time
    }

    // Method to manually trigger dash (useful for UI buttons)
    public void TriggerDash()
    {
        if (canDash && !isDashing)
        {
            Transform nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                // UI button triggered dash is never direction-based
                // Check for obstacles before UI triggered dash
                if (!IsObstacleBetween(transform.position, nearestEnemy.position))
                {
                    StartDash(nearestEnemy, false);
                }
            }
        }
    }

    // Method to toggle automatic dashing
    public void ToggleAutomaticDash(bool enabled)
    {
        if (playerModel != null)
        {
            playerModel.useAutomaticDash = enabled;
            if (enabled)
            {
                // Start automatic dashing immediately
                nextAutoDashTime = Time.time;
            }
        }
    }

    // Method to set the automatic dash interval
    public void SetAutomaticDashInterval(float interval)
    {
        if (playerModel != null)
        {
            playerModel.automaticDashInterval = Mathf.Max(playerModel.dashCooldown, interval);
        }
    }

    // Visual representation of the dash radius in the editor
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // Draw the dash radius
        if (playerModel != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, playerModel.dashRadius);
        }
        else
        {
            // Draw backup radius if playerModel not available in editor
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, backupDetectionRadius);
        }

        // Only draw direction gizmos if we have a rigidbody
        if (rb == null)
        {
            // Try to get the rigidbody in editor mode
            rb = GetComponent<Rigidbody2D>();
            if (rb == null) return;
        }

        // Draw player movement direction if moving
        Vector2 movementDir = rb.linearVelocity.normalized;
        if (rb.linearVelocity.magnitude > minMovementSpeed)
        {
            // Draw movement direction line
            Gizmos.color = Color.green;
            Vector3 endPoint = transform.position + new Vector3(movementDir.x, movementDir.y, 0) * 3f;
            Gizmos.DrawLine(transform.position, endPoint);

            // Draw direction detection cone
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Semi-transparent yellow
            DrawDirectionalCone(transform.position, movementDir, directionAngleThreshold, playerModel != null ? playerModel.dashRadius : backupDetectionRadius);
        }

        // Draw dash path if we have a target enemy and are in dash mode
        if (targetEnemy != null && isDashing)
        {
            DrawDashPath(transform.position, targetEnemy.position, pathWidth);
        }
        
        // Draw all detected enemies in range
        Gizmos.color = Color.red;
        foreach (Transform enemy in enemiesInRange)
        {
            if (enemy != null)
            {
                Gizmos.DrawLine(transform.position, enemy.position);
                Gizmos.DrawWireSphere(enemy.position, 0.3f);
            }
        }
    }

    // Helper method to draw the directional detection cone
    private void DrawDirectionalCone(Vector3 origin, Vector2 direction, float angleThreshold, float radius)
    {
        int segments = 20;
        float angleStep = (angleThreshold * 2) / segments;

        // Starting angle
        float startAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - angleThreshold;
        Vector3 previousPoint = origin;

        // Draw the cone outline
        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + angleStep * i;
            float radian = angle * Mathf.Deg2Rad;
            Vector3 point = origin + new Vector3(Mathf.Cos(radian), Mathf.Sin(radian), 0) * radius;

            if (i > 0)
            {
                Gizmos.DrawLine(origin, point);
                Gizmos.DrawLine(previousPoint, point);
            }

            previousPoint = point;
        }
    }

    // Helper method to draw the dash path with width
    private void DrawDashPath(Vector3 start, Vector3 end, float width)
    {
        Vector2 direction = (end - start).normalized;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x) * width * 0.5f;

        Vector3 p1 = start + new Vector3(perpendicular.x, perpendicular.y, 0);
        Vector3 p2 = start - new Vector3(perpendicular.x, perpendicular.y, 0);
        Vector3 p3 = end + new Vector3(perpendicular.x, perpendicular.y, 0);
        Vector3 p4 = end - new Vector3(perpendicular.x, perpendicular.y, 0);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Semi-transparent orange

        // Draw the sides of the path
        Gizmos.DrawLine(p1, p3);
        Gizmos.DrawLine(p2, p4);

        // Draw the ends
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p3, p4);
    }

    // New method to find all enemies in the dash path
    private void FindEnemiesInDashPath(List<Transform> enemiesInPath)
    {
        if (targetEnemy == null) return;
        
        // Define our dash path as a line from player to target enemy
        Vector2 dashStart = transform.position;
        Vector2 dashEnd = targetEnemy.position;
        Vector2 dashPathDirection = (dashEnd - dashStart).normalized;
        float dashPathLength = Vector2.Distance(dashStart, dashEnd);

        // Check each enemy in range to see if it's in our dash path
        foreach (Transform enemyTransform in enemiesInRange)
        {
            // Skip null enemies
            if (enemyTransform == null) continue;
            
            // Skip the target enemy (we'll handle it separately)
            if (enemyTransform == targetEnemy)
            {
                enemiesInPath.Add(targetEnemy);
                continue;
            }

            // Skip destroyed enemies
            DemonController demonController = enemyTransform.GetComponent<DemonController>();
            if (demonController != null && demonController.IsDead) continue;

            // Calculate the vector from dash start to enemy
            Vector2 enemyPos = enemyTransform.position;
            Vector2 dashToEnemy = enemyPos - dashStart;

            // Project enemy position onto our dash path
            float projection = Vector2.Dot(dashToEnemy, dashPathDirection);

            // If enemy is behind us or beyond the target, it's not in our path
            if (projection < 0 || projection > dashPathLength) continue;

            // Calculate the closest point on the dash path to the enemy
            Vector2 closestPointOnPath = dashStart + dashPathDirection * projection;

            // Calculate the distance from the enemy to the dash path
            float distanceToPath = Vector2.Distance(enemyPos, closestPointOnPath);

            // If enemy is close enough to the path (allowing for some margin), add it
            if (distanceToPath < pathWidth) // Use the configurable path width
            {
                enemiesInPath.Add(enemyTransform);
            }
        }

        // Sort enemies by distance along the dash path (from player to target)
        enemiesInPath.Sort((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            
            float distA = Vector2.Distance(transform.position, a.position);
            float distB = Vector2.Distance(transform.position, b.position);
            return distA.CompareTo(distB);
        });
    }

    // New method to handle slow motion when player is idle
    private void HandleIdleSlowMotion()
    {
        bool isMoving = rb.linearVelocity.magnitude > minMovementSpeed;

        // If player is not moving and slow motion is not active
        if (!isMoving && !isIdleSlowMotionActive && !isDashing)
        {
            // Activate idle slow motion
            ActivateIdleSlowMotion();
        }
        // If player starts moving and idle slow motion is active
        else if (isMoving && isIdleSlowMotionActive)
        {
            // Deactivate idle slow motion
            DeactivateIdleSlowMotion();
        }
    }

    // New method to handle the post-processing effects for slow motion
    private void UpdateSlowMotionEffects()
    {
        // Skip if no post-processing volume
        if (postProcessingVolume == null) return;
        
        // Set target values based on slow motion state
        targetBlurAmount = isIdleSlowMotionActive ? maxBlurAmount : 0f;
        targetVignetteIntensity = isIdleSlowMotionActive ? 0.3f : 0f;
        targetChromaticIntensity = isIdleSlowMotionActive ? 0.2f : 0f;
        
        // Smoothly interpolate current blur amount
        currentBlurAmount = Mathf.Lerp(currentBlurAmount, targetBlurAmount, blurTransitionSpeed * Time.unscaledDeltaTime);
        
        // Apply blur effect (depth of field)
        if (depthOfField != null)
        {
            depthOfField.focalLength.Override(currentBlurAmount);
        }
        
        // Apply vignette effect
        if (vignette != null)
        {
            float vignetteValue = Mathf.Lerp(vignette.intensity.value, targetVignetteIntensity, blurTransitionSpeed * Time.unscaledDeltaTime);
            vignette.intensity.Override(vignetteValue);
        }
        
        // Apply chromatic aberration
        if (chromaticAberration != null)
        {
            float chromaticValue = Mathf.Lerp(chromaticAberration.intensity.value, targetChromaticIntensity, blurTransitionSpeed * Time.unscaledDeltaTime);
            chromaticAberration.intensity.Override(chromaticValue);
        }
    }

    // Method to activate slow motion when idle
    private void ActivateIdleSlowMotion()
    {
        // Don't activate if already in slow motion
        if (Time.timeScale < 0.9f) return;
        
        // Store the original time scale
        originalTimeScale = Time.timeScale;
        
        // Apply idle slow motion effect
        Time.timeScale = idleSlowMotionTimeScale;
        Time.fixedDeltaTime = Time.timeScale * 0.02f;
        
        isIdleSlowMotionActive = true;
        
        // Don't set an end time for idle slow motion
        slowMotionEndTime = 0;
    }
    
    // Method to deactivate slow motion when player moves
    private void DeactivateIdleSlowMotion()
    {
        if (!isIdleSlowMotionActive) return;
        
        // Restore normal time scale
        Time.timeScale = originalTimeScale;
        Time.fixedDeltaTime = Time.timeScale * 0.02f;
        
        isIdleSlowMotionActive = false;
    }

    // Reset all post-processing effects to default values
    private void ResetPostProcessingEffects()
    {
        if (depthOfField != null)
        {
            depthOfField.focalLength.Override(0f);
        }
        
        if (vignette != null)
        {
            vignette.intensity.Override(0f);
        }
        
        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.Override(0f);
        }
    }

    // Initialize post-processing components
    private void InitializePostProcessingEffects()
    {
        if (postProcessingVolume != null)
        {
            // Try to get the depth of field effect
            if (!postProcessingVolume.profile.TryGet(out depthOfField))
            {
                // If it doesn't exist, add it
                depthOfField = postProcessingVolume.profile.Add<DepthOfField>(false);
                depthOfField.mode.Override(DepthOfFieldMode.Bokeh);
            }
            
            // Initially set depth of field to be inactive
            depthOfField.active = true;
            depthOfField.focusDistance.Override(10f);
            depthOfField.focalLength.Override(0f); // Start with no blur
            
            // Get or add vignette
            if (!postProcessingVolume.profile.TryGet(out vignette))
            {
                vignette = postProcessingVolume.profile.Add<Vignette>(false);
            }
            
            // Initialize vignette settings
            vignette.active = true;
            vignette.intensity.Override(0f); // Start with no vignette
            vignette.color.Override(vignetteColor);
            
            // Get or add chromatic aberration
            if (!postProcessingVolume.profile.TryGet(out chromaticAberration))
            {
                chromaticAberration = postProcessingVolume.profile.Add<ChromaticAberration>(false);
            }
            
            // Initialize chromatic aberration settings
            chromaticAberration.active = true;
            chromaticAberration.intensity.Override(0f); // Start with no effect
        }
        else
        {
            Debug.LogWarning("No Post-processing Volume assigned for slow motion effects!");
        }
    }

    // Additional method to ensure we're not missing enemies
    private void ScanForEnemiesUsingPhysics()
    {
        // Use a physics overlap circle to find enemies that might have been missed by trigger detection
        float scanRadius = playerModel != null ? playerModel.dashRadius : backupDetectionRadius;
        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, scanRadius, overlapResults, enemyLayerMask);
        
        // Add enemies from physics check to our tracked enemies set
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D enemyCollider = overlapResults[i];
            
            // Skip the enemy if it's already in our list or is destroyed/dead
            DemonController demonController = enemyCollider.GetComponent<DemonController>();
            if (demonController == null || demonController.IsDead)
                continue;
            
            // Add to our tracked enemies if not already there
            enemiesInRange.Add(enemyCollider.transform);
        }
        
        // Also clean up our enemy list by removing null or destroyed enemies
        CleanUpEnemyList();
    }
    
    // Helper method to clean up the enemy list
    private void CleanUpEnemyList()
    {
        List<Transform> enemiesToRemove = new List<Transform>();
        
        foreach (Transform enemyTransform in enemiesInRange)
        {
            // Check if enemy is null or destroyed
            if (enemyTransform == null)
            {
                enemiesToRemove.Add(enemyTransform);
                continue;
            }
            
            // Check if enemy is dead
            DemonController demonController = enemyTransform.GetComponent<DemonController>();
            if (demonController == null || demonController.IsDead)
            {
                enemiesToRemove.Add(enemyTransform);
            }
        }
        
        // Remove the invalid enemies
        foreach (Transform enemy in enemiesToRemove)
        {
            enemiesInRange.Remove(enemy);
        }
    }

    // New method to check for obstacles between two points
    private bool IsObstacleBetween(Vector2 start, Vector2 end)
    {
        // Calculate direction and distance
        Vector2 direction = (end - start).normalized;
        float distance = Vector2.Distance(start, end);

        // Perform a linecast to check for obstacles
        // Ensure to ignore the player and enemy colliders if they are on the obstacle layer
        // This can be done by temporarily disabling their colliders or using a layer mask that excludes them
        // For simplicity, this example assumes obstacles are on a distinct layer.
        RaycastHit2D hit = Physics2D.Linecast(start, end, obstacleLayerMask);

        // If the hit collider is not null, it means there's an obstacle
        if (hit.collider != null)
        {
            // Optional: Check if the hit object is indeed an obstacle and not the target itself or the player
            // This might be needed if enemies or the player can also be on the obstacleLayerMask
            if (hit.transform != targetEnemy && hit.transform != transform)
            {
                Debug.DrawLine(start, hit.point, Color.yellow, 1f); // Visualize blocked line
                return true;
            }
        }
        Debug.DrawLine(start, end, Color.cyan, 1f); // Visualize clear line
        return false;
    }
}