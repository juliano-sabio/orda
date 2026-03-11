using UnityEngine;
using System.Collections;

public class movi_inimigo_manter_distancia : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    public float velocidade = 3f;
    public float distanciaDetecção = 8f;
    public float distanciaDesejada = 4f; // Distância que quer manter do player
    public float toleranciaDistancia = 0.5f; // Margem de erro aceitável
    public float intervaloAtualizaçãoCaminho = 0.5f;

    [Header("Sistema de Desvio de Obstáculos - SUAVE")]
    public bool usarDesvioObstaculos = true;
    public float distanciaDetecaoObstaculos = 3f;
    public LayerMask camadaObstaculos = -1;
    public float forcaDesvio = 2f;
    public float suavizacaoDesvio = 3f;
    public float anguloDesvio = 45f;

    [Header("Comportamento de Distância")]
    public bool fugirSeMuitoPerto = true;
    public float distanciaFuga = 2.5f; // Se player chegar mais perto que isso, foge
    public float velocidadeFuga = 4f; // Mais rápido ao fugir
    public bool orbitarAoRedor = true; // Se true, tenta se mover lateralmente ao invés de só recuar
    public float forcaOrbital = 1.5f;

    [Header("Ataque")]
    public GameObject prefabProjetil;
    public Transform pontoDisparo; // Vazio filho do inimigo (posição de onde sai o tiro)
    public float intervaloAtaque = 2f;
    public float distanciaAtaque = 5f; // Distância mínima para começar a atirar
    public float forcaDisparo = 8f;
    public bool podeAtirar = true;
    public float tempoRecargaAposFuga = 1f; // Tempo sem atirar depois de fugir

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
    private float distanciaAtualParaPlayer;
    private bool estaMuitoPerto = false;

    // Variáveis para ataque
    private float tempoUltimoAtaque;
    private float tempoUltimaFuga;
    private bool podeAtirarAgora = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        EncontrarPlayer();

        Debug.Log("🚀 Inimigo inicializado - Modo MANTER DISTÂNCIA COM ATAQUE ativado");

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

        // 🎯 SISTEMA DE ATAQUE
        if (podeAtirar && player != null && perseguindoPlayer)
        {
            // Verifica se pode atirar (não está em cooldown de fuga)
            if (Time.time >= tempoUltimaFuga + tempoRecargaAposFuga)
            {
                podeAtirarAgora = true;
            }

            if (podeAtirarAgora)
            {
                float distanciaParaPlayer = Vector2.Distance(transform.position, player.position);
                if (distanciaParaPlayer <= distanciaAtaque && Time.time >= tempoUltimoAtaque + intervaloAtaque)
                {
                    AtirarNoPlayer();
                    tempoUltimoAtaque = Time.time;
                }
            }
        }
    }

    // 🎯 MÉTODO DE ATAQUE
    void AtirarNoPlayer()
    {
        if (prefabProjetil == null || pontoDisparo == null) return;

        // Calcula direção para o player
        Vector2 direcao = (player.position - pontoDisparo.position).normalized;

        // Cria o projétil
        GameObject projetilObj = Instantiate(prefabProjetil, pontoDisparo.position, Quaternion.identity);
        projetil_inimigo projetil = projetilObj.GetComponent<projetil_inimigo>();

        if (projetil != null)
        {
            projetil.SetDirecao(direcao);
        }
        else
        {
            // Fallback se não tiver o script
            Rigidbody2D rb = projetilObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = direcao * forcaDisparo;
            }
        }

        Debug.Log($"🎯 Inimigo atirou na direção: {direcao}");
    }

    // 🔄 CÁLCULO DA DIREÇÃO PARA MANTER DISTÂNCIA
    Vector2 CalcularDirecaoManterDistancia()
    {
        if (player == null) return Vector2.zero;

        distanciaAtualParaPlayer = Vector2.Distance(transform.position, player.position);
        Vector2 direcaoParaPlayer = (player.position - transform.position).normalized;

        // Verifica se está muito perto (modo fuga)
        bool estavaMuitoPerto = estaMuitoPerto;
        estaMuitoPerto = fugirSeMuitoPerto && distanciaAtualParaPlayer < distanciaFuga;

        // Se acabou de entrar em modo fuga, registra o tempo
        if (estaMuitoPerto && !estavaMuitoPerto)
        {
            tempoUltimaFuga = Time.time;
            podeAtirarAgora = false;
        }

        if (estaMuitoPerto)
        {
            // MODO FUGA - Afasta-se do player
            Vector2 direcaoFuga = -direcaoParaPlayer;

            // Se orbitar estiver ativo, adiciona movimento lateral para não ser previsível
            if (orbitarAoRedor)
            {
                float tempoOrbita = Time.time * 0.5f;
                Vector2 direcaoLateral = new Vector2(-direcaoParaPlayer.y, direcaoParaPlayer.x) * Mathf.Sin(tempoOrbita);
                direcaoFuga = (direcaoFuga + direcaoLateral * forcaOrbital).normalized;
            }

            Debug.DrawRay(transform.position, direcaoFuga * 2f, Color.red);
            return direcaoFuga;
        }
        else if (distanciaAtualParaPlayer > distanciaDesejada + toleranciaDistancia)
        {
            // MODO APROXIMAÇÃO - Muito longe, precisa se aproximar
            Debug.DrawRay(transform.position, direcaoParaPlayer * 2f, Color.yellow);
            return direcaoParaPlayer;
        }
        else if (distanciaAtualParaPlayer < distanciaDesejada - toleranciaDistancia)
        {
            // MODO AFASTAMENTO - Muito perto, precisa se afastar um pouco
            Vector2 direcaoAfastar = -direcaoParaPlayer;

            if (orbitarAoRedor)
            {
                float tempoOrbita = Time.time * 0.3f;
                Vector2 direcaoLateral = new Vector2(-direcaoParaPlayer.y, direcaoParaPlayer.x) * Mathf.Sin(tempoOrbita);
                direcaoAfastar = (direcaoAfastar + direcaoLateral * forcaOrbital * 0.5f).normalized;
            }

            Debug.DrawRay(transform.position, direcaoAfastar * 2f, Color.cyan);
            return direcaoAfastar;
        }
        else
        {
            // DISTÂNCIA IDEAL - Mantém posição ou orbita suavemente
            if (orbitarAoRedor)
            {
                float tempoOrbita = Time.time * 0.2f;
                Vector2 direcaoOrbital = new Vector2(-direcaoParaPlayer.y, direcaoParaPlayer.x) * Mathf.Sin(tempoOrbita);
                Debug.DrawRay(transform.position, direcaoOrbital * 2f, Color.green);
                return direcaoOrbital;
            }
            else
            {
                return Vector2.zero;
            }
        }
    }

    // 🔄 SISTEMA DE DESVIO SUAVE
    void CalcularDesvioSuave()
    {
        direcaoDesvio = Vector2.zero;
        intensidadeDesvio = 0f;

        if (direcaoDesejada == Vector2.zero) return;

        RaycastHit2D[] hits = Physics2D.CircleCastAll(
            transform.position,
            0.5f,
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
                float intensidade = 1f - (hit.distance / distanciaDetecaoObstaculos);
                intensidade = Mathf.Clamp01(intensidade);

                Vector2 direcaoObstaculo = (transform.position - hit.collider.transform.position).normalized;
                direcaoDesvio += direcaoObstaculo * intensidade;
                intensidadeDesvio = Mathf.Max(intensidadeDesvio, intensidade);
            }
        }

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

            Vector2 direita = Quaternion.Euler(0, 0, -anguloDesvio) * direcaoDesejada;
            Vector2 esquerda = Quaternion.Euler(0, 0, anguloDesvio) * direcaoDesejada;

            float dotDireita = Vector2.Dot(direita, hitFrente.normal);
            float dotEsquerda = Vector2.Dot(esquerda, hitFrente.normal);

            Vector2 direcaoEvasao = dotDireita > dotEsquerda ? direita : esquerda;
            direcaoDesvio += direcaoEvasao * intensidadeFrente * 2f;
            intensidadeDesvio = Mathf.Max(intensidadeDesvio, intensidadeFrente * 1.5f);
        }

        if (direcaoDesvio != Vector2.zero)
        {
            direcaoDesvio = direcaoDesvio.normalized * forcaDesvio * intensidadeDesvio;
        }
    }

    void AplicarMovimentoSuave()
    {
        Vector2 direcaoFinal = direcaoDesejada;
        float velocidadeAtual = velocidade;

        if (estaMuitoPerto)
        {
            velocidadeAtual = velocidadeFuga;
        }

        if (direcaoDesvio != Vector2.zero)
        {
            direcaoFinal = (direcaoDesejada + direcaoDesvio).normalized;
            direcaoMovimento = Vector2.Lerp(direcaoMovimento, direcaoFinal, Time.deltaTime * suavizacaoDesvio);
        }
        else
        {
            direcaoMovimento = Vector2.Lerp(direcaoMovimento, direcaoDesejada, Time.deltaTime * suavizacaoDesvio);
        }

        if (direcaoMovimento != Vector2.zero)
        {
            rb.linearVelocity = direcaoMovimento * velocidadeAtual;

            if (direcaoMovimento.x != 0)
            {
                float escalaX = Mathf.Abs(transform.localScale.x);
                float direcaoDesejadaEscala = Mathf.Sign(direcaoMovimento.x);
                float direcaoAtual = Mathf.Sign(transform.localScale.x);

                if (Mathf.Abs(direcaoDesejadaEscala - direcaoAtual) > 0.1f)
                {
                    transform.localScale = new Vector3(
                        direcaoDesejadaEscala * escalaX,
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
            direcaoDesejada = CalcularDirecaoManterDistancia();
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
        if (!collision.collider.CompareTag("Player"))
        {
            Vector2 direcaoRepulsao = (transform.position - collision.transform.position).normalized;
            direcaoDesvio += direcaoRepulsao * forcaDesvio * 0.5f;
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

        while (tempo < 0.3f)
        {
            rb.linearVelocity = Vector2.Lerp(velocidadeOriginal, Vector2.zero, tempo / 0.3f);
            tempo += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.3f);

        tempo = 0f;
        while (tempo < 0.3f && rb != null)
        {
            rb.linearVelocity = Vector2.Lerp(Vector2.zero, direcaoMovimento * velocidade, tempo / 0.3f);
            tempo += Time.deltaTime;
            yield return null;
        }
    }

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

    [ContextMenu("🎯 Ativar/Desativar Ataque")]
    public void ToggleAtaque()
    {
        podeAtirar = !podeAtirar;
        Debug.Log($"🎯 Sistema de ataque: {(podeAtirar ? "ATIVADO" : "DESATIVADO")}");
    }

    [ContextMenu("📊 Status do Inimigo")]
    public void DebugStatus()
    {
        Debug.Log($"=== STATUS INIMIGO MANTER DISTÂNCIA ===");
        Debug.Log($"🎯 Player: {(player != null ? player.name : "NULO")}");
        Debug.Log($"🎯 Perseguindo: {perseguindoPlayer}");
        Debug.Log($"📏 Distância atual: {distanciaAtualParaPlayer:F2} / Desejada: {distanciaDesejada:F2}");
        Debug.Log($"🚨 Muito perto: {estaMuitoPerto}");
        Debug.Log($"🔄 Intensidade Desvio: {intensidadeDesvio:F2}");
        Debug.Log($"🧭 Direção Atual: {direcaoMovimento}");
        Debug.Log($"🎯 Pode atirar: {podeAtirar}");
        Debug.Log($"🎯 Próximo tiro em: {Mathf.Max(0, tempoUltimoAtaque + intervaloAtaque - Time.time):F1}s");
    }

    void OnDrawGizmos()
    {
        // Área de detecção (amarelo)
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, distanciaDetecção);

        // Distância desejada (verde)
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, distanciaDesejada);

        // Distância de fuga (vermelho)
        if (fugirSeMuitoPerto)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, distanciaFuga);
        }

        // Distância de ataque (laranja)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, distanciaAtaque);

        // Área de detecção de obstáculos (azul)
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, distanciaDetecaoObstaculos);

        // Ponto de disparo (vermelho)
        if (pontoDisparo != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(pontoDisparo.position, 0.1f);
            Gizmos.DrawLine(pontoDisparo.position, pontoDisparo.position + (Vector3)(direcaoMovimento * 0.5f));
        }

        // Linha até o player com cor baseada no estado
        if (player != null)
        {
            if (estaMuitoPerto)
                Gizmos.color = Color.red;
            else if (distanciaAtualParaPlayer > distanciaDesejada + toleranciaDistancia)
                Gizmos.color = Color.yellow;
            else if (distanciaAtualParaPlayer < distanciaDesejada - toleranciaDistancia)
                Gizmos.color = Color.cyan;
            else
                Gizmos.color = Color.green;

            Gizmos.DrawLine(transform.position, player.position);
        }

        // Direções
        if (direcaoMovimento != Vector2.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, direcaoMovimento * 1.5f);
        }
    }
}