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

    [Header("DETECÇÃO DE COLISÃO")]
    [Tooltip("Layer dos objetos que impedem o spawn (Paredes, Obstáculos)")]
    public LayerMask camadasObstrutivas;
    [Tooltip("Tamanho do raio de checagem (ajuste conforme o tamanho do inimigo)")]
    public float raioChecagemInimigo = 0.5f;
    [Tooltip("Quantas vezes tentar sortear um lugar vazio antes de desistir")]
    public int maxTentativasSpawn = 10;

    [Header("LIMITE DE ÁREA DE ATUAÇÃO")]
    public bool limitarAreaAtuacao = false;
    public Vector2 centroArea = Vector2.zero;
    public Vector2 tamanhoArea = new Vector2(20f, 20f);

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

    private int waveAtualIndex = 0;
    private float tempoWaveAtual = 0f;
    private bool waveAtiva = false;
    private bool esperandoProximaWave = false;

    // Eventos
    public System.Action<int, string> OnWaveIniciada;
    public System.Action<int> OnWaveTerminada;

    void Start()
    {
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
        }

        StartCoroutine(LimparListaInimigos());
    }

    void Update()
    {
        if (player == null) return;

        tempoTotalJogo += Time.deltaTime;
        tempoDesdeUltimoSpawn += Time.deltaTime;
        tempoDesdeUltimoGrupo += Time.deltaTime;

        AtualizarInimigosDisponiveis();

        if (usarWaves)
            GerenciarWaves();
        else
            ProcessarSpawnNormal();
    }

    void ProcessarSpawnNormal()
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

    void GerenciarWaves()
    {
        if (waveAtiva)
        {
            tempoWaveAtual += Time.deltaTime;

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

            if (tempoWaveAtual >= waves[waveAtualIndex].duracao)
                TerminarWave();
        }
        else if (esperandoProximaWave)
        {
            tempoWaveAtual += Time.deltaTime;
            if (tempoWaveAtual >= tempoEntreWaves)
            {
                waveAtualIndex++;
                if (waveAtualIndex < waves.Count)
                    IniciarWave(waveAtualIndex);
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
        waveAtiva = true;
        esperandoProximaWave = false;
        tempoWaveAtual = 0f;

        Wave wave = waves[index];
        tempoEntreSpawns = wave.intervaloSpaw;
        limiteInimigos = wave.maxInimigos;

        Debug.Log($"=== INICIANDO {wave.nome} ===");
        OnWaveIniciada?.Invoke(index, wave.nome);
    }

    void TerminarWave()
    {
        waveAtiva = false;
        esperandoProximaWave = true;
        tempoWaveAtual = 0f;
        Debug.Log($"✅ WAVE {waveAtualIndex + 1} CONCLUÍDA!");
        OnWaveTerminada?.Invoke(waveAtualIndex);
    }

    void SpawnarInimigo()
    {
        if (inimigosDisponiveis.Count == 0) return;

        Vector2? posicaoValida = CalcularPosicaoSpawnLivre();

        if (posicaoValida.HasValue)
        {
            TipoInimigo tipoEscolhido = EscolherTipoInimigoPorPeso();
            GameObject novoInimigo = Instantiate(tipoEscolhido.prefab, posicaoValida.Value, Quaternion.identity);
            if (novoInimigo != null) inimigosAtivos.Add(novoInimigo);
        }
    }

    void SpawnarGrupo()
    {
        int tamanhoGrupo = Random.Range(tamanhoGrupoMin, tamanhoGrupoMax + 1);

        for (int i = 0; i < tamanhoGrupo; i++)
        {
            if (inimigosAtivos.Count >= limiteInimigos) break;

            Vector2? posicaoValida = CalcularPosicaoSpawnLivre();

            if (posicaoValida.HasValue)
            {
                TipoInimigo tipoEscolhido = EscolherTipoInimigoPorPeso();
                GameObject novoInimigo = Instantiate(tipoEscolhido.prefab, posicaoValida.Value, Quaternion.identity);
                if (novoInimigo != null) inimigosAtivos.Add(novoInimigo);
            }
        }
    }

    // A MÁGICA ACONTECE AQUI: Tenta encontrar um lugar sem colisão
    Vector2? CalcularPosicaoSpawnLivre()
    {
        for (int i = 0; i < maxTentativasSpawn; i++)
        {
            Vector2 posicaoSorteada = Vector2.zero;

            if (limitarAreaAtuacao)
            {
                Vector2 min = centroArea - tamanhoArea / 2f;
                Vector2 max = centroArea + tamanhoArea / 2f;
                posicaoSorteada = new Vector2(Random.Range(min.x, max.x), Random.Range(min.y, max.y));
            }
            else
            {
                float angulo = Random.Range(0f, 360f);
                Vector2 direcao = new Vector2(Mathf.Cos(angulo * Mathf.Deg2Rad), Mathf.Sin(angulo * Mathf.Deg2Rad));
                float distancia = Random.Range(distanciaMinima, distanciaMaxima);
                posicaoSorteada = (Vector2)player.position + direcao * distancia;
            }

            // Checa se existe colisor de "Obstaculos" no ponto sorteado
            Collider2D colisorObstrutor = Physics2D.OverlapCircle(posicaoSorteada, raioChecagemInimigo, camadasObstrutivas);

            if (colisorObstrutor == null)
                return posicaoSorteada; // Lugar limpo encontrado!
        }

        return null; // Não achou lugar vazio após as tentativas
    }

    TipoInimigo EscolherTipoInimigoPorPeso()
    {
        float pesoTotal = 0f;
        foreach (var inimigo in inimigosDisponiveis) pesoTotal += inimigo.peso;

        float random = Random.Range(0f, pesoTotal);
        float acumulado = 0f;

        foreach (var inimigo in inimigosDisponiveis)
        {
            acumulado += inimigo.peso;
            if (random <= acumulado) return inimigo;
        }
        return inimigosDisponiveis[0];
    }

    void AtualizarInimigosDisponiveis()
    {
        inimigosDisponiveis.Clear();
        foreach (TipoInimigo inimigo in tiposInimigos)
        {
            if (tempoTotalJogo >= inimigo.tempoParaAparecer)
                inimigosDisponiveis.Add(inimigo);
        }
    }

    bool PodeSpawnarInimigo() => tempoDesdeUltimoSpawn >= tempoEntreSpawns && inimigosAtivos.Count < limiteInimigos && inimigosDisponiveis.Count > 0;
    bool PodeSpawnarGrupo() => tempoDesdeUltimoGrupo >= intervaloEntreGrupos && inimigosAtivos.Count < limiteInimigos - tamanhoGrupoMin;

    IEnumerator LimparListaInimigos()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f);
            inimigosAtivos.RemoveAll(inimigo => inimigo == null);
        }
    }

    void CriarWaveProcedural()
    {
        Wave ultimaWave = waves[waves.Count - 1];
        waves.Add(new Wave
        {
            nome = $"Wave {waves.Count + 1}",
            duracao = ultimaWave.duracao + 15f,
            maxInimigos = ultimaWave.maxInimigos + 10,
            intervaloSpaw = Mathf.Max(0.5f, ultimaWave.intervaloSpaw - 0.1f)
        });
    }

    void VerificarPrefabs()
    {
        for (int i = tiposInimigos.Count - 1; i >= 0; i--)
            if (tiposInimigos[i].prefab == null) tiposInimigos.RemoveAt(i);
    }

    // Desenha as áreas no Editor para facilitar o ajuste
    void OnDrawGizmosSelected()
    {
        if (limitarAreaAtuacao)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(centroArea, tamanhoArea);
        }
        else if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, distanciaMinima);
            Gizmos.DrawWireSphere(player.position, distanciaMaxima);
        }
    }
}