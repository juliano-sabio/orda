using UnityEngine;

public class moviment_player2 : MonoBehaviour
{
    private PlayerStats playerStats;
    private Rigidbody2D rb;
    private Animator anim;

    private Vector2 moveInput;
    private Vector3 originalScale;

    private void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        originalScale = transform.localScale;

        // Garante que o Rigidbody2D não tenha atrito impedindo o movimento
        if (rb != null)
        {
            rb.gravityScale = 0; // Se for jogo Top-Down
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Suaviza o movimento
        }
    }

    private void Update()
    {
        if (playerStats == null || rb == null) return;

        // 1. CAPTURA DE INPUT (Melhor no Update)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(horizontal, vertical).normalized;

        // 2. ANIMAÇÃO
        if (anim != null)
        {
            anim.SetFloat("Speed", moveInput.sqrMagnitude);
        }

        // 3. FLIP (Apenas quando houver movimento horizontal)
        if (horizontal > 0)
        {
            transform.localScale = originalScale;
        }
        else if (horizontal < 0)
        {
            transform.localScale = new Vector3(-originalScale.x, originalScale.y, originalScale.z);
        }
    }

    private void FixedUpdate()
    {
        // 4. MOVIMENTAÇÃO FÍSICA (Sempre no FixedUpdate para evitar lentidão/stuttering)
        if (playerStats != null && rb != null)
        {
            float speed = playerStats.GetSpeed();
            rb.linearVelocity = moveInput * speed;
        }
    }
}