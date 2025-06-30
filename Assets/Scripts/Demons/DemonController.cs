using System.Collections;
using System;
using UnityEngine;

public class DemonController : MonoBehaviour
{
    // References
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;

    // Runtime data
    private DemonModel demonModel;
    private Transform playerTransform;
    private Vector2 moveDirection;
    private float currentHealth;
    private float lastAttackTime;
    private bool isDead = false;
    private bool isAttacking = false;
    private float distanceToPlayer;
    
    // Components
    private BoxCollider2D hitboxCollider;
    private AudioSource audioSource;
    
    // Attack
    [SerializeField] private GameObject attackHitbox;
    private float attackDuration = 0.3f;

    // Optional effects
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private AudioClip hitSound; // Sound to play when hit
    
    // Event for object pooling - called when demon dies
    public event Action<GameObject> OnDemonDeath;
    
    // Properties
    public bool IsDead => isDead;
    public DemonModel DemonData => demonModel;

    private void Awake()
    {
        // Get components if not assigned
        if (rb == null) rb = this.GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
         if (animator != null && animator.runtimeAnimatorController != null)
        {
            // Create a deep copy of the animator controller
            var runtimeController = animator.runtimeAnimatorController;
            var newController = Instantiate(runtimeController);
            animator.runtimeAnimatorController = newController;
            
            // Ensure unique animation state by forcing a rebind
            animator.Rebind();
        }
        // Create a hitbox collider for attacks if needed
        if (attackHitbox == null)
        {
            attackHitbox = new GameObject("AttackHitbox");
            attackHitbox.transform.SetParent(transform);
            attackHitbox.transform.localPosition = Vector3.zero;
            hitboxCollider = attackHitbox.AddComponent<BoxCollider2D>();
            hitboxCollider.isTrigger = true;
            hitboxCollider.size = new Vector2(1.2f, 1.2f);
            attackHitbox.AddComponent<DemonAttackHitbox>().SetOwner(this);
            attackHitbox.SetActive(false);
        }
    }

    public void Initialize(DemonModel model, Transform player)
    {
        demonModel = model;
        playerTransform = player;
        
        // Set up initial health and properties
        currentHealth = model.MaxHealth;
        
        // // Apply visual settings
        // if (spriteRenderer != null)
        // {
        //     // For now just apply a color based on the demon type
        //     int nameHash = model.DemonName.GetHashCode();
        //     float hue = (nameHash % 1000) / 1000f;
        //     spriteRenderer.color = Color.HSVToRGB(hue, 0.7f, 0.8f);
        // }
        
        // Configure attack hitbox size based on demon's attack range
        if (hitboxCollider != null)
        {
            hitboxCollider.size = new Vector2(model.AttackRange, model.AttackRange);
        }
        
        // Set physics properties
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearDamping = 1f;
        }
        
