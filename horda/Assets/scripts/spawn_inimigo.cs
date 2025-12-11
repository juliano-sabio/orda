using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnerCompleto : MonoBehaviour
{
    [System.Serializable]
    public class TipoInimigo
    {
        public GameObject prefab;
        public string nome;
        public int tempoParaAparecer = 0;
        public float peso = 1f;
    }

    [System.Serializable]
    public class Wave
    {
        public string nome;
        public float duracao = 30f;
        public int maxInimigos = 20;
        public float intervaloSpaw = 2f;
        public bool waveEspecial = false;
        public float multiplicadorDificuldade = 1f;
    }

    [Header("REFERÊNCIAS")]
    public Transform player;

    [Header("LISTA DE INIMIGOS")]
    public List<TipoInimigo> tiposInimigos = new List<TipoInimigo>()
    {
        new TipoInimigo { nome = "Zumbi Basico", tempoParaAparecer = 0, peso = 3f },
        new TipoInimigo { nome = "Corredor", tempoParaAparecer = 30, peso = 2f },
        new TipoInimigo { nome = "Tanque", tempoParaAparecer = 60, peso = 1f }
    };

    [Header("SISTEMA DE WAVES")]
    public List<Wave> waves = new List<Wave>()
    {
        new Wave { nome = "Wave 1", duracao = 30f, maxInimigos = 15, intervaloSpaw = 3f, multiplicadorDificuldade = 1f },
        new Wave { nome = "Wave 2", duracao = 45f, maxInimigos = 25, intervaloSpaw = 2f, multiplicadorDificuldade = 1.2f },
        new Wave { nome = "Wave 3", duracao = 60f, maxInimigos = 35, intervaloSpaw = 1.5f, multiplicadorDificuldade = 1.5f }
    };
    public bool usarWaves = true;
    public float tempoEntreWaves = 5f;

    [Header("CONFIGURAÇÕES DE SPAWN")]
    [SerializeField] private float tempoEntreSpawns = 2f;
    [SerializeField] private float distanciaMinima = 5f;
    [SerializeField] private float distanciaMaxima = 10f;
    [SerializeField] private int limiteInimigos = 20;

    [Header("LIMITE DE ÁREA DE ATUAÇÃO")]
    public bool limitarAreaAtuacao = false;
    public Vector2 centroArea = Vector2.zero;
    public Vector2 tamanhoArea = new Vector2(20f, 20f);
    public bool manterInimigosDentroArea = true;

    [Header("EVOLUÇÃO DO JOGO")]
    [SerializeField] private bool aumentaDificuldade = true;
    [SerializeField] private float tempoParaAumentarDificuldade = 30f;
    [SerializeField] private float reducaoTempoSpawn = 0.1f;

    [Header("SPAWN EM GRUPO")]
    public bool spawnEmGrupo = false;
    public int tamanhoGrupoMin = 2;
    public int tamanhoGrupoMax = 5;
    public float intervaloEntreGrupos = 5f;

    // Variáveis privadas - SIMPLIFICADO
    private float tempoDesdeUltimoSpawn = 0f;
    private float tempoTotalJogo = 0f;
    private float tempoDesdeUltimoGrupo = 0f;
    private List<GameObject> inimigosAtivos = new List<GameObject>();
    private List<TipoInimigo> inimigosDisponiveis = new List<TipoInimigo>();

    // Variáveis de wave - SIMPLIFICADO
    private int waveAtualIndex = 0;
    private float tempoWaveAtual = 0f;
    private bool waveAtiva = false;
    private bool esperandoProximaWave = false;

    // SIMPLIFICAÇÃO: Usar apenas tempoTotalJogo para tudo
    private float tempoInicioWave = 0f;

    // Eventos
    public System.Action<int, string> OnWaveIniciada;
    public System.Action<int> OnWaveTerminada;

    void Start()
    {
        Debug.Log("=== INICIANDO SPAWNER SIMPLIFICADO ===");

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        VerificarPrefabs();

        if (usarWaves && waves.Count > 0)
        {
            waveAtualIndex = 0;
            IniciarWave(0);
        }
        else
        {
            AtualizarInimigosDisponiveis();
            Debug.Log("🔁 Modo sem waves ativado");
        }

        StartCoroutine(LimparListaInimigos());

        // DEBUG INICIAL
        Debug.Log("=== CONFIGURAÇÃO INICIAL ===");
        foreach (var inimigo in tiposInimigos)
        {
            Debug.Log($"- {inimigo.nome}: Aparece em {inimigo.tempoParaAparecer}s, Peso: {inimigo.peso}");
        }
    }

    void Update()
    {
        if (player == null) return;

        tempoTotalJogo += Time.deltaTime;
        tempoDesdeUltimoSpawn += Time.deltaTime;

        // ATUALIZAÇÃO SIMPLES: Sempre usa tempoTotalJogo
        AtualizarInimigosDisponiveis();

        if (usarWaves)
        {
            GerenciarWaves();
        }
        else
        {
            if (PodeSpawnarInimigo())
            {
                if (spawnEmGrupo && PodeSpawnarGrupo())
                {
                    SpawnarGrupo();
                    tempoDesdeUltimoGrupo = 0f;
                }
                else
                {
                    SpawnarInimigo();
                    tempoDesdeUltimoSpawn = 0f;
                }
            }
        }

        tempoDesdeUltimoGrupo += Time.deltaTime;
    }

    void GerenciarWaves()
    {
        if (waveAtiva)
        {
            tempoWaveAtual += Time.deltaTime;

            if (tempoDesdeUltimoSpawn >= tempoEntreSpawns && inimigosDisponiveis.Count > 0)
            {
                if (spawnEmGrupo && PodeSpawnarGrupo())
                {
                    SpawnarGrupo();
                    tempoDesdeUltimoGrupo = 0f;
                }
                else
                {
                    SpawnarInimigo();
                }
                tempoDesdeUltimoSpawn = 0f;
            }

            if (tempoWaveAtual >= waves[waveAtualIndex].duracao)
            {
                TerminarWave();
            }
        }
        else if (esperandoProximaWave)
        {
            tempoWaveAtual += Time.deltaTime;
            if (tempoWaveAtual >= tempoEntreWaves)
            {
                waveAtualIndex++;
                if (waveAtualIndex < waves.Count)
                {
                    IniciarWave(waveAtualIndex);
                }
                else
                {
                    CriarWaveProcedural();
                    IniciarWave(waveAtualIndex);
                }
            }
        }
    }

    void IniciarWave(int index)
    {
        if (index < 0 || index >= waves.Count)
        {
            Debug.LogError($"Índice de wave inválido: {index}");
            return;
        }

        waveAtiva = true;
        esperandoProximaWave = false;
        tempoWaveAtual = 0f;

        Wave wave = waves[index];
        tempoEntreSpawns = wave.intervaloSpaw;
        limiteInimigos = wave.maxInimigos;
        tempoInicioWave = tempoTotalJogo;

        Debug.Log($"=== INICIANDO WAVE {index + 1} ===");
        Debug.Log($"📊 Nome: {wave.nome}");
        Debug.Log($"⏱️ Duração: {wave.duracao}s");
        Debug.Log($"🎯 Max Inimigos: {wave.maxInimigos}");
        Debug.Log($"⚡ Intervalo Spawn: {wave.intervaloSpaw}s");

        OnWaveIniciada?.Invoke(index, wave.nome);
    }

    void TerminarWave()
    {
        waveAtiva = false;
        esperandoProximaWave = true;
        tempoWaveAtual = 0f;

        Debug.Log($"✅ WAVE {waveAtualIndex + 1} CONCLUÍDA!");
        Debug.Log($"⏰ Tempo total de jogo: {tempoTotalJogo:F1}s");

        OnWaveTerminada?.Invoke(waveAtualIndex);
    }

    void CriarWaveProcedural()
    {
        Wave ultimaWave = waves[waves.Count - 1];
        Wave novaWave = new Wave
        {
            nome = $"Wave {waves.Count + 1}",
            duracao = ultimaWave.duracao + 15f,
            maxInimigos = ultimaWave.maxInimigos + 10,
            intervaloSpaw = Mathf.Max(0.5f, ultimaWave.intervaloSpaw - 0.1f),
            multiplicadorDificuldade = ultimaWave.multiplicadorDificuldade + 0.15f
        };
        waves.Add(novaWave);
    }

    void VerificarPrefabs()
    {
        for (int i = tiposInimigos.Count - 1; i >= 0; i--)
        {
            if (tiposInimigos[i].prefab == null)
            {
                Debug.LogWarning($"Removendo {tiposInimigos[i].nome} (prefab nulo)");
                tiposInimigos.RemoveAt(i);
            }
        }
    }

    void AtualizarInimigosDisponiveis()
    {
        // ✅ SIMPLIFICAÇÃO TOTAL: Usa apenas tempoTotalJogo
        float tempoAtual = tempoTotalJogo;

        inimigosDisponiveis.Clear();

        foreach (TipoInimigo inimigo in tiposInimigos)
        {
            // ✅ VERIFICAÇÃO SIMPLES: tempoTotalJogo >= tempoParaAparecer
            if (tempoAtual >= inimigo.tempoParaAparecer)
            {
                inimigosDisponiveis.Add(inimigo);
            }
        }

        // DEBUG a cada 10 segundos
        if (Time.time % 10f < 0.1f)
        {
            Debug.Log($"=== DISPONÍVEIS em {tempoAtual:F1}s ===");
            foreach (var inimigo in inimigosDisponiveis)
            {
                Debug.Log($"✅ {inimigo.nome} (Aparece em: {inimigo.tempoParaAparecer}s)");
            }
            if (inimigosDisponiveis.Count == 0)
            {
                Debug.LogWarning("⚠️ NENHUM inimigo disponível!");
            }
        }
    }

    void SpawnarInimigo()
    {
        if (inimigosDisponiveis.Count == 0)
        {
            Debug.LogError("Tentando spawnar sem inimigos disponíveis!");
            return;
        }

        TipoInimigo tipoEscolhido = EscolherTipoInimigoPorPeso();

        if (tipoEscolhido == null || tipoEscolhido.prefab == null)
        {
            Debug.LogError("Tipo de inimigo inválido!");
            return;
        }

        Vector2 posicao = CalcularPosicaoSpawn();
        GameObject novoInimigo = Instantiate(tipoEscolhido.prefab, posicao, Quaternion.identity);

        if (novoInimigo != null)
        {
            inimigosAtivos.Add(novoInimigo);

            // DEBUG do spawn
            Debug.Log($"🎯 SPAWN: {tipoEscolhido.nome} em {tempoTotalJogo:F1}s");
        }
    }

    void SpawnarGrupo()
    {
        int tamanhoGrupo = Random.Range(tamanhoGrupoMin, tamanhoGrupoMax + 1);
        Vector2 posicaoBase = CalcularPosicaoSpawn();

        for (int i = 0; i < tamanhoGrupo; i++)
        {
            if (inimigosAtivos.Count >= limiteInimigos) break;

            Vector2 offset = new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f));
            Vector2 posicaoSpawn = posicaoBase + offset;

            TipoInimigo tipoEscolhido = EscolherTipoInimigoPorPeso();

            if (tipoEscolhido != null && tipoEscolhido.prefab != null)
            {
                GameObject novoInimigo = Instantiate(tipoEscolhido.prefab, posicaoSpawn, Quaternion.identity);
                if (novoInimigo != null)
                {
                    inimigosAtivos.Add(novoInimigo);
                    Debug.Log($"👥 Spawn em grupo: {tipoEscolhido.nome}");
                }
            }
        }
    }

    TipoInimigo EscolherTipoInimigoPorPeso()
    {
        if (inimigosDisponiveis.Count == 0) return null;

        // Calcular peso total
        float pesoTotal = 0f;
        foreach (var inimigo in inimigosDisponiveis)
        {
            pesoTotal += inimigo.peso;
        }

        // Escolher baseado no peso
        float random = Random.Range(0f, pesoTotal);
        float acumulado = 0f;

        foreach (var inimigo in inimigosDisponiveis)
        {
            acumulado += inimigo.peso;
            if (random <= acumulado)
            {
                return inimigo;
            }
        }

        return inimigosDisponiveis[0];
    }

    Vector2 CalcularPosicaoSpawn()
    {
        if (limitarAreaAtuacao)
        {
            Vector2 min = centroArea - tamanhoArea / 2f;
            Vector2 max = centroArea + tamanhoArea / 2f;
            return new Vector2(Random.Range(min.x, max.x), Random.Range(min.y, max.y));
        }
        else
        {
            float angulo = Random.Range(0f, 360f);
            Vector2 direcao = new Vector2(Mathf.Cos(angulo * Mathf.Deg2Rad), Mathf.Sin(angulo * Mathf.Deg2Rad));
            float distancia = Random.Range(distanciaMinima, distanciaMaxima);
            return (Vector2)player.position + direcao * distancia;
        }
    }

    bool PodeSpawnarInimigo()
    {
        return tempoDesdeUltimoSpawn >= tempoEntreSpawns &&
               inimigosAtivos.Count < limiteInimigos &&
               inimigosDisponiveis.Count > 0;
    }

    bool PodeSpawnarGrupo()
    {
        return tempoDesdeUltimoGrupo >= intervaloEntreGrupos &&
               inimigosAtivos.Count < limiteInimigos - tamanhoGrupoMin;
    }

    IEnumerator LimparListaInimigos()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f);
            inimigosAtivos.RemoveAll(inimigo => inimigo == null);
        }
    }

    // Métodos públicos para UI
    public int GetWaveAtualIndex() => waveAtualIndex;
    public string GetNomeWaveAtual() => waveAtualIndex < waves.Count ? waves[waveAtualIndex].nome : "Nenhuma";
    public int GetInimigosAtivosCount() => inimigosAtivos.Count;
    public float GetTempoTotalJogo() => tempoTotalJogo;
    public bool IsWaveAtiva() => waveAtiva;
}