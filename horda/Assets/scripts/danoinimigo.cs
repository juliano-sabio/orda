using UnityEngine;

public class DanoInimigo : MonoBehaviour
{
    [Header("Configurações de Dano")]
    public float dano = 10f;
    public float intervaloAtaque = 2.5f;
    public bool danoContinuo = false;

    private float proximoAtaque = 0f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            AplicarDano(other.GetComponent<PlayerStats>());
            proximoAtaque = Time.time + intervaloAtaque;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && Time.time >= proximoAtaque)
        {
            AplicarDano(other.GetComponent<PlayerStats>());
            proximoAtaque = Time.time + intervaloAtaque;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            AplicarDano(collision.gameObject.GetComponent<PlayerStats>());
            proximoAtaque = Time.time + intervaloAtaque;
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && Time.time >= proximoAtaque)
        {
            AplicarDano(collision.gameObject.GetComponent<PlayerStats>());
            proximoAtaque = Time.time + intervaloAtaque;
        }
    }

    private void AplicarDano(PlayerStats stats)
    {
        if (stats != null)
            stats.TakeDamage(dano);
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