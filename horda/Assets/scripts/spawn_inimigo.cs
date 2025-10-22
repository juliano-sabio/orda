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
        public status_inimigo status;
    }

    [Header("REFERÊNCIAS")]
    public Transform player;

    [Header("LISTA DE INIMIGOS - ARRASTE OS PREFABS AQUI")]
    public List<TipoInimigo> tiposInimigos = new List<TipoInimigo>()
    {
        new TipoInimigo { nome = "Zumbi Basico", tempoParaAparecer = 0, peso = 3f },
        new TipoInimigo { nome = "Corredor", tempoParaAparecer = 30, peso = 2f },
        new TipoInimigo { nome = "Tanque", tempoParaAparecer = 60, peso = 1f }
    };

    [Header("CONFIGURAÇÕES DE SPAWN")]
    [SerializeField] private float tempoEntreSpawns = 2f;
    [SerializeField] private float distanciaMinima = 5f;
    [SerializeField] private float distanciaMaxima = 10f;
    [SerializeField] private int limiteInimigos = 20;

    [Header("LIMITES DO MUNDO - CONFIGURE AQUI")]
    [SerializeField] private bool usarLimitesMundo = true;
    [SerializeField] private Vector2 limiteMin = new Vector2(-50, -50);
    [SerializeField] private Vector2 limiteMax = new Vector2(50, 50);
    [SerializeField] private int maxTentativasSpawn = 10;

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

        VerificarPrefabs();
        AtualizarInimigosDisponiveis();
        StartCoroutine(LimparListaInimigos());

        Debug.Log($"Spawner pronto! {tiposInimigos.Count} tipos de inimigos configurados.");

        if (usarLimitesMundo)
        {
            Debug.Log($"🌍 Limites do mundo: {limiteMin} até {limiteMax}");
        }
    }

    void Update()
    {
        if (player == null) return;

        tempoDesdeUltimoSpawn += Time.deltaTime;
        tempoTotalJogo += Time.deltaTime;

        AtualizarInimigosDisponiveis();

        if (PodeSpawnarInimigo())
        {
            SpawnarInimigo();
            tempoDesdeUltimoSpawn = 0f;
        }

        if (aumentaDificuldade && tempoTotalJogo >= tempoParaAumentarDificuldade)
        {
            AumentarDificuldade();
            tempoTotalJogo = 0f;
        }
    }

    void VerificarPrefabs()
    {
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
        TipoInimigo inimigoEscolhido = EscolherInimigoPorPeso();

        if (inimigoEscolhido != null && inimigoEscolhido.prefab != null)
        {
            // ⭐⭐ NOVO: Sistema de spawn com verificação de limites
            Vector2? posicaoSpawn = CalcularPosicaoSpawnValida();

            if (posicaoSpawn.HasValue)
            {
                GameObject novoInimigo = Instantiate(inimigoEscolhido.prefab, posicaoSpawn.Value, Quaternion.identity);
                ConfigurarInimigoSpawnado(novoInimigo, inimigoEscolhido);
                inimigosAtivos.Add(novoInimigo);

                if (Time.time % 10f < 0.1f)
                {
                    Debug.Log($"Spawn: {novoInimigo.name} | Pos: {posicaoSpawn.Value} | Total: {inimigosAtivos.Count}");
                }
            }
            else
            {
                Debug.LogWarning("Não foi possível encontrar posição de spawn válida!");
            }
        }
    }

    // ⭐⭐ NOVO MÉTODO: Calcula posição de spawn válida dentro dos limites
    Vector2? CalcularPosicaoSpawnValida()
    {
        if (!usarLimitesMundo)
        {
            return CalcularPosicaoSpawn(); // Volta ao método antigo
        }

        for (int tentativa = 0; tentativa < maxTentativasSpawn; tentativa++)
        {
            Vector2 posicaoTentativa = CalcularPosicaoSpawn();

            if (PosicaoDentroDosLimites(posicaoTentativa))
            {
                return posicaoTentativa;
            }
        }

        // ⭐ FALLBACK: Tenta posições mais próximas do player
        for (int tentativa = 0; tentativa < 5; tentativa++)
        {
            Vector2 posicaoTentativa = CalcularPosicaoSpawnMaisProxima();

            if (PosicaoDentroDosLimites(posicaoTentativa))
            {
                Debug.Log($"Usando posição fallback (tentativa {tentativa + 1})");
                return posicaoTentativa;
            }
        }

        return null; // Não encontrou posição válida
    }

    // ⭐ NOVO MÉTODO: Verifica se posição está dentro dos limites
    bool PosicaoDentroDosLimites(Vector2 posicao)
    {
        return posicao.x >= limiteMin.x && posicao.x <= limiteMax.x &&
               posicao.y >= limiteMin.y && posicao.y <= limiteMax.y;
    }

    // ⭐ NOVO MÉTODO: Calcula posição mais próxima do player (fallback)
    Vector2 CalcularPosicaoSpawnMaisProxima()
    {
        float angulo = Random.Range(0f, 360f);
        Vector2 direcao = new Vector2(
            Mathf.Cos(angulo * Mathf.Deg2Rad),
            Mathf.Sin(angulo * Mathf.Deg2Rad)
        );

        // Usa distância menor para fallback
        float distancia = Random.Range(distanciaMinima, distanciaMinima + 2f);

        return (Vector2)player.position + direcao * distancia;
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

    void ConfigurarInimigoSpawnado(GameObject inimigo, TipoInimigo tipo)
    {
        EnemyDamage enemyDamage = inimigo.GetComponent<EnemyDamage>();
        if (enemyDamage == null) return;

        if (tipo.status != null)
        {
            enemyDamage.enemyStatus = tipo.status;
        }
        else
        {
            // Fallback automático (mantive do código anterior)
            status_inimigo statusAuto = Resources.Load<status_inimigo>($"Inimigos/Status/{tipo.nome}");
            if (statusAuto != null)
            {
                enemyDamage.enemyStatus = statusAuto;
            }
        }
    }

    TipoInimigo EscolherInimigoPorPeso()
    {
        if (inimigosDisponiveis.Count == 0) return null;

        float pesoTotal = 0f;
        foreach (TipoInimigo inimigo in inimigosDisponiveis)
        {
            if (inimigo.prefab != null)
                pesoTotal += inimigo.peso;
        }

        if (pesoTotal <= 0) return null;

        float valorAleatorio = Random.Range(0f, pesoTotal);
        float pesoAcumulado = 0f;

        foreach (TipoInimigo inimigo in inimigosDisponiveis)
        {
            if (inimigo.prefab == null) continue;

            pesoAcumulado += inimigo.peso;
            if (valorAleatorio <= pesoAcumulado)
            {
                return inimigo;
            }
        }

        return inimigosDisponiveis[0];
    }

    void AumentarDificuldade()
    {
        float novoTempoSpawn = tempoEntreSpawns - reducaoTempoSpawn;
        tempoEntreSpawns = Mathf.Max(0.3f, novoTempoSpawn);
        limiteInimigos += 2;

        Debug.Log($"📈 Dificuldade Aumentada! " +
                 $"Spawn: {tempoEntreSpawns:F1}s | " +
                 $"Limite: {limiteInimigos}");
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

    // === NOVOS MÉTODOS PARA CONFIGURAR LIMITES ===

    public void DefinirLimitesMundo(Vector2 min, Vector2 max)
    {
        limiteMin = min;
        limiteMax = max;
        usarLimitesMundo = true;
        Debug.Log($"🌍 Limites do mundo definidos: {limiteMin} até {limiteMax}");
    }

    public void DesativarLimitesMundo()
    {
        usarLimitesMundo = false;
        Debug.Log("🌍 Limites do mundo desativados");
    }

    // === MÉTODOS DE DEBUG ===

    [ContextMenu("Debug - Mostrar Info")]
    public void MostrarInformacoes()
    {
        Debug.Log("=== INFO SPAWNER ===");
        Debug.Log($"Tempo Total: {tempoTotalJogo:F0}s");
        Debug.Log($"Inimigos Ativos: {inimigosAtivos.Count}");
        Debug.Log($"Inimigos Disponíveis: {inimigosDisponiveis.Count}");
        Debug.Log($"Usando Limites: {usarLimitesMundo}");

        if (usarLimitesMundo)
        {
            Debug.Log($"Limites: {limiteMin} até {limiteMax}");
        }
    }

    [ContextMenu("Debug - Testar Spawn com Limites")]
    public void TestarSpawnComLimites()
    {
        Vector2? posicaoTeste = CalcularPosicaoSpawnValida();
        if (posicaoTeste.HasValue)
        {
            Debug.Log($"✅ Posição de spawn válida: {posicaoTeste.Value}");
        }
        else
        {
            Debug.LogWarning("❌ Não foi encontrar posição de spawn válida!");
        }
    }

    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            // Área de spawn original (amarelo)
            Gizmos.color = new Color(1, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(player.position, distanciaMinima);
            Gizmos.DrawWireSphere(player.position, distanciaMaxima);

            // ⭐⭐ NOVO: Limites do mundo (vermelho)
            if (usarLimitesMundo)
            {
                Gizmos.color = new Color(1, 0, 0, 0.3f);
                Vector3 centro = new Vector3((limiteMin.x + limiteMax.x) * 0.5f, (limiteMin.y + limiteMax.y) * 0.5f, 0);
                Vector3 tamanho = new Vector3(limiteMax.x - limiteMin.x, limiteMax.y - limiteMin.y, 0.1f);
                Gizmos.DrawWireCube(centro, tamanho);

                // Área segura dentro dos limites (verde)
                Gizmos.color = new Color(0, 1, 0, 0.1f);
                Gizmos.DrawCube(centro, tamanho);
            }
        }
    }
}