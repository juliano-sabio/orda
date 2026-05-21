using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class ProjetilEspecialBoss : MonoBehaviour
{
    public float velocidade = 5.5f;
    public float tempoMaximoVida = 6f;
    public float dano = 45f;
    public float duracaoVisao = 3f;
    [Tooltip("Raio visível ao redor do player em pixels na tela")]
    public float raioVisaoTela = 4f;

    [Tooltip("Tempo em segundos até dividir em dois (0 = não divide)")]
    public float tempoDivisao = 2f;
    [HideInInspector] public bool podeDividir = true;

    private Rigidbody2D rb;
    private bool jaAcertou;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!TryGetComponent<Light2D>(out Light2D luz))
            luz = gameObject.AddComponent<Light2D>();
        luz.lightType = Light2D.LightType.Point;
        luz.intensity = 2.5f;
        luz.pointLightOuterRadius = 3.5f;
        luz.pointLightInnerRadius = 1f;
    }

    void Start()
    {
        if (TryGetComponent<Light2D>(out var l))
            l.color = new Color(0.7f, 0f, 1f);
        Destroy(gameObject, tempoMaximoVida);
        if (podeDividir && tempoDivisao > 0f)
            StartCoroutine(Dividir());
    }

    public void SetDirecao(Vector2 dir)
    {
        if (rb != null)
            rb.linearVelocity = dir.normalized * velocidade;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, ang);
    }

    IEnumerator Dividir()
    {
        yield return new WaitForSeconds(tempoDivisao);
        if (jaAcertou) yield break;

        Vector2 dirAtual = rb != null ? rb.linearVelocity.normalized : transform.right;
        float anguloBase = Mathf.Atan2(dirAtual.y, dirAtual.x) * Mathf.Rad2Deg;

        for (int i = 0; i < 2; i++)
        {
            float offset = i == 0 ? 25f : -25f;
            float rad = (anguloBase + offset) * Mathf.Deg2Rad;
            Vector2 novaDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            GameObject filho = Instantiate(gameObject, transform.position, Quaternion.identity);
            ProjetilEspecialBoss p = filho.GetComponent<ProjetilEspecialBoss>();
            if (p != null)
            {
                p.podeDividir = false;
                p.SetDirecao(novaDir);
            }
        }

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (jaAcertou) return;

        if (other.CompareTag("Player"))
        {
            jaAcertou = true;
            PlayerStats stats = other.GetComponent<PlayerStats>();
            if (stats != null) stats.TakeDamage(dano);
            AplicarVisaoReduzida(duracaoVisao, raioVisaoTela);
            Destroy(gameObject);
        }
        else if (other.gameObject.tag == "Chao" || other.gameObject.tag == "Obstacles")
        {
            Destroy(gameObject);
        }
    }

    static void AplicarVisaoReduzida(float duracao, float raioTela)
    {
        GameObject go = new GameObject("VisaoRunner");
        go.AddComponent<VisaoEscuridaoRunner>().Iniciar(duracao, raioTela);
    }
}

public class VisaoEscuridaoRunner : MonoBehaviour
{
    public void Iniciar(float duracao, float raioTela) => StartCoroutine(Efeito(duracao, raioTela));

    IEnumerator Efeito(float duracao, float raioTela)
    {
        // ── Gera textura: buraco circular transparente no centro ──────
        int sz = 512;
        float raioTex  = sz * 0.10f;   // raio visível na textura
        float softTex  = sz * 0.05f;   // suavidade da borda

        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float c = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
            float a = Mathf.Clamp01((d - raioTex) / softTex);
            tex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
        }
        tex.Apply();

        // ── Canvas ────────────────────────────────────────────────────
        GameObject cvGO = new GameObject("VisaoCanvas");
        Canvas cv = cvGO.AddComponent<Canvas>();
        cv.renderMode  = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 999;

        // ── RawImage: escala para que raioTex corresponda a raioTela px
        GameObject imgGO = new GameObject("Overlay");
        imgGO.transform.SetParent(cvGO.transform, false);
        RawImage img = imgGO.AddComponent<RawImage>();
        img.texture      = tex;
        img.raycastTarget = false;

        RectTransform rt = img.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 0.5f);

        // Tamanho: escala pelo raio, depois multiplica para cobrir a tela toda
        float escala = raioTela / raioTex;
        float diagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
        float lado = Mathf.Max(sz * escala * 2.5f, diagonal * 2f);
        rt.sizeDelta = new Vector2(lado, lado);

        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // ── Fade in ───────────────────────────────────────────────────
        CanvasGroup cg = cvGO.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, t / 0.25f);
            SeguirPlayer(rt, player);
            yield return null;
        }
        cg.alpha = 1f;

        // ── Duração: segue o player ───────────────────────────────────
        t = 0f;
        while (t < duracao)
        {
            t += Time.deltaTime;
            SeguirPlayer(rt, player);
            yield return null;
        }

        // ── Fade out ──────────────────────────────────────────────────
        t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, t / 0.5f);
            SeguirPlayer(rt, player);
            yield return null;
        }

        Destroy(cvGO);
        Destroy(gameObject);
    }

    static void SeguirPlayer(RectTransform rt, Transform player)
    {
        if (player == null || Camera.main == null) return;
        Vector3 sp = Camera.main.WorldToScreenPoint(player.position);
        rt.position = new Vector3(sp.x, sp.y, 0f);
    }
}
