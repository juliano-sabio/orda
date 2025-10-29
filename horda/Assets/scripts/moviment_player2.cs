using UnityEngine;

public class moviment_player2 : MonoBehaviour
{
    private PlayerStats playerStats;
    private Rigidbody2D rb;

    private void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        rb = GetComponent<Rigidbody2D>();

        if (playerStats == null)
        {
            Debug.LogError("PlayerStats não encontrado no GameObject!");
        }

        if (rb == null)
        {
            Debug.LogError("Rigidbody2D não encontrado no GameObject!");
        }
    }

    private void Update()
    {
        if (playerStats == null || rb == null) return;

        // Input do jogador
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector2 movement = new Vector2(horizontal, vertical).normalized;

        // ✅ CORREÇÃO: Use GetSpeed() em vez de GetMoveSpeed()
        float currentSpeed = playerStats.GetSpeed();

        // Aplica o movimento
        rb.linearVelocity = movement * currentSpeed;

        // ✅ CORREÇÃO: Debug opcional para verificar a velocidade
        // Debug.Log($"Movimento: {movement} | Velocidade: {currentSpeed}");
    }
}