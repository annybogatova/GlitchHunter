using UnityEngine;

public class Enemy : MonoBehaviour
{
    public delegate void DeathHandler(GameObject enemy);
    public event DeathHandler OnDeath;
    
    [SerializeField] private int health;

    public void TakeDamage(int damage)
    {
        Debug.Log(gameObject.name + " has " + damage + " damage");
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        OnDeath?.Invoke(gameObject);
        Destroy(gameObject);
    }
}
