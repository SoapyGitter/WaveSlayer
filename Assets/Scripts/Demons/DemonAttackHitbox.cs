using UnityEngine;

public class DemonAttackHitbox : MonoBehaviour
{
    private DemonController owner;

    public void SetOwner(DemonController demonController)
    {
        owner = demonController;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (owner == null || owner.DemonData == null) return;

        //// Check if we hit the player
        //if (collision.CompareTag("Player"))
        //{
        //    // Get player health component
        //    PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
        //    if (playerHealth != null)
        //    {
        //        playerHealth.TakeDamage(owner.DemonData.Damage);
        //    }

        //    // Apply knockback
        //    Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();
        //    if (playerRb != null)
        //    {
        //        Vector2 knockbackDirection = (collision.transform.position - transform.position).normalized;
        //        playerRb.AddForce(knockbackDirection * 5f, ForceMode2D.Impulse);
        //    }
        //}
    }
}