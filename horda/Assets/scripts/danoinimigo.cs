using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [Header("Configurações de Dano")]
    public int damageAmount = 10;
    public float attackCooldown = 1f;

    [Header("Referência ao ScriptableObject")]
    public status_inimigo enemyStatus; // ⭐ Arraste o SO no Inspector

    private float lastAttackTime;

    private void Start()
    {
        // ✅ CORREÇÃO: Para ScriptableObject, NÃO use GetComponent!
        // A referência deve ser arrastada no Inspector ou carregada de outra forma

        if (enemyStatus == null)
        {
            Debug.LogError($"status_inimigo não atribuído no Inspector para {gameObject.name}!");

            // ⭐ Opcional: Tentar carregar automaticamente
            enemyStatus = Resources.Load<status_inimigo>("Inimigos/status_inimigo");
            if (enemyStatus != null)
            {
                Debug.Log("status_inimigo carregado automaticamente dos Resources!");
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            TryDamagePlayer(collision.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            TryDamagePlayer(other.gameObject);
        }
    }

    private void TryDamagePlayer(GameObject player)
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        PlayerStats playerStats = player.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.ReceberDano(damageAmount);
            lastAttackTime = Time.time;

            Debug.Log($"Inimigo causou {damageAmount} de dano no player!");

            // ✅ Agora você pode acessar os dados do ScriptableObject
            if (enemyStatus != null)
            {
                Debug.Log($"Usando dados de: {enemyStatus.name}");
                // Exemplo: enemyStatus.danoBase, enemyStatus.vidaMaxima, etc.
            }
        }
    }
}