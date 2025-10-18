using UnityEditorInternal;
using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    status_inimigo  status_inimigo; 
    public float tempoEntreDanos = 1f; // Intervalo entre danos (para não causar dano a cada frame)
    private float tempoProximoDano = 0f;
    private void Start()
    {
        status_inimigo = GetComponent<status_inimigo>();
    }
    private void OnCollisionStay2D(Collision2D collision) // Para jogos 2D
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time >= tempoProximoDano)
            {
                // Procura o script de vida no Player
                PlayerStats playerHealth = collision.gameObject.GetComponent<PlayerStats>();

                if (playerHealth != null)
                {
                    playerHealth.ReceberDano(status_inimigo.dano);
                }

                // Define o tempo do próximo dano
                tempoProximoDano = Time.time + tempoEntreDanos;
            }
        }
    }

}


