using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InimigoSuporte : MonoBehaviour
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

        if (particulasCura != null)
        {
            GameObject particulas = Instantiate(particulasCura, transform);
            particulas.transform.localPosition = Vector3.zero;

            ParticleSystem ps = particulas.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var shape = ps.shape;
                shape.radius = raioCura * 0.8f;
            }
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
        {
            CurarInimigo(alvosParaCurar[i]);
            CriarEfeitoVisual(alvosParaCurar[i].transform.position);
        }

        if (somCura != null && audioSource != null)
        {
            audioSource.PlayOneShot(somCura);
        }
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