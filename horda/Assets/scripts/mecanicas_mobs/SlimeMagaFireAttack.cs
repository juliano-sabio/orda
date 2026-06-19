using System.Collections;
using UnityEngine;

// Adicione ao prefab da slime_maga.
// Dispara periodicamente uma bola de fogo carregada que explode em área.
public class SlimeMagaFireAttack : MonoBehaviour
{
    [Header("Timing")]
    public float primeiroAtaque = 5f;
    public float intervaloCarga = 8f;
    public float duracaoCarga   = 1.8f;

    [Header("Projetil")]
    public float velocidadeProjetil = 6f;
    public float danoExplosao       = 15f;
    public float raioExplosao       = 2.4f;

    [Header("Fogo no Chão")]
    public float duracaoFogo   = 5f;
    public float danoPorTick   = 5f;
    public float intervaloTick = 0.8f;

    InimigoController inimigoCtrl;
    Transform         pontoDisparo;
    Transform         playerTr;
    bool              carregando;
    float             proxAtaque;

    void Start()
    {
        inimigoCtrl  = GetComponent<InimigoController>();
        pontoDisparo = transform.Find("pivo") ?? transform;
        proxAtaque   = Time.time + primeiroAtaque;

        var ps = PlayerStats.MaisProximo(transform.position);
        if (ps != null) playerTr = ps.transform;
    }

    void Update()
    {
        if (inimigoCtrl != null && inimigoCtrl.estaMorrendo) return;
        if (carregando) return;

        if (playerTr == null)
        {
            var ps = PlayerStats.MaisProximo(transform.position);
            if (ps != null) playerTr = ps.transform;
            return;
        }

        if (Time.time >= proxAtaque)
            StartCoroutine(SequenciaCarga());
    }

    IEnumerator SequenciaCarga()
    {
        carregando = true;
        proxAtaque = Time.time + intervaloCarga;

        var sr      = GetComponent<SpriteRenderer>();
        var corOrig = sr != null ? sr.color : Color.white;

        // ── Visual de carga ──────────────────────────────────────────────────

        // Container pai — filho do pivo, nunca muda de escala
        var container = new GameObject("CargaFogo");
        container.transform.SetParent(pontoDisparo, worldPositionStays: false);
        container.transform.localPosition = Vector3.zero;

        // Halo central (anel pequeno que cresce sutilmente)
        var haloGO = new GameObject("Halo");
        haloGO.transform.SetParent(container.transform, false);
        var haloSR = haloGO.AddComponent<SpriteRenderer>();
        haloSR.sprite       = FogoSprites.Anel;
        haloSR.sortingOrder = 16;

        // 3 faíscas que orbitam e convergem ao centro
        const int N = 3;
        var sparks = new SpriteRenderer[N];
        for (int i = 0; i < N; i++)
        {
            var sg = new GameObject($"Spark{i}");
            sg.transform.SetParent(container.transform, false);
            var ssr = sg.AddComponent<SpriteRenderer>();
            ssr.sprite       = FogoSprites.Disco;
            ssr.sortingOrder = 16;
            sparks[i] = ssr;
        }

        float orbitVel = Mathf.PI * 2.8f; // ~1.4 voltas/s

        float t = 0f;
        while (t < duracaoCarga)
        {
            if (inimigoCtrl != null && inimigoCtrl.estaMorrendo)
            {
                Destroy(container);
                carregando = false;
                yield break;
            }

            t += Time.deltaTime;
            float p = t / duracaoCarga;

            // Halo: pequeno, pulsa levemente, fica mais vivo perto do fim
            float haloEsc = Mathf.Lerp(0.10f, 0.32f, p) * (1f + Mathf.Sin(t * 9f) * 0.07f);
            haloGO.transform.localScale = Vector3.one * haloEsc;
            float haloA = Mathf.Lerp(0.25f, 0.60f, p);
            haloSR.color = new Color(1f, Mathf.Lerp(0.75f, 0.25f, p), 0f, haloA);

            // Faíscas: raio de órbita encolhe (convergem), aparecem gradualmente
            float orbitR   = Mathf.Lerp(0.55f, 0.07f, Mathf.Pow(p, 1.8f));
            float sparkAlp = Mathf.Clamp01(p * 5f);
            float sparkEsc = Mathf.Lerp(0.10f, 0.04f, p);
            for (int i = 0; i < N; i++)
            {
                float ang = Time.time * orbitVel + i * (Mathf.PI * 2f / N);
                sparks[i].transform.localPosition = new Vector3(
                    Mathf.Cos(ang) * orbitR,
                    Mathf.Sin(ang) * orbitR, 0f);
                sparks[i].transform.localScale = Vector3.one * sparkEsc;
                float corP = (Mathf.Sin(t * 7f + i * 1.3f) + 1f) * 0.5f;
                sparks[i].color = new Color(1f, Mathf.Lerp(0.3f, 0.9f, corP), 0f, sparkAlp * 0.80f);
            }

            // Tinta na slime: muito sutil, mal perceptível
            if (sr != null)
                sr.color = Color.Lerp(corOrig, new Color(1f, 0.45f, 0.1f), p * 0.18f);

            yield return null;
        }

        // Flash breve no momento do lançamento
        haloGO.transform.localScale = Vector3.one * 0.45f;
        haloSR.color = new Color(1f, 0.9f, 0.35f, 0.85f);
        yield return new WaitForSeconds(0.06f);

        Destroy(container);
        if (sr != null) sr.color = corOrig;

        if (!(inimigoCtrl != null && inimigoCtrl.estaMorrendo) && playerTr != null)
            DispararBolaDeFogo();

        carregando = false;
    }

    void DispararBolaDeFogo()
    {
        Vector2 dir = ((Vector2)playerTr.position - (Vector2)pontoDisparo.position).normalized;

        var go = new GameObject("BolaDeFogoMaga");
        go.transform.position = pontoDisparo.position;
        go.AddComponent<BolaDeFogoInimigo>().Inicializar(
            dir, velocidadeProjetil, raioExplosao, danoExplosao,
            duracaoFogo, danoPorTick, intervaloTick);
    }
}
