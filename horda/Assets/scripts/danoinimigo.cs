using UnityEngine;

public class DanoInimigo : MonoBehaviour
{
    [Header("Configurações de Dano")]
    public float dano = 10f;
    public float intervaloAtaque = 1f;
    public bool danoContinuo = false;

    private float proximoAtaque = 0f;
    private PlayerStats playerStats;

    void Start()
    {
        playerStats = FindAnyObjectByType<PlayerStats>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (!danoContinuo)
            {
                AplicarDano(other.GetComponent<PlayerStats>());
            }
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && danoContinuo)
        {
            if (Time.time >= proximoAtaque)
            {
                AplicarDano(other.GetComponent<PlayerStats>());
                proximoAtaque = Time.time + intervaloAtaque;
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (!danoContinuo)
            {
                AplicarDano(collision.gameObject.GetComponent<PlayerStats>());
            }
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && danoContinuo)
        {
            if (Time.time >= proximoAtaque)
            {
                AplicarDano(collision.gameObject.GetComponent<PlayerStats>());
                proximoAtaque = Time.time + intervaloAtaque;
            }
        }
    }

    private void AplicarDano(PlayerStats stats)
    {
        if (stats != null)
        {
            // Use o método correto - TakeDamage em vez de ReceberDano
            stats.TakeDamage(dano);
            Debug.Log($"💥 Inimigo causou {dano} de dano no jogador!");
        }
    }

    // Método para configurar o dano dinamicamente
    public void SetDano(float novoDano)
    {
        dano = novoDano;
    }

    // Método para aumentar o dano (útil para inimigos que ficam mais fortes)
    public void AumentarDano(float bonus)
    {
        dano += bonus;
    }
}