        // Set animator parameters if available
        if (animator != null)
        {
            animator.SetFloat("MovementSpeed", model.MovementSpeed);
        }
    }

    private void Update()
    {
        if (isDead || demonModel == null || playerTransform == null) return;
        
        // Check if player is in attack range
        distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        
        if (distanceToPlayer <= demonModel.AttackRange && !isAttacking)
        {
            // Only attack if cooldown has passed
            if (Time.time - lastAttackTime >= demonModel.AttackCooldown)
            {
                StartCoroutine(PerformAttack());
            }
        }
        
        // Update animator if available
        if (animator != null)
        {
            animator.SetBool("IsMoving", rb.linearVelocity.magnitude > 0.1f);
            animator.SetBool("IsAttacking", isAttacking);
            
            // Always set direction parameters to face the player
            animator.SetFloat("MoveX", moveDirection.x);
            animator.SetFloat("MoveY", moveDirection.y);
        }
    }

    private void FixedUpdate()
    {
        if (isDead || demonModel == null || playerTransform == null) return;
        
        // Calculate direction to player
        Vector2 direction = ((Vector2)playerTransform.position - this.rb.position).normalized;
        moveDirection = direction;
        
        // Only move if we're not currently attacking AND not in attack range
        if (!isAttacking || !isDead)
        {
            if (distanceToPlayer > demonModel.AttackRange)
            {
                // Move towards player
                rb.linearVelocity = direction * demonModel.MovementSpeed;
            }
            else
            {
                // Stop moving when in attack range
                rb.linearVelocity = Vector2.zero;
            }
        }
        else
        {
            // Stop moving while attacking
            rb.linearVelocity = Vector2.zero;
        }
    }
    
    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        
        // Stop movement during attack
        rb.linearVelocity = Vector2.zero;
        
        // Play attack animation if available
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        // Wait a small delay before activating hitbox
        yield return new WaitForSeconds(0.1f);
        
        // Activate attack hitbox
        if (attackHitbox != null)
        {
            attackHitbox.SetActive(true);
            
            // Position hitbox based on movement direction instead of sprite flipping
            Vector2 hitboxOffset = moveDirection * 0.5f;
            attackHitbox.transform.localPosition = hitboxOffset;
        }
        
        // Wait for attack duration
        yield return new WaitForSeconds(attackDuration);
        
        // Deactivate attack hitbox
        if (attackHitbox != null)
        {
            attackHitbox.SetActive(false);
        }
        
        // End attack state
        isAttacking = false;
    }
    
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        
        // Play hit animation/effect
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }

        // Play hit sound
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
        
        // Spawn hit effect
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
        
        // Flash the sprite
        StartCoroutine(FlashSprite());
        
        // Check if dead
        if (currentHealth <= 0)
        {
            StartCoroutine(Die());
        }
    }
    
    private IEnumerator Die()
    {
        isDead = true;
        
        // Stop movement
        rb.linearVelocity = Vector2.zero;
        
        // Play death animation if available
        if (animator != null)
        {
            animator.SetTrigger("Death");
            
            // Wait for death animation to complete
            float animationLength = 0;
            AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            
            if (clipInfo.Length > 0)
            {
                animationLength = clipInfo[0].clip.length;
                Debug.Log("Animation length: " + clipInfo.Length);
                yield return new WaitForSeconds(animationLength);
            }
            else
            {
                // If we can't determine the animation length, wait a default time
                yield return new WaitForSeconds(1.0f);
            }
        }
        
        // Spawn death effect
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }
        
        // Disable colliders
        foreach (Collider2D col in GetComponents<Collider2D>())
        {
            col.enabled = false;
        }
        
        // Add score and experience, or notify game manager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(demonModel.ScoreValue);
            GameManager.Instance.AddExperience(demonModel.ExperienceValue); // Grant experience points
        }
        
        // Notify that this demon has died (for object pooling)
        OnDemonDeath?.Invoke(gameObject);
        
        // Note: We no longer destroy the gameObject, the object pool handles recycling it
    }
    
    private IEnumerator FlashSprite()
    {
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }
    
    // Reset the demon state for reuse from the object pool
    public void ResetState()
    {
        // Reset all state variables
        isDead = false;
        isAttacking = false;
        
        // Reset health to full based on the demon model
        if (demonModel != null)
        {
            currentHealth = demonModel.MaxHealth;
        }
        
        // Re-enable colliders
        foreach (Collider2D col in GetComponents<Collider2D>())
        {
            col.enabled = true;
        }
        
        // Reset any animations if needed
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
        
        // Disable attack hitbox
        if (attackHitbox != null)
        {
            attackHitbox.SetActive(false);
        }
        
        // Reset movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
    
    // Helper method for debugging
    private void OnDrawGizmosSelected()
    {
        if (demonModel == null) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, demonModel.AttackRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, demonModel.DetectionRange);
        
        // Draw move direction
        Gizmos.color = Color.blue;
        Vector3 direction = moveDirection.normalized;
        Gizmos.DrawLine(transform.position, transform.position + direction * 2f);
        
        // Draw an arrowhead to show direction clearly
        Vector3 arrowEnd = transform.position + direction * 2f;
        Vector3 right = Quaternion.Euler(0, 0, 30) * -direction * 0.5f;
        Vector3 left = Quaternion.Euler(0, 0, -30) * -direction * 0.5f;
        Gizmos.DrawLine(arrowEnd, arrowEnd + right);
        Gizmos.DrawLine(arrowEnd, arrowEnd + left);
    }
}

// Separate class for the attack hitbox
