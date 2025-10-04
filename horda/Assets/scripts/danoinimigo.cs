using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public int dano = 10; // Quanto de dano o inimigo causa
    public float tempoEntreDanos = 1f; // Intervalo entre danos (para não causar dano a cada frame)
    private float tempoProximoDano = 0f;

    private void OnCollisionStay2D(Collision2D collision) // Para jogos 2D
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time >= tempoProximoDano)
            {
                // Procura o script de vida no Player
                PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();

                if (playerHealth != null)
                {
                    playerHealth.ReceberDano(dano);
                }

                // Define o tempo do próximo dano
                tempoProximoDano = Time.time + tempoEntreDanos;
            }
        }
    }

    private void OnCollisionStay(Collision collision) // Para jogos 3D
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time >= tempoProximoDano)
            {
                PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();

                if (playerHealth != null)
                {
                    playerHealth.ReceberDano(dano);
                }

                tempoProximoDano = Time.time + tempoEntreDanos;
            }
        }
    }
}


