using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private PlayerModel playerModel;
    
    [Header("UI References")]
    [SerializeField] private Image healthBar;
    [SerializeField] private GameObject damageFlashOverlay;
    
    [Header("Effects")]
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private GameObject deathEffect;
    
    // Damage control
    [SerializeField] private float invincibilityDuration = 0.5f;
    private bool isInvincible = false;
    
    // Components
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private PlayerBase playerBase;
    
    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        playerBase = GetComponent<PlayerBase>();
        
        // Initialize health
        if (playerModel != null)
        {
            playerModel.CurrentHealth = playerModel.MaxHealth;
        }
        
        UpdateHealthUI();
    }
    
    public void TakeDamage(int damage)
    {
        // Don't take damage if invincible
        if (isInvincible) return;
        
        if (playerModel != null)
        {
            // Apply damage
            playerModel.CurrentHealth -= (short)damage;
            
            // Clamp health to 0
            playerModel.CurrentHealth = (short)Mathf.Max(0, playerModel.CurrentHealth);
            
            // Update UI
            UpdateHealthUI();
            
            // Show hit effect
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
            
            // Show damage flash overlay
            if (damageFlashOverlay != null)
            {
                damageFlashOverlay.SetActive(true);
                StartCoroutine(DisableDamageFlash());
            }
            
            // Flash the player sprite
            if (spriteRenderer != null)
            {
                StartCoroutine(FlashSprite());
            }
            
            // Make player invincible briefly
            StartCoroutine(InvincibilityFrames());
            
            // Check if player is dead
            if (playerModel.CurrentHealth <= 0)
            {
                Die();
            }
        }
    }
    
    private void Die()
    {
        // Show death effect
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }
        
        // Disable player control
        if (playerBase != null)
        {
            playerBase.enabled = false;
        }
        
        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        
        // Notify game manager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerDied();
        }
        
        // Disable sprite
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        
        // Disable colliders
        foreach (Collider2D col in GetComponents<Collider2D>())
        {
            col.enabled = false;
        }
    }
    
    private void UpdateHealthUI()
    {
        if (healthBar != null && playerModel != null)
        {
            float healthPercent = (float)playerModel.CurrentHealth / playerModel.MaxHealth;
            healthBar.fillAmount = healthPercent;
        }
    }
    
    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }
    
    private IEnumerator FlashSprite()
    {
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        
        // Flash white a few times
        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private IEnumerator DisableDamageFlash()
    {
        yield return new WaitForSeconds(0.2f);
        if (damageFlashOverlay != null)
        {
            damageFlashOverlay.SetActive(false);
        }
    }
    
    // Public method to heal the player
    public void Heal(int amount)
    {
        if (playerModel != null)
        {
            playerModel.CurrentHealth += (short)amount;
            
            // Clamp to max health
            playerModel.CurrentHealth = (short)Mathf.Min(playerModel.MaxHealth, playerModel.CurrentHealth);
            
            // Update UI
            UpdateHealthUI();
        }
    }
} 