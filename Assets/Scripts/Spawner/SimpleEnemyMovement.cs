using UnityEngine;

// Simple movement script for basic enemy behavior
public class SimpleEnemyMovement : MonoBehaviour
{
    private Transform targetTransform;
    private float moveSpeed = 1f;
    private Rigidbody2D rb;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    public void SetTarget(Transform target)
    {
        targetTransform = target;
    }
    
    public void SetSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    private void FixedUpdate()
    {
        if (targetTransform != null && rb != null)
        {
            // Calculate direction to target
            Vector2 direction = ((Vector2)targetTransform.position - rb.position).normalized;
            
            // Move towards target
            rb.linearVelocity = direction * moveSpeed;
        }
    }
} 