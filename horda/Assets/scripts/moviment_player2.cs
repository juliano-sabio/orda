using UnityEngine;

public class moviment_player2 : MonoBehaviour
{
    private PlayerStats playerStats;
    private Rigidbody2D rb;
    private Animator anim;

    // Variável para guardar o tamanho original do seu Player
    private Vector3 originalScale;

    private void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        // ✅ SALVA A ESCALA ATUAL (aquela que você definiu no Inspector)
        originalScale = transform.localScale;

        if (playerStats == null) Debug.LogError("PlayerStats não encontrado!");
        if (rb == null) Debug.LogError("Rigidbody2D não encontrado!");
    }

    private void Update()
    {
        if (playerStats == null || rb == null) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector2 movement = new Vector2(horizontal, vertical).normalized;
        float currentSpeed = playerStats.GetSpeed();

        rb.linearVelocity = movement * currentSpeed;

        // Animação
        if (anim != null)
        {
            anim.SetFloat("Speed", movement.sqrMagnitude);
        }

        // ✅ FLIP CORRIGIDO: Mantém o tamanho original e só inverte o X
        if (horizontal > 0)
        {
            transform.localScale = originalScale;
        }
        else if (horizontal < 0)
        {
            transform.localScale = new Vector3(-originalScale.x, originalScale.y, originalScale.z);
        }
    }
}