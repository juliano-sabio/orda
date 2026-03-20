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

    void GerenciarSistemaWaves()
    {
        if (waveAtiva)
        {
            if (cronometroSpawn >= waves[waveAtualIndex].intervaloSpaw)
            {
                TentarSpawnar();
                cronometroSpawn = 0;
            }
        }
    }

    void LógicaSpawnSimples()
    {
        if (cronometroSpawn >= 2f) // Intervalo padrão se não houver wave
        {
            TentarSpawnar();
            cronometroSpawn = 0;
        }
    }

    void TentarSpawnar()
    {
        if (inimigosAtivos.Count >= limiteInimigosGlobal || inimigosDisponiveis.Count == 0) return;

        // Tenta encontrar uma posição válida
        Vector2? posicaoValida = ObterPosicaoLivre();

        if (posicaoValida.HasValue)
        {
            TipoInimigo tipo = EscolherInimigoPorPeso();
            GameObject novoInimigo = Instantiate(tipo.prefab, posicaoValida.Value, Quaternion.identity);
            inimigosAtivos.Add(novoInimigo);
        }
    }

    // A FUNÇÃO QUE RESOLVE SEU PROBLEMA
    Vector2? ObterPosicaoLivre()
    {
        for (int i = 0; i < tentativasMaximas; i++)
        {
            // 1. Sorteia uma posição ao redor do player
            float angulo = Random.Range(0f, 360f);
            Vector2 direcao = new Vector2(Mathf.Cos(angulo * Mathf.Deg2Rad), Mathf.Sin(angulo * Mathf.Deg2Rad));
            float distancia = Random.Range(distanciaMinima, distanciaMaxima);
            Vector2 pontoSorteado = (Vector2)player.position + (direcao * distancia);

            // 2. Checa se nesse ponto existe algum colisor das camadas bloqueadas
            // O OverlapCircle retorna qualquer colisor que tocar esse círculo
            Collider2D colisorEncontrado = Physics2D.OverlapCircle(pontoSorteado, raioDeChecagem, camadasBloqueadas);

            if (colisorEncontrado == null)
            {
                // Se for null, o caminho está livre!
                return pontoSorteado;
            }

            // Se chegou aqui, ele bateu em algo e o loop vai tentar de novo (até o limite de tentativasMaximas)
        }

        return null; // Falhou em achar um lugar limpo
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
            if (tempoTotalJogo >= inimigo.tempoParaAparecer) inimigosDisponiveis.Add(inimigo);
    }

    void IniciarWave(int index)
    {
        waveAtualIndex = index;
        waveAtiva = true;
        Debug.Log("Iniciando: " + waves[index].nome);
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