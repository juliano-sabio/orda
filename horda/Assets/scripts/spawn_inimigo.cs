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

    // Variáveis privadas
    private float tempoDesdeUltimoSpawn = 0f;
    private float tempoTotalJogo = 0f;
    private float tempoDesdeUltimoGrupo = 0f;
    private List<GameObject> inimigosAtivos = new List<GameObject>();
    private List<TipoInimigo> inimigosDisponiveis = new List<TipoInimigo>();

    // Variáveis de wave - ✅ CORRIGIDO: INICIALIZAÇÃO CORRETA
    private int waveAtualIndex = -1; // ✅ Começa em -1 para a primeira wave ser 0
    private float tempoWaveAtual = 0f;
    private bool waveAtiva = false;
    private bool esperandoProximaWave = false;

    // Eventos
    public System.Action<int, string> OnWaveIniciada;
    public System.Action<int> OnWaveTerminada;

    void Start()
    {
        Debug.Log("=== INICIANDO SPAWNER COMPLETO ===");

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

        if (centroArea == Vector2.zero)
        {
            centroArea = transform.position;
        }

        // ✅ CORREÇÃO: INICIALIZAÇÃO CORRETA DAS WAVES
        if (usarWaves && waves.Count > 0)
        {
            // Começa esperando a primeira wave
            esperandoProximaWave = true;
            waveAtiva = false;
            waveAtualIndex = -1;
            tempoWaveAtual = 0f;

            Debug.Log("⏳ Aguardando início da primeira wave...");
        }
        else
        {
            AtualizarInimigosDisponiveis();
            Debug.Log("🔁 Modo sem waves ativado");
        }

        StartCoroutine(LimparListaInimigos());

        if (limitarAreaAtuacao && manterInimigosDentroArea)
        {
            StartCoroutine(ManterInimigosNaArea());
        }

        Debug.Log($"Spawner pronto! {tiposInimigos.Count} tipos de inimigos configurados.");

        if (limitarAreaAtuacao)
        {
            Debug.Log($"📍 Área limitada: Centro {centroArea}, Tamanho {tamanhoArea}");
        }
    }

    void Update()
    {
        if (player == null) return;

        tempoTotalJogo += Time.deltaTime;
        tempoDesdeUltimoSpawn += Time.deltaTime;

        if (usarWaves)
        {
            GerenciarWaves();
        }
        else
        {
            AtualizarInimigosDisponiveis();

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

            if (aumentaDificuldade && tempoTotalJogo >= tempoParaAumentarDificuldade)
            {
                AumentarDificuldade();
                tempoTotalJogo = 0f;
            }
        }

        tempoDesdeUltimoGrupo += Time.deltaTime;
    }

    // ✅ CORREÇÃO COMPLETA DO SISTEMA DE WAVES
    void GerenciarWaves()
    {
        if (waveAtiva)
        {
            // WAVE ATIVA - spawna inimigos e conta o tempo
            tempoWaveAtual += Time.deltaTime;

            // Spawn durante a wave
            if (tempoDesdeUltimoSpawn >= tempoEntreSpawns)
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

            // Verifica se a wave terminou
            if (tempoWaveAtual >= waves[waveAtualIndex].duracao)
            {
                TerminarWave();
            }
        }
        else if (esperandoProximaWave)
        {
            // ESPERANDO PRÓXIMA WAVE - conta o tempo entre waves
            tempoWaveAtual += Time.deltaTime;

            if (tempoWaveAtual >= tempoEntreWaves)
            {
                IniciarProximaWave();
            }
        }
        else
        {
            // NENHUMA WAVE ATIVA - inicia a primeira wave
            IniciarProximaWave();
        }
    }

    // ✅ CORREÇÃO: INICIAR WAVE COM VERIFICAÇÕES
    void IniciarWave(int index)
    {
        if (index < 0 || index >= waves.Count)
        {
            Debug.LogError($"❌ Índice de wave inválido: {index}");
            return;
        }

        waveAtualIndex = index;
        waveAtiva = true;
        esperandoProximaWave = false;
        tempoWaveAtual = 0f;

        Wave wave = waves[waveAtualIndex];

        // ✅ CORREÇÃO: USA OS VALORES DA WAVE ATUAL
        tempoEntreSpawns = wave.intervaloSpaw;
        limiteInimigos = wave.maxInimigos;

        AtualizarInimigosDisponiveis();

        Debug.Log($"🚀 INICIANDO {wave.nome}!");
        Debug.Log($"⏱️ Duração: {wave.duracao}s");
        Debug.Log($"🎯 Inimigos: {wave.maxInimigos}");
        Debug.Log($"⚡ Spawn: {wave.intervaloSpaw}s");
        Debug.Log($"📈 Dificuldade: x{wave.multiplicadorDificuldade}");

        OnWaveIniciada?.Invoke(waveAtualIndex, wave.nome);
    }

    // ✅ CORREÇÃO: TERMINAR WAVE
    void TerminarWave()
    {
        if (waveAtualIndex < 0 || waveAtualIndex >= waves.Count) return;

        waveAtiva = false;
        esperandoProximaWave = true;
        tempoWaveAtual = 0f;

        string nomeWave = waves[waveAtualIndex].nome;
        Debug.Log($"✅ WAVE {nomeWave} CONCLUÍDA!");

        OnWaveTerminada?.Invoke(waveAtualIndex);

        // Verifica se há mais waves
        if (waveAtualIndex >= waves.Count - 1)
        {
            Debug.Log("🏁 Todas as waves completas! Criando wave procedural...");
            CriarWaveProcedural();
        }
        else
        {
            Debug.Log($"⏳ Próxima wave em {tempoEntreWaves} segundos...");
        }
    }

    // ✅ CORREÇÃO: INICIAR PRÓXIMA WAVE
    void IniciarProximaWave()
    {
        int proximoIndex = waveAtualIndex + 1;

        if (proximoIndex >= waves.Count)
        {
            Debug.LogWarning("⚠️ Tentando iniciar wave além do limite! Criando procedural...");
            CriarWaveProcedural();
            proximoIndex = waves.Count - 1; // Usa a última wave criada
        }

        IniciarWave(proximoIndex);
    }

    // ✅ CORREÇÃO: CRIAR WAVE PROCEDURAL
    void CriarWaveProcedural()
    {
        if (waves.Count == 0)
        {
            Debug.LogError("❌ Não há waves para criar wave procedural!");
            return;
        }

        Wave ultimaWave = waves[waves.Count - 1];
        Wave novaWave = new Wave
        {
            nome = $"Wave {waves.Count + 1}",
            duracao = ultimaWave.duracao + 15f,
            maxInimigos = ultimaWave.maxInimigos + 10,
            intervaloSpaw = Mathf.Max(0.5f, ultimaWave.intervaloSpaw - 0.1f), // ✅ Redução mais suave
            multiplicadorDificuldade = ultimaWave.multiplicadorDificuldade + 0.15f, // ✅ Aumento mais suave
            waveEspecial = (waves.Count % 3 == 0)
        };

        waves.Add(novaWave);
        Debug.Log($"🆕 Wave procedural criada: {novaWave.nome}");
        Debug.Log($"📊 Stats: {novaWave.duracao}s, {novaWave.maxInimigos} inimigos, x{novaWave.multiplicadorDificuldade} dificuldade");
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
        else
        {
            Debug.Log($"✅ {tiposInimigos.Count} tipos de inimigos configurados com prefabs");
        }
    }

    void AplicarDificuldadeNoInimigo(GameObject inimigo, int waveIndex)
    {
        if (inimigo == null) return;

        InimigoController controller = inimigo.GetComponent<InimigoController>();
        if (controller != null && controller.dadosInimigo != null)
        {
            if (usarWaves && waveIndex >= 0 && waveIndex < waves.Count)
            {
                float multiplicador = waves[waveIndex].multiplicadorDificuldade;
                controller.AplicarDificuldade(multiplicador);
            }
        }
    }

    GameObject CriarInimigoSeguro(TipoInimigo tipo, Vector2 posicao)
    {
        if (tipo.prefab == null) return null;
        return Instantiate(tipo.prefab, posicao, Quaternion.identity);
    }

    void SpawnarGrupo()
    {
        int tamanhoGrupo = Random.Range(tamanhoGrupoMin, tamanhoGrupoMax + 1);
        Vector2 posicaoBase = CalcularPosicaoSpawn();

        int inimigosSpawnados = 0;

        for (int i = 0; i < tamanhoGrupo; i++)
        {
            if (inimigosAtivos.Count >= limiteInimigos) break;

            Vector2 offset = new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f));
            Vector2 posicaoSpawn = posicaoBase + offset;

            if (limitarAreaAtuacao)
            {
                posicaoSpawn = ManterDentroDaArea(posicaoSpawn);
            }

            TipoInimigo tipoEscolhido = EscolherTipoInimigoPorPeso();

            // ✅ CORREÇÃO: VERIFICAR SE O TIPO ESCOLHIDO É VÁLIDO
            if (tipoEscolhido == null || tipoEscolhido.prefab == null)
            {
                Debug.LogWarning("⚠️ Tipo de inimigo inválido para spawn em grupo");
                continue;
            }

            GameObject novoInimigo = CriarInimigoSeguro(tipoEscolhido, posicaoSpawn);
            if (novoInimigo != null)
            {
                inimigosAtivos.Add(novoInimigo);
                AplicarDificuldadeNoInimigo(novoInimigo, waveAtualIndex);
                inimigosSpawnados++;
            }
        }

        if (inimigosSpawnados > 0)
        {
            Debug.Log($"👥 Grupo de {inimigosSpawnados} inimigos spawnado!");
        }
    }

    void SpawnarInimigo()
    {
        TipoInimigo tipoEscolhido = EscolherTipoInimigoPorPeso();

        // ✅ CORREÇÃO: VERIFICAR SE O TIPO ESCOLHIDO É VÁLIDO ANTES DE USAR
        if (tipoEscolhido == null)
        {
            Debug.LogWarning("⚠️ Nenhum tipo de inimigo disponível para spawn!");
            return;
        }

        if (tipoEscolhido.prefab != null)
        {
            Vector2 posicaoSpawn = CalcularPosicaoSpawn();
            GameObject novoInimigo = CriarInimigoSeguro(tipoEscolhido, posicaoSpawn);

            if (novoInimigo != null)
            {
                inimigosAtivos.Add(novoInimigo);
                AplicarDificuldadeNoInimigo(novoInimigo, waveAtualIndex);

                if (Time.time % 10f < 0.1f)
                {
                    Debug.Log($"🎯 Spawn: {tipoEscolhido.nome} | Wave: {waveAtualIndex + 1}");
                }
            }
        }
        else
        {
            Debug.LogError($"❌ Prefab ausente para: {tipoEscolhido.nome}");
        }
    }

    public bool EstaDentroDaArea(Vector2 posicao)
    {
        if (!limitarAreaAtuacao) return true;
        Vector2 min = centroArea - tamanhoArea / 2f;
        Vector2 max = centroArea + tamanhoArea / 2f;
        return posicao.x >= min.x && posicao.x <= max.x && posicao.y >= min.y && posicao.y <= max.y;
    }

    public Vector2 ObterPosicaoAleatoriaNaArea()
    {
        Vector2 min = centroArea - tamanhoArea / 2f;
        Vector2 max = centroArea + tamanhoArea / 2f;
        return new Vector2(Random.Range(min.x, max.x), Random.Range(min.y, max.y));
    }

    public Vector2 ManterDentroDaArea(Vector2 posicao)
    {
        if (!limitarAreaAtuacao) return posicao;
        Vector2 min = centroArea - tamanhoArea / 2f;
        Vector2 max = centroArea + tamanhoArea / 2f;
        posicao.x = Mathf.Clamp(posicao.x, min.x, max.x);
        posicao.y = Mathf.Clamp(posicao.y, min.y, max.y);
        return posicao;
    }

    IEnumerator ManterInimigosNaArea()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            for (int i = inimigosAtivos.Count - 1; i >= 0; i--)
            {
                if (inimigosAtivos[i] != null && !EstaDentroDaArea(inimigosAtivos[i].transform.position))
                {
                    inimigosAtivos[i].transform.position = ManterDentroDaArea(inimigosAtivos[i].transform.position);
                }
            }
        }
    }

    Vector2 CalcularPosicaoSpawn()
    {
        if (limitarAreaAtuacao) return ObterPosicaoAleatoriaNaArea();

        float angulo = Random.Range(0f, 360f);
        Vector2 direcao = new Vector2(Mathf.Cos(angulo * Mathf.Deg2Rad), Mathf.Sin(angulo * Mathf.Deg2Rad));
        float distancia = Random.Range(distanciaMinima, distanciaMaxima);
        Vector2 posicao = (Vector2)player.position + direcao * distancia;

        return manterInimigosDentroArea ? ManterDentroDaArea(posicao) : posicao;
    }

    bool PodeSpawnarGrupo()
    {
        return tempoDesdeUltimoGrupo >= intervaloEntreGrupos && inimigosAtivos.Count < limiteInimigos - tamanhoGrupoMin;
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
    }

    bool PodeSpawnarInimigo()
    {
        return tempoDesdeUltimoSpawn >= tempoEntreSpawns && inimigosAtivos.Count < limiteInimigos && inimigosDisponiveis.Count > 0;
    }

    TipoInimigo EscolherTipoInimigoPorPeso()
    {
        if (inimigosDisponiveis.Count == 0) return null;
        float pesoTotal = 0f;
        foreach (TipoInimigo inimigo in inimigosDisponiveis) pesoTotal += inimigo.peso;
        if (pesoTotal <= 0) return null;

        float valorAleatorio = Random.Range(0f, pesoTotal);
        float pesoAcumulado = 0f;
        foreach (TipoInimigo inimigo in inimigosDisponiveis)
        {
            pesoAcumulado += inimigo.peso;
            if (valorAleatorio <= pesoAcumulado) return inimigo;
        }
        return inimigosDisponiveis[0];
    }

    void AumentarDificuldade()
    {
        tempoEntreSpawns = Mathf.Max(0.3f, tempoEntreSpawns - reducaoTempoSpawn);
        limiteInimigos += 2;
        Debug.Log($"📈 Dificuldade Aumentada! Spawn: {tempoEntreSpawns:F1}s | Limite: {limiteInimigos}");
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

    // Métodos de debug e informações
    public int GetWaveAtualIndex() => waveAtualIndex;
    public string GetNomeWaveAtual() => waveAtualIndex >= 0 && waveAtualIndex < waves.Count ? waves[waveAtualIndex].nome : "Nenhuma";
    public int GetInimigosAtivosCount() => inimigosAtivos.Count;
    public int GetInimigosDisponiveisCount() => inimigosDisponiveis.Count;
    public bool IsWaveAtiva() => waveAtiva;
}