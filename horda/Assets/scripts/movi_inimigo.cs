using UnityEngine;
using System.Collections;

public class movi_inimigo : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    public float velocidade = 3f;
    public float distanciaDetecção = 8f;
    public float distanciaAtaque = 2f;
    public float intervaloAtualizaçãoCaminho = 0.5f;

    [Header("Sistema de Desvio de Obstáculos - SUAVE")]
    public bool usarDesvioObstaculos = true;
    public float distanciaDetecaoObstaculos = 3f;
    public LayerMask camadaObstaculos = -1;
    public float forcaDesvio = 2f;
    public float suavizacaoDesvio = 3f;
    public float anguloDesvio = 45f;

    [Header("Referências")]
    public Transform player;
    public Transform[] pontosCaminho;

    private Rigidbody2D rb;
    private Vector2 direcaoMovimento;
    private Vector2 direcaoDesejada;
    private int pontoAtual = 0;
    private float tempoUltimaAtualização;
    private bool perseguindoPlayer = false;
    private bool procurandoPlayer = false;
    private Vector2 direcaoDesvio;
    private float intensidadeDesvio;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        EncontrarPlayer();

        Debug.Log("🚀 Inimigo inicializado - Sistema de desvio SUAVE ativado");

        tempoUltimaAtualização = Time.time;
        StartCoroutine(ProcurarPlayerPeriodicamente());
    }

    void Update()
    {
        if (player == null && !procurandoPlayer)
        {
            EncontrarPlayer();
            return;
        }

        if (player == null) return;

        VerificarPlayer();

        if (Time.time - tempoUltimaAtualização >= intervaloAtualizaçãoCaminho)
        {
            AtualizarCaminho();
            tempoUltimaAtualização = Time.time;
        }

        // 🔄 DESVIO SUAVE
        if (usarDesvioObstaculos)
        {
            CalcularDesvioSuave();
        }

        AplicarMovimentoSuave();
    }

    // 🔄 SISTEMA DE DESVIO SUAVE
    void CalcularDesvioSuave()
    {
        direcaoDesvio = Vector2.zero;
        intensidadeDesvio = 0f;

        if (direcaoDesejada == Vector2.zero) return;

        // Detecta obstáculos usando sphere cast para mais naturalidade
        RaycastHit2D[] hits = Physics2D.CircleCastAll(
            transform.position,
            0.5f, // Raio pequeno para detectar perto do inimigo
            direcaoDesejada,
            distanciaDetecaoObstaculos,
            camadaObstaculos
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null &&
                hit.collider.gameObject != this.gameObject &&
                !hit.collider.CompareTag("Player"))
            {
                // Calcula intensidade baseada na distância (mais perto = mais intenso)
                float intensidade = 1f - (hit.distance / distanciaDetecaoObstaculos);
                intensidade = Mathf.Clamp01(intensidade);

                // Direção de desvio é perpendicular ao obstáculo
                Vector2 direcaoObstaculo = (transform.position - hit.collider.transform.position).normalized;
                direcaoDesvio += direcaoObstaculo * intensidade;
                intensidadeDesvio = Mathf.Max(intensidadeDesvio, intensidade);

                Debug.DrawRay(transform.position, direcaoObstaculo * intensidade * 2f, Color.magenta);
            }
        }

        // 🔄 RAYCAST FRONTAL PARA DETECÇÃO ANTECIPADA
        RaycastHit2D hitFrente = Physics2D.Raycast(
            transform.position,
            direcaoDesejada,
            distanciaDetecaoObstaculos,
            camadaObstaculos
        );

        if (hitFrente.collider != null &&
            hitFrente.collider.gameObject != this.gameObject &&
            !hitFrente.collider.CompareTag("Player"))
        {
            float intensidadeFrente = 1f - (hitFrente.distance / distanciaDetecaoObstaculos);
            intensidadeFrente = Mathf.Clamp01(intensidadeFrente);

            // Para obstáculos na frente, usa desvio mais forte mas ainda suave
            Vector2 direita = Quaternion.Euler(0, 0, -anguloDesvio) * direcaoDesejada;
            Vector2 esquerda = Quaternion.Euler(0, 0, anguloDesvio) * direcaoDesejada;

            // Escolhe a direção que mais se afasta do obstáculo
            float dotDireita = Vector2.Dot(direita, hitFrente.normal);
            float dotEsquerda = Vector2.Dot(esquerda, hitFrente.normal);

            Vector2 direcaoEvasao = dotDireita > dotEsquerda ? direita : esquerda;
            direcaoDesvio += direcaoEvasao * intensidadeFrente * 2f;
            intensidadeDesvio = Mathf.Max(intensidadeDesvio, intensidadeFrente * 1.5f);

            Debug.DrawRay(transform.position, direcaoEvasao * intensidadeFrente * 2f, Color.cyan);
        }

        // Normaliza e aplica força
        if (direcaoDesvio != Vector2.zero)
        {
            direcaoDesvio = direcaoDesvio.normalized * forcaDesvio * intensidadeDesvio;
        }
    }

    // 🔄 MOVIMENTO SUAVE COMBINANDO DIREÇÕES
    void AplicarMovimentoSuave()
    {
        Vector2 direcaoFinal = direcaoDesejada;

        // Combina direção desejada com desvio de forma suave
        if (direcaoDesvio != Vector2.zero)
        {
            direcaoFinal = (direcaoDesejada + direcaoDesvio).normalized;

            // Interpolação suave entre direções
            direcaoMovimento = Vector2.Lerp(direcaoMovimento, direcaoFinal, Time.deltaTime * suavizacaoDesvio);
        }
        else
        {
            // Sem desvio, volta suavemente para direção desejada
            direcaoMovimento = Vector2.Lerp(direcaoMovimento, direcaoDesejada, Time.deltaTime * suavizacaoDesvio);
        }

        // Aplica movimento
        if (direcaoMovimento != Vector2.zero)
        {
            rb.linearVelocity = direcaoMovimento * velocidade;

            // Rotação suave
            if (direcaoMovimento.x != 0)
            {
                float escalaX = Mathf.Abs(transform.localScale.x);
                float direcaoDesejada = Mathf.Sign(direcaoMovimento.x);
                float direcaoAtual = Mathf.Sign(transform.localScale.x);

                // Suaviza a rotação também
                if (Mathf.Abs(direcaoDesejada - direcaoAtual) > 0.1f)
                {
                    transform.localScale = new Vector3(
                        direcaoDesejada * escalaX,
                        transform.localScale.y,
                        transform.localScale.z
                    );
                }
            }
        }
        else
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * suavizacaoDesvio);
        }
    }

    void EncontrarPlayer()
    {
        procurandoPlayer = true;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log($"🎯 Player encontrado: {player.name}");
        }

        procurandoPlayer = false;
    }

    IEnumerator ProcurarPlayerPeriodicamente()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f);
            if (player == null)
            {
                EncontrarPlayer();
            }
        }
    }

    void VerificarPlayer()
    {
        if (player == null) return;

        float distanciaParaPlayer = Vector2.Distance(transform.position, player.position);
        perseguindoPlayer = distanciaParaPlayer <= distanciaDetecção;
    }

    void AtualizarCaminho()
    {
        if (perseguindoPlayer && player != null)
        {
            direcaoDesejada = (player.position - transform.position).normalized;
        }
        else
        {
            SeguirCaminho();
        }
    }

    void SeguirCaminho()
    {
        if (pontosCaminho == null || pontosCaminho.Length == 0)
        {
            direcaoDesejada = Vector2.zero;
            return;
        }

        if (pontoAtual >= pontosCaminho.Length || pontosCaminho[pontoAtual] == null)
        {
            pontoAtual = 0;
        }

        Vector2 direcao = (pontosCaminho[pontoAtual].position - transform.position).normalized;
        direcaoDesejada = direcao;

        float distanciaParaPonto = Vector2.Distance(transform.position, pontosCaminho[pontoAtual].position);
        if (distanciaParaPonto < 0.5f)
        {
            pontoAtual = (pontoAtual + 1) % pontosCaminho.Length;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 🔄 REAÇÃO SUAVE A COLISÕES
        if (!collision.collider.CompareTag("Player"))
        {
            Debug.Log($"💥 Colisão suave com: {collision.collider.name}");

            // Direção de repulsão suave
            Vector2 direcaoRepulsao = (transform.position - collision.transform.position).normalized;
            direcaoDesvio += direcaoRepulsao * forcaDesvio * 0.5f; // Força reduzida para suavidade
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(PararMovimentoTemporariamente());
        }
    }

    IEnumerator PararMovimentoTemporariamente()
    {
        Vector2 velocidadeOriginal = rb.linearVelocity;
        float tempo = 0f;

        // Para suavemente
        while (tempo < 0.3f)
        {
            rb.linearVelocity = Vector2.Lerp(velocidadeOriginal, Vector2.zero, tempo / 0.3f);
            tempo += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.3f);

        // Retoma suavemente
        tempo = 0f;
        while (tempo < 0.3f && rb != null)
        {
            rb.linearVelocity = Vector2.Lerp(Vector2.zero, direcaoMovimento * velocidade, tempo / 0.3f);
            tempo += Time.deltaTime;
            yield return null;
        }
    }

    // 🔄 MÉTODOS DE CONFIGURAÇÃO SUAVE
    [ContextMenu("🔄 Aumentar Suavização")]
    public void AumentarSuavizacao()
    {
        suavizacaoDesvio += 1f;
        Debug.Log($"🔄 Suavização aumentada para: {suavizacaoDesvio}");
    }

    [ContextMenu("🔄 Diminuir Suavização")]
    public void DiminuirSuavizacao()
    {
        suavizacaoDesvio = Mathf.Max(1f, suavizacaoDesvio - 1f);
        Debug.Log($"🔄 Suavização diminuída para: {suavizacaoDesvio}");
    }

    [ContextMenu("🎯 Ativar/Desativar Desvio")]
    public void ToggleDesvio()
    {
        usarDesvioObstaculos = !usarDesvioObstaculos;
        Debug.Log($"🔄 Desvio de obstáculos: {(usarDesvioObstaculos ? "ATIVADO" : "DESATIVADO")}");
    }

    [ContextMenu("📊 Status do Inimigo")]
    public void DebugStatus()
    {
        Debug.Log($"=== STATUS INIMIGO SUAVE ===");
        Debug.Log($"🎯 Player: {(player != null ? player.name : "NULO")}");
        Debug.Log($"🎯 Perseguindo: {perseguindoPlayer}");
        Debug.Log($"🔄 Intensidade Desvio: {intensidadeDesvio:F2}");
        Debug.Log($"📏 Velocidade: {rb.linearVelocity.magnitude:F1}");
        Debug.Log($"🧭 Direção Desejada: {direcaoDesejada}");
        Debug.Log($"🧭 Direção Atual: {direcaoMovimento}");
        Debug.Log($"🧭 Direção Desvio: {direcaoDesvio}");
        Debug.Log($"⚡ Suavização: {suavizacaoDesvio}");
    }

    // 🔄 GIZMOS SUAVES
    void OnDrawGizmos()
    {
        // Área de detecção do player (amarelo suave)
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, distanciaDetecção);

        // Área de detecção de obstáculos (azul suave)
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, distanciaDetecaoObstaculos);

        // Direção desejada (verde)
        if (direcaoDesejada != Vector2.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, direcaoDesejada * 1.5f);
        }

        // Direção atual (azul)
        if (direcaoMovimento != Vector2.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, direcaoMovimento * 1.5f);
        }

        // Direção de desvio (magenta)
        if (direcaoDesvio != Vector2.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, direcaoDesvio * 0.5f);
        }

        // Linha até o player (branco/vermelho)
        if (player != null)
        {
            Gizmos.color = perseguindoPlayer ? Color.red : Color.white;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Gizmos mais destacados quando selecionado
        OnDrawGizmos();

        // Adiciona sphere sólida para área de obstáculos quando selecionado
        Gizmos.color = new Color(0f, 1f, 1f, 0.1f);
        Gizmos.DrawSphere(transform.position, distanciaDetecaoObstaculos);
    }
}