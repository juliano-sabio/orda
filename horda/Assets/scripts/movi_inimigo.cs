using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InimigoPathfinder : MonoBehaviour
{
    [Header("Configurações de Pathfinding")]
    stats_inimigo stats_Inimigo;
    public float distanciaParada = 1f;
    public float raioDetecao = 5f;
    public float intervaloAtualizacaoPath = 0.5f;
    public int numeroRaios = 8;
    public float distanciaMaximaRaycast = 4f;

    [Header("Debug")]
    public bool mostrarDebug = true;
    public Color corRaios = Color.yellow;
    public Color corCaminho = Color.green;

    [Header("Referências")]
    public LayerMask camadaObstaculos;
    public LayerMask camadaPlayer;

    private Transform player;
    private Rigidbody2D rb;
    private List<Vector2> caminhoAtual = new List<Vector2>();
    private int indiceWaypointAtual = 0;
    private float tempoUltimaAtualizacao;
    private Vector2 ultimaPosicaoPlayer;

    void Start()
    {
        stats_Inimigo = GetComponent<stats_inimigo>();
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;
        }

        EncontrarPlayer();
    }

    void Update()
    {
        if (player == null)
        {
            EncontrarPlayer();
            return;
        }

        // Atualizar path se necessário
        if (Time.time - tempoUltimaAtualizacao > intervaloAtualizacaoPath ||
            Vector2.Distance(player.position, ultimaPosicaoPlayer) > 1f)
        {
            CalcularNovoCaminho();
            tempoUltimaAtualizacao = Time.time;
            ultimaPosicaoPlayer = player.position;
        }

        SeguirCaminho();
    }

    void EncontrarPlayer()
    {
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, raioDetecao, camadaPlayer);
        if (playerCollider != null)
        {
            player = playerCollider.transform;
            ultimaPosicaoPlayer = player.position;
        }
    }

    void CalcularNovoCaminho()
    {
        if (player == null) return;

        caminhoAtual.Clear();
        indiceWaypointAtual = 0;

        Vector2 direcaoPlayer = (Vector2)player.position - (Vector2)transform.position;
        float distanciaPlayer = direcaoPlayer.magnitude;

        // Verificar se há caminho direto
        RaycastHit2D hitDireto = Physics2D.Raycast(transform.position, direcaoPlayer.normalized,
                                                  distanciaPlayer, camadaObstaculos);

        if (hitDireto.collider == null)
        {
            // Caminho direto disponível
            caminhoAtual.Add(player.position);
        }
        else
        {
            // Encontrar caminho alternativo
            Vector2 waypoint = EncontrarWaypointAlternativo(direcaoPlayer, hitDireto.point);
            caminhoAtual.Add(waypoint);
            caminhoAtual.Add(player.position);
        }
    }

    Vector2 EncontrarWaypointAlternativo(Vector2 direcaoOriginal, Vector2 pontoColisao)
    {
        float melhorPontuacao = -Mathf.Infinity;
        Vector2 melhorDirecao = direcaoOriginal.normalized;

        // Testar diferentes direções ao redor
        for (int i = 0; i < numeroRaios; i++)
        {
            float angulo = (i / (float)numeroRaios) * 360f;
            Vector2 direcaoTeste = RotateVector(direcaoOriginal.normalized, angulo);

            // Verificar se esta direção está livre
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direcaoTeste,
                                               distanciaMaximaRaycast, camadaObstaculos);

            if (hit.collider == null)
            {
                // Direção livre - calcular quão boa ela é
                float pontuacao = CalcularPontuacaoDirecao(direcaoTeste, direcaoOriginal);
                if (pontuacao > melhorPontuacao)
                {
                    melhorPontuacao = pontuacao;
                    melhorDirecao = direcaoTeste;
                }
            }
            else
            {
                // Tentar direção que contorna o obstáculo
                Vector2 direcaoContorno = CalcularDirecaoContorno(hit.normal);
                float pontuacao = CalcularPontuacaoDirecao(direcaoContorno, direcaoOriginal);
                if (pontuacao > melhorPontuacao)
                {
                    melhorPontuacao = pontuacao;
                    melhorDirecao = direcaoContorno;
                }
            }
        }

        return (Vector2)transform.position + melhorDirecao * distanciaMaximaRaycast;
    }

    Vector2 CalcularDirecaoContorno(Vector2 normalObstaculo)
    {
        // Calcular direção que contorna o obstáculo
        Vector2 direita = new Vector2(-normalObstaculo.y, normalObstaculo.x);
        Vector2 esquerda = new Vector2(normalObstaculo.y, -normalObstaculo.x);

        // Testar qual direção é melhor
        RaycastHit2D hitDireita = Physics2D.Raycast(transform.position, direita, 2f, camadaObstaculos);
        RaycastHit2D hitEsquerda = Physics2D.Raycast(transform.position, esquerda, 2f, camadaObstaculos);

        if (hitDireita.collider == null && hitEsquerda.collider == null)
        {
            // Ambas livres, escolher a que vai mais na direção do player
            return Vector2.Dot(direita, (Vector2)player.position - (Vector2)transform.position) > 0 ? direita : esquerda;
        }
        else if (hitDireita.collider == null)
        {
            return direita;
        }
        else
        {
            return esquerda;
        }
    }

    float CalcularPontuacaoDirecao(Vector2 direcaoTeste, Vector2 direcaoDesejada)
    {
        float pontuacao = 0f;

        // Pontuar baseado na similaridade com a direção desejada
        pontuacao += Vector2.Dot(direcaoTeste, direcaoDesejada.normalized) * 2f;

        // Pontuar baseado na distância até o player
        Vector2 posicaoAlvo = (Vector2)transform.position + direcaoTeste * distanciaMaximaRaycast;
        float distanciaPlayer = Vector2.Distance(posicaoAlvo, player.position);
        pontuacao += (1f / (distanciaPlayer + 0.1f)) * 1.5f;

        // Verificar se há obstáculos nesta direção
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direcaoTeste, distanciaMaximaRaycast, camadaObstaculos);
        if (hit.collider != null)
        {
            pontuacao -= 3f;
        }

        return pontuacao;
    }

    void SeguirCaminho()
    {
        if (caminhoAtual.Count == 0 || indiceWaypointAtual >= caminhoAtual.Count) return;

        Vector2 waypointAtual = caminhoAtual[indiceWaypointAtual];
        Vector2 direcao = (waypointAtual - (Vector2)transform.position).normalized;

        // Mover em direção ao waypoint
        rb.linearVelocity = direcao * stats_Inimigo.Speed;

        // Verificar se chegou ao waypoint
        if (Vector2.Distance(transform.position, waypointAtual) < 0.5f)
        {
            indiceWaypointAtual++;

            // Se chegou ao final do caminho, recalcular se necessário
            if (indiceWaypointAtual >= caminhoAtual.Count &&
                Vector2.Distance(transform.position, player.position) > distanciaParada)
            {
                CalcularNovoCaminho();
            }
        }

        // Rotacionar na direção do movimento
        if (rb.linearVelocity != Vector2.zero)
        {
            float angulo = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angulo, Vector3.forward);
        }
    }

    Vector2 RotateVector(Vector2 vector, float anguloGraus)
    {
        float anguloRad = anguloGraus * Mathf.Deg2Rad;
        float cos = Mathf.Cos(anguloRad);
        float sin = Mathf.Sin(anguloRad);
        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
    }

    void OnDrawGizmosSelected()
    {
        if (!mostrarDebug) return;

        // Desenhar raio de detecção
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, raioDetecao);

        // Desenhar raios de teste
        Gizmos.color = corRaios;
        for (int i = 0; i < numeroRaios; i++)
        {
            float angulo = (i / (float)numeroRaios) * 360f;
            Vector2 direcao = RotateVector(Vector2.right, angulo);
            Gizmos.DrawRay(transform.position, direcao * distanciaMaximaRaycast);
        }

        // Desenhar caminho atual
        if (caminhoAtual.Count > 0)
        {
            Gizmos.color = corCaminho;
            Vector2 pontoAnterior = transform.position;

            for (int i = 0; i < caminhoAtual.Count; i++)
            {
                Gizmos.DrawSphere(caminhoAtual[i], 0.2f);
                Gizmos.DrawLine(pontoAnterior, caminhoAtual[i]);
                pontoAnterior = caminhoAtual[i];

                // Desenhar linha até o player se for o último ponto
                if (i == caminhoAtual.Count - 1 && player != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(caminhoAtual[i], player.position);
                }
            }
        }

        // Desenhar direção atual do movimento
        if (Application.isPlaying && rb != null && rb.linearVelocity != Vector2.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 2f);
        }
    }
}