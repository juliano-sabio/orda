using UnityEditorInternal;
using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    stats_inimigo  stats_inimigo; 
    public float tempoEntreDanos = 1f; // Intervalo entre danos (para não causar dano a cada frame)
    private float tempoProximoDano = 0f;
    private void Start()
    {
        stats_inimigo = GetComponent<stats_inimigo>();
    }
    private void OnCollisionStay2D(Collision2D collision) // Para jogos 2D
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time >= tempoProximoDano)
            {
                // Procura o script de vida no Player
                player_stats playerHealth = collision.gameObject.GetComponent<player_stats>();

                if (playerHealth != null)
                {
                    playerHealth.ReceberDano(stats_inimigo.dano);
                }

                // Define o tempo do próximo dano
                tempoProximoDano = Time.time + tempoEntreDanos;
            }
        }
    }

}


