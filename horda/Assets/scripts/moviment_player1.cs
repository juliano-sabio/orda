using UnityEngine;

public class moviment_player : MonoBehaviour
{
    private PlayerStats playerStats;
    private Rigidbody2D rb;

    private void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Seu código de input aqui...
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector2 movement = new Vector2(horizontal, vertical).normalized;

        // Pega a velocidade atualizada do PlayerStats
        float currentSpeed = playerStats.GetMoveSpeed();

        // Aplica o movimento
        rb.linearVelocity = movement * currentSpeed;
    }
}