using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnerCorrigido : MonoBehaviour
{
    [System.Serializable]
    public class TipoInimigo
    {
        public GameObject prefab;
        public string nome;
        public int tempoParaAparecer = 0;
        public float peso = 1f;
    }

    [Header("REFERÊNCIAS")]
    public Transform player;

    [Header("LISTA DE INIMIGOS - ARRASTE OS PREFABS AQUI")]
    public List<TipoInimigo> tiposInimigos = new List<TipoInimigo>()
    {
        // Exemplo com alguns inimigos pré-configurados
        new TipoInimigo { nome = "Zumbi Basico", tempoParaAparecer = 0, peso = 3f },
        new TipoInimigo { nome = "Corredor", tempoParaAparecer = 30, peso = 2f },
        new TipoInimigo { nome = "Tanque", tempoParaAparecer = 60, peso = 1f }
    };

    [Header("CONFIGURAÇÕES DE SPAWN")]
    [SerializeField] private float tempoEntreSpawns = 2f;
    [SerializeField] private float distanciaMinima = 5f;
    [SerializeField] private float distanciaMaxima = 10f;
    [SerializeField] private int limiteInimigos = 20;

    [Header("EVOLUÇÃO DO JOGO")]
    [SerializeField] private bool aumentaDificuldade = true;
    [SerializeField] private float tempoParaAumentarDificuldade = 30f;
    [SerializeField] private float reducaoTempoSpawn = 0.1f;

    // Variáveis privadas
    private float tempoDesdeUltimoSpawn = 0f;
    private float tempoTotalJogo = 0f;
    private List<GameObject> inimigosAtivos = new List<GameObject>();
    private List<TipoInimigo> inimigosDisponiveis = new List<TipoInimigo>();

    void Start()
    {
        Debug.Log("=== INICIANDO SPAWNER ===");

        // Encontra o player automaticamente
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("Player encontrado automaticamente!");
            }
            else
            {
                Debug.LogError("Player não encontrado! Certifique-se de ter um objeto com tag 'Player'");
            }
        }

        // Verifica se há prefabs atribuídos
        VerificarPrefabs();

        // Inicializa inimigos disponíveis
        AtualizarInimigosDisponiveis();

        // Começa corrotina de limpeza
        StartCoroutine(LimparListaInimigos());

        Debug.Log($"Spawner pronto! {tiposInimigos.Count} tipos de inimigos configurados.");
    }

    void Update()
    {
        if (player == null) return;

        tempoDesdeUltimoSpawn += Time.deltaTime;
        tempoTotalJogo += Time.deltaTime;

        // Atualiza lista de inimigos disponíveis
        AtualizarInimigosDisponiveis();

        // Spawn de inimigos
        if (PodeSpawnarInimigo())
        {
            SpawnarInimigo();
            tempoDesdeUltimoSpawn = 0f;
        }

        // Aumenta dificuldade
        if (aumentaDificuldade && tempoTotalJogo >= tempoParaAumentarDificuldade)
        {
            AumentarDificuldade();
            tempoTotalJogo = 0f;
        }
    }

    void VerificarPrefabs()
    {
        // Remove entradas que não têm prefab
        for (int i = tiposInimigos.Count - 1; i >= 0; i--)
        {
            if (tiposInimigos[i].prefab == null)
            {
                Debug.LogWarning($"Removendo inimigo sem prefab: {tiposInimigos[i].nome}");
                tiposInimigos.RemoveAt(i);
            }
        }

        if (tiposInimigos.Count == 0)
        {
            Debug.LogError("Nenhum prefab de inimigo foi atribuído! O spawner não funcionará.");
        }
    }

    void AtualizarInimigosDisponiveis()
    {
        inimigosDisponiveis.Clear();

        foreach (TipoInimigo inimigo in tiposInimigos)
        {
            if (tempoTotalJogo >= inimigo.tempoParaAparecer && inimigo.prefab != null)
            {
                inimigosDisponiveis.Add(inimigo);
            }
        }

        // Fallback: usa pelo menos o primeiro inimigo disponível
        if (inimigosDisponiveis.Count == 0 && tiposInimigos.Count > 0)
        {
            foreach (TipoInimigo inimigo in tiposInimigos)
            {
                if (inimigo.prefab != null)
                {
                    inimigosDisponiveis.Add(inimigo);
                    break;
                }
            }
        }
    }

    bool PodeSpawnarInimigo()
    {
        return tempoDesdeUltimoSpawn >= tempoEntreSpawns &&
               inimigosAtivos.Count < limiteInimigos &&
               inimigosDisponiveis.Count > 0;
    }

    void SpawnarInimigo()
    {
        GameObject inimigoEscolhido = EscolherInimigoPorPeso();

        if (inimigoEscolhido != null)
        {
            Vector2 posicaoSpawn = CalcularPosicaoSpawn();
            GameObject novoInimigo = Instantiate(inimigoEscolhido, posicaoSpawn, Quaternion.identity);
            inimigosAtivos.Add(novoInimigo);

            // Debug opcional
            if (Time.time % 10f < 0.1f) // A cada ~10 segundos
            {
                Debug.Log($"Spawn: {novoInimigo.name} | Tempo: {Time.time:F0}s | Total: {inimigosAtivos.Count}");
            }
        }
    }

    GameObject EscolherInimigoPorPeso()
    {
        if (inimigosDisponiveis.Count == 0) return null;

        // Calcula peso total
        float pesoTotal = 0f;
        foreach (TipoInimigo inimigo in inimigosDisponiveis)
        {
            if (inimigo.prefab != null)
                pesoTotal += inimigo.peso;
        }

        if (pesoTotal <= 0) return null;

        // Sistema de escolha por peso
        float valorAleatorio = Random.Range(0f, pesoTotal);
        float pesoAcumulado = 0f;

        foreach (TipoInimigo inimigo in inimigosDisponiveis)
        {
            if (inimigo.prefab == null) continue;

            pesoAcumulado += inimigo.peso;
            if (valorAleatorio <= pesoAcumulado)
            {
                return inimigo.prefab;
            }
        }

        return inimigosDisponiveis[0].prefab;
    }

    Vector2 CalcularPosicaoSpawn()
    {
        float angulo = Random.Range(0f, 360f);
        Vector2 direcao = new Vector2(
            Mathf.Cos(angulo * Mathf.Deg2Rad),
            Mathf.Sin(angulo * Mathf.Deg2Rad)
        );
        float distancia = Random.Range(distanciaMinima, distanciaMaxima);

        return (Vector2)player.position + direcao * distancia;
    }

    void AumentarDificuldade()
    {
        float novoTempoSpawn = tempoEntreSpawns - reducaoTempoSpawn;
        tempoEntreSpawns = Mathf.Max(0.3f, novoTempoSpawn);
        limiteInimigos += 2;

        Debug.Log($"📈 Dificuldade Aumentada! " +
                 $"Spawn: {tempoEntreSpawns:F1}s | " +
                 $"Limite: {limiteInimigos} | " +
                 $"Inimigos Disponíveis: {inimigosDisponiveis.Count}");
    }

    IEnumerator LimparListaInimigos()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f);
            int countAntes = inimigosAtivos.Count;
            inimigosAtivos.RemoveAll(inimigo => inimigo == null);

            if (countAntes != inimigosAtivos.Count)
            {
                Debug.Log($"🧹 Limpeza: {countAntes} → {inimigosAtivos.Count} inimigos");
            }
        }
    }

    // === MÉTODOS PÚBLICOS PARA FACILITAR ===

    public void AdicionarNovoInimigo(GameObject prefab, string nome, int tempoAparecer, float peso)
    {
        TipoInimigo novoInimigo = new TipoInimigo
        {
            prefab = prefab,
            nome = nome,
            tempoParaAparecer = tempoAparecer,
            peso = peso
        };

        tiposInimigos.Add(novoInimigo);
        Debug.Log($"Novo inimigo adicionado: {nome}");
    }

    public void ConfigurarSpawn(float novoTempoSpawn, int novoLimite)
    {
        tempoEntreSpawns = novoTempoSpawn;
        limiteInimigos = novoLimite;
    }

    // === MÉTODOS DE DEBUG ===

    [ContextMenu("Debug - Mostrar Info")]
    public void MostrarInformacoes()
    {
        Debug.Log("=== INFO SPAWNER ===");
        Debug.Log($"Tempo Total: {tempoTotalJogo:F0}s");
        Debug.Log($"Inimigos Ativos: {inimigosAtivos.Count}");
        Debug.Log($"Inimigos Disponíveis: {inimigosDisponiveis.Count}");
        Debug.Log($"Próximo Spawn em: {tempoEntreSpawns - tempoDesdeUltimoSpawn:F1}s");

        foreach (var inimigo in inimigosDisponiveis)
        {
            Debug.Log($"- {inimigo.nome} (Peso: {inimigo.peso})");
        }
    }

    [ContextMenu("Debug - Spawn Manual")]
    public void SpawnManual()
    {
        SpawnarInimigo();
        Debug.Log("Spawn manual executado!");
    }

    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            // Área de spawn
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireSphere(player.position, distanciaMinima);

            Gizmos.color = new Color(1, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(player.position, distanciaMaxima);
        }
    }
}