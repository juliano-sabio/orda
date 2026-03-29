using UnityEngine;
using System.Collections.Generic;

public class SwordSpinSkillBehavior : SkillBehavior
{
    [Header("Configurações de Vida")]
    public float duration = 5f; // Duração mínima garantida
    public float rotationSpeed = 360f;
    public float damageMultiplier = 1.5f;

    private float spawnTime;

    public override void Initialize(PlayerStats stats)
    {
        base.Initialize(stats);
        spawnTime = Time.time;

        // Garante que a espada se destrua APENAS após a duração definida
        Destroy(gameObject, duration);

        // Impede que a espada herde rotações estranhas do player
        transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
    }

    public override void ApplyEffect() { }
    public override void RemoveEffect() { Destroy(gameObject); }

    void Update()
    {
        if (playerStats != null)
        {
            // Segue o player e gira
            transform.position = playerStats.transform.position;
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // Lógica de dano que você já tem...
            var inimigo = other.GetComponent<InimigoController>();
            if (inimigo != null) inimigo.ReceberDano(playerStats.attack * damageMultiplier);
        }
    }
}