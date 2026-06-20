using System.Collections;
using UnityEngine;

public class moviment_player2 : MonoBehaviour
{
    private PlayerStats playerStats;
    private Rigidbody2D rb;
    private Animator anim;

    private Vector2 moveInput;
    private Vector3 originalScale;

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.15f;

    private bool isDashing = false;
    private Vector2 dashDirection;
    private DashEffect dashEffect;

    private float tempoImobilizado = 0f;

    public void Imobilizar(float duracao)
    {
        // Fundação Firme (infusão defensiva) — imune a controle/empurrão enquanto ativa
        if (GetComponent<FundacaoFirmeMarker>() != null) return;
        tempoImobilizado = Mathf.Max(tempoImobilizado, duracao);
        isDashing = false;
        playerStats?.AplicarParalisiaPlayer(duracao);
    }

    private void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        dashEffect = GetComponent<DashEffect>();

        originalScale = transform.localScale;

        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    private void Update()
    {
        if (playerStats == null || rb == null) return;
        if (!playerStats.IsLocalAuthority) return;
        if (playerStats.EstaCaido) return;
        if (LobbyState.EmLobby) return;

        if (tempoImobilizado > 0f)
        {
            tempoImobilizado -= Time.deltaTime;
            moveInput = Vector2.zero;
            if (anim != null) anim.SetFloat("Speed", 0f);
            return;
        }

        Vector2 eixo = InputBindings.EixoMovimento();
        float horizontal = eixo.x;
        float vertical = eixo.y;
        moveInput = new Vector2(horizontal, vertical).normalized;

        if (anim != null)
            anim.SetFloat("Speed", moveInput.sqrMagnitude);

        if (horizontal > 0)
            transform.localScale = originalScale;
        else if (horizontal < 0)
            transform.localScale = new Vector3(-originalScale.x, originalScale.y, originalScale.z);

        if (InputBindings.DashDown() && !isDashing && playerStats.HasDashCharge())
        {
            dashDirection = moveInput != Vector2.zero ? moveInput : new Vector2(transform.localScale.x > 0 ? 1f : -1f, 0f);
            playerStats.ConsumeDashCharge();
            StartCoroutine(DashCoroutine());
        }
    }

    private void FixedUpdate()
    {
        if (playerStats == null || rb == null) return;
        if (!playerStats.IsLocalAuthority) return;
        if (playerStats.EstaCaido) { rb.linearVelocity = Vector2.zero; return; }
        if (LobbyState.EmLobby) { rb.linearVelocity = Vector2.zero; return; }

        if (tempoImobilizado > 0f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isDashing)
            rb.linearVelocity = dashDirection * dashSpeed;
        else
            rb.linearVelocity = moveInput * playerStats.GetSpeed();
    }

    private IEnumerator DashCoroutine()
    {
        isDashing = true;
        dashEffect?.IniciarEfeito(dashDirection);

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
        dashEffect?.PararEfeito();
    }
}
