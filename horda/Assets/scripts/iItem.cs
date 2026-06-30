
using UnityEngine;

public class Item : MonoBehaviour
{
    public enum StatType
    {
        Health,
        Attack,
        Defense,
        Speed
    }

    [Header("Configurações do Item")]
    public string itemName;
    public StatType statToBoost;
    public float boostValue = 5f;
    public AudioClip collectSound;

    [Header("Efeitos Visuais")]
    public ParticleSystem collectParticles;
    public float rotationSpeed = 50f;

    void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CollectItem(other.GetComponent<PlayerStats>());
        }
    }

    private void CollectItem(PlayerStats playerStats)
    {
        if (playerStats != null)
        {
            // ✅ CORREÇÃO: Chama o método com os 3 parâmetros necessários
            playerStats.ApplyItemEffect(itemName, statToBoost.ToString(), boostValue);

            if (collectSound != null)
                AudioSource.PlayClipAtPoint(collectSound, transform.position);

            if (collectParticles != null)
                Instantiate(collectParticles, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}