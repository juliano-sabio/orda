using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InimigoSuporte : MonoBehaviour, IEnemyCosmetic
{
    [Header("Configurações de Cura")]
    public float taxaCura = 10f;
    public float intervaloCura = 2f;
    public float raioCura = 5f;
    public LayerMask alvosCuraLayer;
    public bool curaContinuamente = true;
    public int maximoAlvosPorVez = 3;

    [Header("Animação Independente (Objeto Filho)")]
    public Animator areaCuraAnimator; // Arraste o objeto FILHO aqui
    public string triggerCura = "Cura"; // Nome do Trigger no Animator do filho

    [Header("Efeitos Visuais")]
    public GameObject efeitoCuraPrefab;
    public Color corAreaCura = new Color(0f, 1f, 0f, 0.3f);
    public GameObject particulasCura;
    public AudioClip somCura;
    public float tempoEfeitoCura = 1f;

    [Header("Configurações de Estado")]
    public bool estaAtivo = true;
    public float delayInicial = 0f;

    [Header("Prioridade de Cura")]
    public bool priorizarAliadosComMenosVida = true;

    [Header("Sistema de Buff")]
    public bool aplicarBuff = false;
    public float duracaoBuff = 5f;
    public float aumentoDefesa = 0.1f;

    [Header("Onda de Repulsão ao Curar")]
    public float forcaRepulsao  = 14f;
    public float raioRepulsao   = 7f;
    public float duracaoOndaRep = 0.55f;

    private float proximaCura = 0f;
    private AudioSource audioSource;
    private List<GameObject> efeitosAtivos = new List<GameObject>();

    void Start()
    {
        InicializarComponentes();

        if (delayInicial > 0)
        {
            StartCoroutine(IniciarAposDelay());
        }
        else
        {
            proximaCura = Time.time + intervaloCura;
        }
    }

    void InicializarComponentes()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null && somCura != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.volume = 0.5f;
            audioSource.playOnAwake = false;
        }

        CriarAuraParticulas();
    }

    void CriarAuraParticulas()
    {
        if (particulasCura == null) return;

        GameObject particulas = Instantiate(particulasCura, transform);
        particulas.transform.localPosition = Vector3.zero;

        ParticleSystem ps = particulas.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var shape = ps.shape;
            shape.radius = raioCura * 0.8f;
        }
    }

    // Co-op (cliente): o script é desligado no fantoche — o EnemyNet chama isto pra
    // montar só a aura de partículas de cura (visual ambiente).
    public void SetupVisualCosmetico() => CriarAuraParticulas();

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

        // ✅ DISPARA A ANIMAÇÃO NO FILHO SEM PARAR O PAI
        if (areaCuraAnimator != null)
        {
            areaCuraAnimator.SetTrigger(triggerCura);
        }

        Collider2D[] alvosNaArea = Physics2D.OverlapCircleAll(
            transform.position,
            raioCura,
            alvosCuraLayer
        );

        if (alvosNaArea.Length == 0) return;

        List<InimigoController> alvosParaCurar = new List<InimigoController>();

        foreach (Collider2D col in alvosNaArea)
        {
            InimigoController inimigo = col.GetComponent<InimigoController>();
            if (inimigo != null &&
                inimigo != this.GetComponent<InimigoController>() &&
                inimigo.podeReceberCura &&
                !inimigo.estaMorrendo &&
                inimigo.vidaAtual < inimigo.vidaMaxima)
            {
                alvosParaCurar.Add(inimigo);
            }
        }

        if (alvosParaCurar.Count == 0) return;

        if (priorizarAliadosComMenosVida)
        {
            alvosParaCurar.Sort((a, b) =>
                a.GetPorcentagemVida().CompareTo(b.GetPorcentagemVida())
            );
        }

        int alvosACurar = Mathf.Min(alvosParaCurar.Count, maximoAlvosPorVez);

        for (int i = 0; i < alvosACurar; i++)
            CurarInimigo(alvosParaCurar[i]);

        // Onda de cura centrada na própria slime
        SomSkill.Tocar(SomSkill.Tipo.SlimeCurativaCura, transform.position, 0.45f);

        var ondaGO = new GameObject("OndaCura");
        ondaGO.transform.position = transform.position;
        ondaGO.AddComponent<OndaCuraVisual>().Iniciar(raioCura, 0.6f);

        // Co-op: replica o pulso de cura (onda + som) pro P2 (a slime não roda gameplay no cliente).
        GetComponent<EnemyNet>()?.BroadcastCosmetico(MobCosmeticos.OndaCura, transform.position, raioCura, 0.6f);

        EmitirOndaRepulsao();

        if (somCura != null && audioSource != null)
        {
            AudioBus.PlayOn(audioSource, somCura);
        }
    }

    void EmitirOndaRepulsao()
    {
        var cols = Physics2D.OverlapCircleAll(transform.position, raioRepulsao, alvosCuraLayer);
        foreach (var c in cols)
        {
            var rb = c.GetComponent<Rigidbody2D>() ?? c.GetComponentInParent<Rigidbody2D>();
            if (rb == null || rb.gameObject == gameObject) continue;

            Vector2 dir  = (Vector2)rb.transform.position - (Vector2)transform.position;
            float   dist = dir.magnitude;
            if (dist > 0.1f)
                rb.AddForce(dir.normalized * forcaRepulsao, ForceMode2D.Impulse);
        }

        var go = new GameObject("OndaRepulsao");
        go.transform.position = transform.position;
        StartCoroutine(AnimarOndaRepulsao(go));

        // Co-op: replica a onda de repulsão (anel verde) pro P2.
        GetComponent<EnemyNet>()?.BroadcastCosmetico(MobCosmeticos.OndaCor,
            transform.position, raioRepulsao, 0.25f, 1f, 0.45f, duracaoOndaRep);
    }

    IEnumerator AnimarOndaRepulsao(GameObject go)
    {
        const int SEGS = 48;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 10;

        Vector2 centro = go.transform.position;

        for (float t = 0f; t < duracaoOndaRep; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p    = t / duracaoOndaRep;
            float r    = Mathf.Lerp(0f, raioRepulsao, p);
            float a    = Mathf.Lerp(0.85f, 0f, p);
            float larg = Mathf.Lerp(0.32f, 0.05f, p);
            Color cor  = new Color(0.25f, 1f, 0.45f, a);

            lr.startColor = lr.endColor = cor;
            lr.startWidth = lr.endWidth = larg;

            for (int i = 0; i < SEGS; i++)
            {
                float ang = i / (float)SEGS * Mathf.PI * 2f;
                lr.SetPosition(i, centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
            }

            yield return null;
        }

        if (go != null) Destroy(go);
    }

    void CurarInimigo(InimigoController inimigo)
    {
        if (inimigo == null || inimigo.estaMorrendo) return;

        inimigo.ReceberCura(taxaCura);

        if (aplicarBuff && inimigo != null)
        {
            inimigo.AplicarBuffDefesa(aumentoDefesa, duracaoBuff);
        }
    }

    void CriarEfeitoVisual(Vector3 posicao)
    {
        if (efeitoCuraPrefab == null) return;

        GameObject efeito = Instantiate(efeitoCuraPrefab, posicao, Quaternion.identity);
        efeitosAtivos.Add(efeito);

        StartCoroutine(DestruirEfeitoAposTempo(efeito, tempoEfeitoCura));
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

    public void AtivarCura(bool ativar)
    {
        estaAtivo = ativar;
        if (ativar)
        {
            proximaCura = Time.time + intervaloCura;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = corAreaCura;
        Gizmos.DrawWireSphere(transform.position, raioCura);
    }
}