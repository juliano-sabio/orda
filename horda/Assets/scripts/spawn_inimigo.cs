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
    }

    [Header("REFERÊNCIAS")]
    public Transform player;

    [Header("CAMADAS PROIBIDAS (IMPORTANTE)")]
    [Tooltip("Selecione aqui as Layers que os inimigos NÃO podem nascer em cima (ex: Obstacles, Paredes, Agua)")]
    public LayerMask camadasBloqueadas;
    [Tooltip("O tamanho do 'corpo' do inimigo para checar se cabe no lugar")]
    public float raioDeChecagem = 0.5f;
    [Tooltip("Quantas vezes ele tenta sortear um lugar novo se o primeiro estiver ocupado")]
    public int tentativasMaximas = 15;

    [Header("LISTA DE INIMIGOS")]
    public List<TipoInimigo> tiposInimigos = new List<TipoInimigo>();

    [Header("SISTEMA DE WAVES")]
    public List<Wave> waves = new List<Wave>();
    public bool usarWaves = true;
    public float tempoEntreWaves = 5f;

    [Header("CONFIGURAÇÕES DE DISTÂNCIA")]
    [SerializeField] private float distanciaMinima = 8f;
    [SerializeField] private float distanciaMaxima = 15f;
    [SerializeField] private int limiteInimigosGlobal = 30;

    [Header("ESCALADA DE DIFICULDADE")]
    [Tooltip("Ativa o aumento progressivo de dificuldade com o tempo")]
    public bool escalarDificuldade = true;
    [Tooltip("Intervalo de spawn no início do jogo (segundos)")]
    public float intervaloInicial   = 2.5f;
    [Tooltip("Intervalo mínimo de spawn (mais difícil possível)")]
    public float intervaloMinimo    = 0.35f;
    [Tooltip("Limite de inimigos simultâneos no início")]
    public int   limiteInicialInimigos = 15;
    [Tooltip("Limite de inimigos simultâneos no pico de dificuldade")]
    public int   limiteFinalInimigos   = 60;
    [Tooltip("Tempo (segundos) para atingir dificuldade máxima")]
    public float tempoParaEscalar   = 360f;

    // Variáveis de controle interno
    private float tempoTotalJogo = 0f;
    private float cronometroSpawn = 0f;
    private int waveAtualIndex = 0;
    private bool waveAtiva = false;
    private List<GameObject> inimigosAtivos = new List<GameObject>();
    private List<TipoInimigo> inimigosDisponiveis = new List<TipoInimigo>();

    void Start()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (usarWaves && waves.Count > 0) IniciarWave(0);
        StartCoroutine(LimpezaAutomatica());
    }

    void Update()
    {
        if (player == null) return;

        tempoTotalJogo += Time.deltaTime;
        cronometroSpawn += Time.deltaTime;

        AtualizarInimigosDisponiveis();

        if (usarWaves) GerenciarSistemaWaves();
        else LógicaSpawnSimples();
    }

    float GetIntervaloAtual()
    {
        if (!escalarDificuldade) return usarWaves && waves.Count > 0 ? waves[waveAtualIndex].intervaloSpaw : 2f;
        float t = Mathf.Clamp01(tempoTotalJogo / tempoParaEscalar);
        float curva = t * t * (3f - 2f * t); // smoothstep
        float base_ = usarWaves && waves.Count > 0 ? waves[waveAtualIndex].intervaloSpaw : intervaloInicial;
        return Mathf.Lerp(Mathf.Min(base_, intervaloInicial), intervaloMinimo, curva);
    }

    int GetLimiteAtual()
    {
        if (!escalarDificuldade) return limiteInimigosGlobal;
        float t = Mathf.Clamp01(tempoTotalJogo / tempoParaEscalar);
        float curva = t * t * (3f - 2f * t);
        return Mathf.RoundToInt(Mathf.Lerp(limiteInicialInimigos, limiteFinalInimigos, curva));
    }

    void GerenciarSistemaWaves()
    {
        if (waveAtiva)
        {
            if (cronometroSpawn >= GetIntervaloAtual())
            {
                TentarSpawnar();
                cronometroSpawn = 0;
            }
        }
    }

    void LógicaSpawnSimples()
    {
        if (cronometroSpawn >= GetIntervaloAtual())
        {
            TentarSpawnar();
            cronometroSpawn = 0;
        }
    }

    void TentarSpawnar()
    {
        if (inimigosAtivos.Count >= GetLimiteAtual() || inimigosDisponiveis.Count == 0) return;

        // Tenta encontrar uma posição válida
        Vector2? posicaoValida = ObterPosicaoLivre();

        if (posicaoValida.HasValue)
        {
            TipoInimigo tipo = EscolherInimigoPorPeso();
            if (tipo == null || tipo.prefab == null) return;
            GameObject novoInimigo = Instantiate(tipo.prefab, posicaoValida.Value, Quaternion.identity);
            inimigosAtivos.Add(novoInimigo);
        }
    }

    bool ForaDaCamera(Vector2 pos)
    {
        if (Camera.main == null) return true;
        Vector3 vp = Camera.main.WorldToViewportPoint(pos);
        const float margem = 0.05f;
        return vp.x < -margem || vp.x > 1f + margem || vp.y < -margem || vp.y > 1f + margem;
    }

    Vector2? ObterPosicaoLivre()
    {
        for (int i = 0; i < tentativasMaximas; i++)
        {
            float angulo    = Random.Range(0f, 360f);
            Vector2 direcao = new Vector2(Mathf.Cos(angulo * Mathf.Deg2Rad), Mathf.Sin(angulo * Mathf.Deg2Rad));
            float distancia = Random.Range(distanciaMinima, distanciaMaxima);
            Vector2 ponto   = (Vector2)player.position + direcao * distancia;

            if (!ForaDaCamera(ponto)) continue;

            if (Physics2D.OverlapCircle(ponto, raioDeChecagem, camadasBloqueadas) == null)
                return ponto;
        }

        return null;
    }

    TipoInimigo EscolherInimigoPorPeso()
    {
        float pesoTotal = 0;
        foreach (var inimigo in inimigosDisponiveis) pesoTotal += inimigo.peso;
        float sorteio = Random.Range(0, pesoTotal);
        float acumulado = 0;
        foreach (var inimigo in inimigosDisponiveis)
        {
            acumulado += inimigo.peso;
            if (sorteio <= acumulado) return inimigo;
        }
        return inimigosDisponiveis[0];
    }

    void AtualizarInimigosDisponiveis()
    {
        inimigosDisponiveis.Clear();
        foreach (var inimigo in tiposInimigos)
            if (inimigo.prefab != null && tempoTotalJogo >= inimigo.tempoParaAparecer) inimigosDisponiveis.Add(inimigo);
    }

    void IniciarWave(int index)
    {
        waveAtualIndex = index;
        waveAtiva = true;
    }

    IEnumerator LimpezaAutomatica()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            inimigosAtivos.RemoveAll(item => item == null);
        }
    }

    // Visualização no Editor
    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, distanciaMinima);
            Gizmos.DrawWireSphere(player.position, distanciaMaxima);
        }
    }
}
