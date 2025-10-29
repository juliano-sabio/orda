using UnityEngine;
using System.Collections;

public class movi_inimigo : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    public float velocidade = 3f;
    public float distanciaDetecção = 8f;
    public float distanciaAtaque = 2f;
    public float intervaloAtualizaçãoCaminho = 0.5f;

    [Header("Referências")]
    public Transform player;
    public Transform[] pontosCaminho;

    private Rigidbody2D rb;
    private Vector2 direcaoMovimento;
    private int pontoAtual = 0;
    private float tempoUltimaAtualização;
    private bool perseguindoPlayer = false;
    private bool procurandoPlayer = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // 🔥 ENCONTRA O PLAYER AUTOMATICAMENTE
        EncontrarPlayer();

        // Verifica se tem pontos de caminho
        if (pontosCaminho != null && pontosCaminho.Length > 0)
        {
            Debug.Log($"🛣️ {pontosCaminho.Length} pontos de caminho configurados");
        }
        else
        {
            Debug.LogWarning("⚠️ Nenhum ponto de caminho configurado. Inimigo ficará parado.");
        }

        tempoUltimaAtualização = Time.time;
        
        // Começa a corrotina para procurar player periodicamente
        StartCoroutine(ProcurarPlayerPeriodicamente());
    }

    void Update()
    {
        // 🔥 SE NÃO TEM PLAYER, TENTA ENCONTRAR NOVAMENTE
        if (player == null && !procurandoPlayer)
        {
            EncontrarPlayer();
            return;
        }

        if (player == null) return;

        // Verifica se deve perseguir o player
        VerificarPlayer();

        // Atualiza o caminho periodicamente
        if (Time.time - tempoUltimaAtualização >= intervaloAtualizaçãoCaminho)
        {
            AtualizarCaminho();
            tempoUltimaAtualização = Time.time;
        }

        // Move o inimigo
        Mover();
    }

    // 🔥 MÉTODO MELHORADO PARA ENCONTRAR PLAYER
    void EncontrarPlayer()
    {
        procurandoPlayer = true;
        
        // Tenta encontrar por tag primeiro
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log($"🎯 Player encontrado por tag: {player.name}");
            procurandoPlayer = false;
            return;
        }

        // Se não encontrou por tag, tenta encontrar por nome
        playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log($"🎯 Player encontrado por nome: {player.name}");
            procurandoPlayer = false;
            return;
        }

        // Se ainda não encontrou, tenta encontrar qualquer objeto com componente Player
       // MonoBehaviour[] todosOsObjetos = FindObjectOfType<MonoBehaviour>();
       // foreach (MonoBehaviour obj in todosOsObjetos)
       // {
        //    if (obj.GetType().Name.ToLower().Contains("player"))
        //    {
        //        player = obj.transform;
          //      Debug.Log($"🎯 Player encontrado por componente: {player.name}");
        //        procurandoPlayer = false;
        //        return;
         //   }
       // }

        // Se não encontrou de jeito nenhum
        Debug.LogWarning("⚠️ Player não encontrado automaticamente. Tentando novamente...");
        procurandoPlayer = false;
    }

    // 🔥 CORROTINA PARA PROCURAR PLAYER PERIODICAMENTE
    IEnumerator ProcurarPlayerPeriodicamente()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f); // Procura a cada 3 segundos
            
            if (player == null)
            {
                Debug.Log("🔍 Procurando player periodicamente...");
                EncontrarPlayer();
            }
            else
            {
                // Verifica se o player ainda existe na cena
                if (player.gameObject.scene.IsValid() == false)
                {
                    Debug.LogWarning("🚨 Player foi destruído! Procurando novo...");
                    player = null;
                    EncontrarPlayer();
                }
            }
        }
    }

    void VerificarPlayer()
    {
        if (player == null) return;

        float distanciaParaPlayer = Vector2.Distance(transform.position, player.position);

        if (distanciaParaPlayer <= distanciaDetecção)
        {
            perseguindoPlayer = true;
        }
        else if (distanciaParaPlayer > distanciaDetecção * 1.5f)
        {
            perseguindoPlayer = false;
        }
    }

    void AtualizarCaminho()
    {
        if (perseguindoPlayer && player != null)
        {
            // Persegue o player
            direcaoMovimento = (player.position - transform.position).normalized;
        }
        else
        {
            // Segue os pontos de caminho
            SeguirCaminho();
        }
    }

    void SeguirCaminho()
    {
        // ⭐⭐ CORREÇÃO: Verifica se há pontos de caminho antes de acessar
        if (pontosCaminho == null || pontosCaminho.Length == 0)
        {
            // Se não há pontos de caminho, fica parado
            direcaoMovimento = Vector2.zero;
            return;
        }

        // Verifica se o ponto atual é válido
        if (pontoAtual >= pontosCaminho.Length || pontosCaminho[pontoAtual] == null)
        {
            pontoAtual = 0; // Volta para o primeiro ponto
            if (pontosCaminho.Length == 0 || pontosCaminho[0] == null)
            {
                direcaoMovimento = Vector2.zero;
                return;
            }
        }

        // Move em direção ao ponto atual
        Vector2 direcao = (pontosCaminho[pontoAtual].position - transform.position).normalized;
        direcaoMovimento = direcao;

        // Verifica se chegou perto o suficiente do ponto
        float distanciaParaPonto = Vector2.Distance(transform.position, pontosCaminho[pontoAtual].position);

        if (distanciaParaPonto < 0.5f)
        {
            pontoAtual++;
            if (pontoAtual >= pontosCaminho.Length)
            {
                pontoAtual = 0; // Volta para o primeiro ponto (loop)
            }
        }
    }

    void Mover()
    {
        if (direcaoMovimento != Vector2.zero)
        {
            // Aplica movimento
            rb.linearVelocity = direcaoMovimento * velocidade;

            // Rotaciona na direção do movimento (opcional)
            if (direcaoMovimento.x != 0)
            {
                float escalaX = Mathf.Abs(transform.localScale.x);
                transform.localScale = new Vector3(
                    direcaoMovimento.x < 0? escalaX : -escalaX,
                    transform.localScale.y,
                    transform.localScale.z
                );
            }
        }
        else
        {
            // Para o movimento se não há direção
            rb.linearVelocity = Vector2.zero;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Para de se mover temporariamente ao entrar em contato com o player
        if (other.CompareTag("Player"))
        {
            StartCoroutine(PararMovimentoTemporariamente());
        }
    }

    IEnumerator PararMovimentoTemporariamente()
    {
        Vector2 velocidadeOriginal = rb.linearVelocity;
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(0.5f);

        // Restaura movimento se ainda estiver vivo
        if (rb != null)
        {
            rb.linearVelocity = velocidadeOriginal;
        }
    }

    // 🔥 MÉTODO MELHORADO PARA CONFIGURAR PLAYER
    public void ConfigurarPlayer(Transform novoPlayer)
    {
        if (novoPlayer != null)
        {
            player = novoPlayer;
            Debug.Log($"🎯 Player configurado manualmente: {player.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ Tentativa de configurar player nulo!");
            EncontrarPlayer(); // Tenta encontrar automaticamente
        }
    }

    // Método para configurar pontos de caminho dinamicamente
    public void ConfigurarPontosCaminho(Transform[] novosPontos)
    {
        pontosCaminho = novosPontos;
        pontoAtual = 0;

        if (pontosCaminho != null && pontosCaminho.Length > 0)
        {
            Debug.Log($"🛣️ {pontosCaminho.Length} novos pontos de caminho configurados");
        }
    }

    // 🔥 MÉTODO PARA FORÇAR BUSCA IMEDIATA DO PLAYER
    [ContextMenu("Forçar Busca do Player")]
    public void ForcarBuscaPlayer()
    {
        Debug.Log("🔍 Forçando busca imediata do player...");
        EncontrarPlayer();
    }

    // Método para forçar perseguição
    public void ForcarPerseguicao(bool perseguir)
    {
        perseguindoPlayer = perseguir;
    }

    // 🔥 MÉTODO MELHORADO PARA OBTER ESTADO ATUAL
    public string GetEstadoAtual()
    {
        if (player == null)
            return "❌ SEM PLAYER - Procurando...";
        else if (perseguindoPlayer)
            return "🎯 Perseguindo Player";
        else if (pontosCaminho != null && pontosCaminho.Length > 0)
            return $"🛣️ Seguindo Caminho (Ponto {pontoAtual + 1}/{pontosCaminho.Length})";
        else
            return "⏸️ Parado";
    }

    // 🔥 MÉTODO PARA DEBUG NO CONSOLE
    [ContextMenu("Debug Info")]
    public void DebugInfo()
    {
        Debug.Log($"=== DEBUG INIMIGO {name} ===");
        Debug.Log($"Estado: {GetEstadoAtual()}");
        Debug.Log($"Player: {(player != null ? player.name : "NULO")}");
        Debug.Log($"Posição: {transform.position}");
        Debug.Log($"Velocidade: {rb.linearVelocity.magnitude:F1}");
        Debug.Log($"Perseguindo: {perseguindoPlayer}");
    }

    // Método para debug visual
    void OnDrawGizmosSelected()
    {
        // Área de detecção
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaDetecção);

        // Área de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaAtaque);

        // Direção do movimento atual
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, direcaoMovimento * 2f);

        // Pontos de caminho
        if (pontosCaminho != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < pontosCaminho.Length; i++)
            {
                if (pontosCaminho[i] != null)
                {
                    Gizmos.DrawWireSphere(pontosCaminho[i].position, 0.3f);
                    if (i < pontosCaminho.Length - 1 && pontosCaminho[i + 1] != null)
                    {
                        Gizmos.DrawLine(pontosCaminho[i].position, pontosCaminho[i + 1].position);
                    }
                }
            }
        }

        // 🔥 LINHA ATÉ O PLAYER (se existir)
        if (player != null)
        {
            Gizmos.color = perseguindoPlayer ? Color.red : Color.white;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}