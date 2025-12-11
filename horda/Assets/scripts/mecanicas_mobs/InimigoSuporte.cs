using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InimigoSuporte : MonoBehaviour
{
    [Header("Configurações de Cura")]
    public float taxaCura = 10f; // Quantidade de cura por tick
    public float intervaloCura = 2f; // Intervalo entre curas
    public float raioCura = 5f; // Raio da área de cura
    public LayerMask alvosCuraLayer; // Layer dos mobs que podem ser curados
    public bool curaContinuamente = true; // Se cura continuamente ou uma vez
    public bool curaAPrimeiros = true; // Cura os mais próximos primeiro
    public int maximoAlvosPorVez = 3; // Máximo de alvos curados por tick

    [Header("Efeitos Visuais")]
    public GameObject efeitoCuraPrefab;
    public Color corAreaCura = new Color(0f, 1f, 0f, 0.3f);
    public GameObject particulasCura;
    public AudioClip somCura;
    public float tempoEfeitoCura = 1f;

    [Header("Configurações de Estado")]
    public bool estaAtivo = true;
    public float delayInicial = 0f;

    private float proximaCura = 0f;
    private AudioSource audioSource;
    private List<GameObject> efeitosAtivos = new List<GameObject>();
    private SphereCollider areaCuraCollider;

    [Header("Prioridade de Cura")]
    public bool priorizarAliadosComMenosVida = true;
    public float porcentagemVidaParaPriorizar = 0.3f; // 30% de vida

    [Header("Sistema de Buff")]
    public bool aplicarBuff = false;
    public float duracaoBuff = 5f;
    public float aumentoDefesa = 0.1f; // 10% de redução de dano

    void Start()
    {
        InicializarComponentes();
        ConfigurarAreaCura();

        if (delayInicial > 0)
        {
            StartCoroutine(IniciarAposDelay());
        }
    }

    void InicializarComponentes()
    {
        // Adicionar ou obter AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && somCura != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.volume = 0.5f;
        }

        // Criar collider para área de cura (opcional, para trigger events)
        areaCuraCollider = gameObject.AddComponent<SphereCollider>();
        areaCuraCollider.radius = raioCura;
        areaCuraCollider.isTrigger = true;
        areaCuraCollider.enabled = false; // Usaremos apenas para debug
    }

    void ConfigurarAreaCura()
    {
        // Se tiver partículas de cura, configurar
        if (particulasCura != null)
        {
            GameObject particulas = Instantiate(particulasCura, transform);
            particulas.transform.localPosition = Vector3.zero;

            // Ajustar tamanho das partículas baseado no raio
            ParticleSystem.ShapeModule shape = particulas.GetComponent<ParticleSystem>().shape;
            shape.radius = raioCura * 0.8f;
        }
    }

    IEnumerator IniciarAposDelay()
    {
        estaAtivo = false;
        yield return new WaitForSeconds(delayInicial);
        estaAtivo = true;
        proximaCura = Time.time + intervaloCura;
    }

    void Update()
    {
        if (!estaAtivo) return;

        if (curaContinuamente && Time.time >= proximaCura)
        {
            ExecutarCuraEmArea();
            proximaCura = Time.time + intervaloCura;
        }
    }

    public void ExecutarCuraEmArea()
    {
        if (!estaAtivo) return;

        // Encontrar todos os alvos na área
        Collider2D[] alvosNaArea = Physics2D.OverlapCircleAll(
            transform.position,
            raioCura,
            alvosCuraLayer
        );

        if (alvosNaArea.Length == 0) return;

        // Filtrar e ordenar alvos
        List<Collider2D> alvosParaCurar = FiltrarEOrdenarAlvos(alvosNaArea);

        // Limitar número de alvos por tick
        int alvosACurar = Mathf.Min(alvosParaCurar.Count, maximoAlvosPorVez);

        for (int i = 0; i < alvosACurar; i++)
        {
            InimigoController inimigo = alvosParaCurar[i].GetComponent<InimigoController>();
            if (inimigo != null && inimigo != this.GetComponent<InimigoController>())
            {
                CurarInimigo(inimigo);
                CriarEfeitoVisual(alvosParaCurar[i].transform.position);
            }
        }

        // Tocar som de cura
        if (somCura != null && audioSource != null)
        {
            audioSource.PlayOneShot(somCura);
        }

        Debug.Log($"✨ {gameObject.name} curou {alvosACurar} alvos!");
    }

    List<Collider2D> FiltrarEOrdenarAlvos(Collider2D[] alvos)
    {
        List<Collider2D> alvosFiltrados = new List<Collider2D>();

        foreach (Collider2D col in alvos)
        {
            InimigoController inimigo = col.GetComponent<InimigoController>();
            if (inimigo != null && inimigo != this.GetComponent<InimigoController>())
            {
                alvosFiltrados.Add(col);
            }
        }

        // Ordenar por prioridade
        if (priorizarAliadosComMenosVida)
        {
            alvosFiltrados.Sort((a, b) =>
            {
                InimigoController inimigoA = a.GetComponent<InimigoController>();
                InimigoController inimigoB = b.GetComponent<InimigoController>();

                // Calcular porcentagem de vida
                float vidaPorcentagemA = inimigoA.vidaAtual / inimigoA.dadosInimigo.vidaBase;
                float vidaPorcentagemB = inimigoB.vidaAtual / inimigoB.dadosInimigo.vidaBase;

                // Priorizar quem tem menos vida
                return vidaPorcentagemA.CompareTo(vidaPorcentagemB);
            });
        }
        else if (curaAPrimeiros)
        {
            // Ordenar por proximidade
            alvosFiltrados.Sort((a, b) =>
            {
                float distanciaA = Vector2.Distance(transform.position, a.transform.position);
                float distanciaB = Vector2.Distance(transform.position, b.transform.position);
                return distanciaA.CompareTo(distanciaB);
            });
        }

        return alvosFiltrados;
    }

    void CurarInimigo(InimigoController inimigo)
    {
        // Aplicar cura
        float vidaAnterior = inimigo.vidaAtual;
        float vidaMaxima = inimigo.dadosInimigo.vidaBase;

        inimigo.vidaAtual = Mathf.Min(vidaAnterior + taxaCura, vidaMaxima);

        Debug.Log($"💚 Curando {inimigo.gameObject.name}: +{taxaCura} HP " +
                  $"({vidaAnterior:F0} -> {inimigo.vidaAtual:F0}/{vidaMaxima:F0})");

        // Aplicar buff se configurado
        if (aplicarBuff)
        {
            AplicarBuffDefesa(inimigo);
        }

        // Mostrar efeito de cura
        MostrarEfeitoCuraNoInimigo(inimigo.transform);
    }

    void AplicarBuffDefesa(InimigoController inimigo)
    {
        // Aqui você pode adicionar lógica de buff
        // Por exemplo, reduzir dano recebido temporariamente
        StartCoroutine(BuffDefesaTemporario(inimigo));
    }

    IEnumerator BuffDefesaTemporario(InimigoController inimigo)
    {
        // Salvar dano original
        float danoOriginal = inimigo.danoAtual;

        // Aplicar redução de dano (aumento de defesa)
        float reducaoDano = 1f - aumentoDefesa;
        inimigo.danoAtual *= reducaoDano;

        Debug.Log($"🛡️ Buff aplicado em {inimigo.gameObject.name}: " +
                  $"Defesa +{aumentoDefesa * 100}% por {duracaoBuff}s");

        // Aguardar duração do buff
        yield return new WaitForSeconds(duracaoBuff);

        // Restaurar dano original
        if (inimigo != null)
        {
            inimigo.danoAtual = danoOriginal;
            Debug.Log($"🛡️ Buff removido de {inimigo.gameObject.name}");
        }
    }

    void CriarEfeitoVisual(Vector3 posicao)
    {
        if (efeitoCuraPrefab == null) return;

        GameObject efeito = Instantiate(efeitoCuraPrefab, posicao, Quaternion.identity);
        efeitosAtivos.Add(efeito);

        // Destruir após tempo
        StartCoroutine(DestruirEfeitoAposTempo(efeito, tempoEfeitoCura));
    }

    void MostrarEfeitoCuraNoInimigo(Transform alvo)
    {
        // Criar texto flutuante de cura
        if (DamageNumberManager.Instance != null)
        {
            DamageNumberManager.Instance.ShowHeal(alvo, taxaCura);
        }
        else
        {
            CriarTextoCuraLocal(alvo.position, taxaCura);
        }
    }

    void CriarTextoCuraLocal(Vector3 posicao, float quantidade)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject textObj = new GameObject("CuraTexto");
        textObj.transform.SetParent(canvas.transform, false);

        Vector3 screenPos = Camera.main.WorldToScreenPoint(posicao);
        screenPos.y += 80;
        textObj.transform.position = screenPos;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = $"+{Mathf.RoundToInt(quantidade)}";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 24;
        text.color = Color.green;
        text.fontStyle = FontStyles.Bold;

        // Adicionar animação
        textObj.AddComponent<CuraAnimatorLocal>().Initialize();

        Destroy(textObj, 1f);
    }

    IEnumerator DestruirEfeitoAposTempo(GameObject efeito, float tempo)
    {
        yield return new WaitForSeconds(tempo);

        if (efeito != null)
        {
            efeitosAtivos.Remove(efeito);
            Destroy(efeito);
        }
    }

    // Método para ativar/desativar cura manualmente
    public void AtivarCura(bool ativar)
    {
        estaAtivo = ativar;
        if (ativar)
        {
            proximaCura = Time.time + intervaloCura;
        }
    }

    // Método para cura única (pode ser chamado por evento)
    public void CurarUmaVez()
    {
        if (!estaAtivo) return;
        ExecutarCuraEmArea();
    }

    // Método para ajustar parâmetros dinamicamente
    public void ConfigurarCura(float novaTaxa, float novoIntervalo, float novoRaio)
    {
        taxaCura = novaTaxa;
        intervaloCura = novoIntervalo;
        raioCura = novoRaio;

        // Atualizar collider se existir
        if (areaCuraCollider != null)
        {
            areaCuraCollider.radius = novoRaio;
        }
    }

    // Método para aumentar poder de cura (útil para waves mais difíceis)
    public void AumentarPoderCura(float bonus)
    {
        taxaCura += bonus;
        Debug.Log($"✨ Poder de cura aumentado para: {taxaCura}");
    }

    void OnDrawGizmosSelected()
    {
        // Desenhar área de cura
        Gizmos.color = corAreaCura;
        Gizmos.DrawWireSphere(transform.position, raioCura);

        // Área sólida para melhor visualização
        Gizmos.color = new Color(corAreaCura.r, corAreaCura.g, corAreaCura.b, 0.1f);
        Gizmos.DrawSphere(transform.position, raioCura);
    }

    void OnDestroy()
    {
        // Limpar efeitos ao destruir
        foreach (GameObject efeito in efeitosAtivos)
        {
            if (efeito != null)
            {
                Destroy(efeito);
            }
        }
        efeitosAtivos.Clear();
    }
}

// Classe de animação para texto de cura
public class CuraAnimatorLocal : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    private float timer = 0f;
    private Vector3 startPos;

    public void Initialize()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        startPos = transform.position;
    }

    void Update()
    {
        if (textMesh == null) return;

        timer += Time.deltaTime;

        // Move para cima suavemente
        float speed = 60f;
        transform.position = startPos + new Vector3(0, speed * timer, 0);

        // Efeito de escala (pequeno pulso)
        float pulse = Mathf.Sin(timer * 10f) * 0.1f + 1f;
        transform.localScale = Vector3.one * pulse;

        // Fade out
        Color color = textMesh.color;
        color.a = 1f - (timer / 1f);
        textMesh.color = color;

        if (timer >= 1f)
            Destroy(gameObject);
    }
}