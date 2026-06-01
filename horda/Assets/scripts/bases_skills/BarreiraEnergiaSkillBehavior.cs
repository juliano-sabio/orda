using System.Collections;
using UnityEngine;

public class BarreiraEnergiaSkillBehavior : SkillBehavior, ISkillComRecarga
{
    float vidaEscudo      = 150f;
    float tempoRecarga    = 12f;
    float escudoMaxAtual  = 150f;

    bool  emRecarga       = false;
    float timerRecarga    = 0f;

    public bool  EmRecarga    => emRecarga;
    public float TimerRecarga => timerRecarga;
    public float RecargaTotal => tempoRecarga;

    // Visual
    GameObject  anelGO;
    LineRenderer lrAnel;
    float        angRot;

    static readonly Color COR_ESCUDO  = new Color(0.2f, 0.6f, 1f, 0.9f);
    static readonly Color COR_QUEBRADO = new Color(0.4f, 0.4f, 0.4f, 0.4f);

    public override void Initialize(PlayerStats stats)
    {
        base.Initialize(stats);
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        vidaEscudo     = data.healthBonus > 0f        ? data.healthBonus     : 150f;
        tempoRecarga   = data.cooldown > 0f            ? data.cooldown        : 12f;
        escudoMaxAtual = vidaEscudo;
    }

    void Start()
    {
        AtivarEscudo();
        CriarVisual();
    }

    void OnDestroy()
    {
        if (anelGO != null) Destroy(anelGO);
        if (playerStats != null) playerStats.shieldPoints = 0f;
    }

    void AtivarEscudo()
    {
        if (playerStats == null) return;
        playerStats.shieldPoints = escudoMaxAtual;
        emRecarga = false;
        timerRecarga = 0f;
    }

    void Update()
    {
        if (playerStats == null) return;

        angRot += Time.deltaTime * 90f;
        AtualizarVisual();

        // Detecta escudo quebrado
        if (!emRecarga && playerStats.shieldPoints <= 0f)
        {
            emRecarga    = true;
            timerRecarga = tempoRecarga;
            StartCoroutine(EfeitoQuebrando());
        }

        // Recarga
        if (emRecarga)
        {
            timerRecarga -= Time.deltaTime;
            if (timerRecarga <= 0f)
            {
                AtivarEscudo();
                StartCoroutine(EfeitoRecuperando());
            }
        }
    }

    // ── Visual ────────────────────────────────────────────────────────────────

    void CriarVisual()
    {
        if (playerStats == null) return;

        anelGO = new GameObject("BarreiraEnergiaAnel");
        anelGO.transform.SetParent(playerStats.transform, false);
        anelGO.transform.localPosition = Vector3.zero;

        lrAnel = anelGO.AddComponent<LineRenderer>();
        lrAnel.useWorldSpace = false;
        lrAnel.loop          = true;
        lrAnel.positionCount = 40;
        lrAnel.material      = new Material(Shader.Find("Sprites/Default"));
        lrAnel.sortingOrder  = 11;

        AtualizarPontosAnel(1f);
    }

    void AtualizarVisual()
    {
        if (lrAnel == null || playerStats == null) return;

        float pct   = emRecarga ? 0f : Mathf.Clamp01(playerStats.shieldPoints / escudoMaxAtual);
        float pulso = Mathf.Sin(Time.time * 5f) * 0.5f + 0.5f;

        // Cor varia com HP do escudo
        Color cor = emRecarga
            ? new Color(COR_QUEBRADO.r, COR_QUEBRADO.g, COR_QUEBRADO.b, 0.15f + pulso * 0.1f)
            : new Color(
                Mathf.Lerp(1f, COR_ESCUDO.r, pct),
                Mathf.Lerp(0.1f, COR_ESCUDO.g, pct),
                Mathf.Lerp(0.1f, COR_ESCUDO.b, pct),
                0.5f + pulso * 0.35f + pct * 0.15f);

        lrAnel.startColor = lrAnel.endColor = cor;
        lrAnel.startWidth = lrAnel.endWidth = emRecarga
            ? 0.04f
            : Mathf.Lerp(0.05f, 0.14f, pct) + pulso * 0.04f;

        // Rotaciona o anel
        if (anelGO != null)
            anelGO.transform.localRotation = Quaternion.Euler(0f, 0f, angRot);
    }

    void AtualizarPontosAnel(float raio)
    {
        if (lrAnel == null) return;
        const int SEGS = 40;
        for (int i = 0; i < SEGS; i++)
        {
            float ang = 360f / SEGS * i * Mathf.Deg2Rad;
            lrAnel.SetPosition(i, new Vector3(Mathf.Cos(ang) * raio, Mathf.Sin(ang) * raio, 0f));
        }
    }

    IEnumerator EfeitoQuebrando()
    {
        if (playerStats == null) yield break;

        // Flash branco + câmera shake
        var sr = playerStats.GetComponent<SpriteRenderer>();
        CameraShaker.Tremer(0.15f, 0.25f);

        // Fragmentos do escudo voando
        for (int i = 0; i < 10; i++)
        {
            float ang = i / 10f * Mathf.PI * 2f;
            var go = new GameObject("FragBarreira");
            go.transform.position = (Vector2)playerStats.transform.position
                + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 1f;
            var fsr = go.AddComponent<SpriteRenderer>();
            fsr.sprite = GerarDisco(6);
            fsr.color  = new Color(0.3f, 0.6f, 1f);
            fsr.sortingOrder = 12;
            go.transform.localScale = Vector3.one * Random.Range(0.1f, 0.2f);
            Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(2f, 5f);
            StartCoroutine(AnimarFragmento(fsr, vel));
        }

        // Pisca vermelho
        if (sr != null)
        {
            sr.color = new Color(0.3f, 0.6f, 1f);
            yield return new WaitForSeconds(0.1f);
            if (sr != null) sr.color = Color.white;
        }

        // Anel some gradualmente
        if (lrAnel != null)
        {
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                if (lrAnel == null) yield break;
                Color c = lrAnel.startColor;
                c.a = Mathf.Lerp(0.5f, 0f, t / 0.3f);
                lrAnel.startColor = lrAnel.endColor = c;
                yield return null;
            }
        }
    }

    IEnumerator EfeitoRecuperando()
    {
        if (playerStats == null) yield break;

        // Anel aparece gradualmente
        AtualizarPontosAnel(1f);

        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            if (lrAnel == null) yield break;
            lrAnel.startColor = lrAnel.endColor =
                new Color(COR_ESCUDO.r, COR_ESCUDO.g, COR_ESCUDO.b, Mathf.Lerp(0f, 0.9f, t / 0.5f));
            lrAnel.startWidth = lrAnel.endWidth = Mathf.Lerp(0.3f, 0.12f, t / 0.5f);
            yield return null;
        }

        // Anel de pulso dourado indicando recuperação
        const int SEGS = 40;
        var go = new GameObject("PulsoBarreira");
        go.transform.position = playerStats.transform.position;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = SEGS;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 10;

        float dur = 0.5f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / dur;
            float r = Mathf.Lerp(0.2f, 2.5f, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.2f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(0.2f, 0.6f, 1f, Mathf.Lerp(1f, 0f, p));
            for (int i = 0; i < SEGS; i++)
            {
                float ang = 360f / SEGS * i * Mathf.Deg2Rad;
                lr.SetPosition(i, (Vector2)playerStats.transform.position
                    + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator AnimarFragmento(SpriteRenderer fsr, Vector2 vel)
    {
        Color cor = fsr.color;
        float vida = Random.Range(0.3f, 0.6f);
        for (float t = 0f; t < vida; t += Time.deltaTime)
        {
            vel *= Mathf.Pow(0.85f, Time.deltaTime * 60f);
            if (fsr != null)
            {
                fsr.transform.position += (Vector3)(vel * Time.deltaTime);
                fsr.color = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(1f, 0f, t / vida));
            }
            yield return null;
        }
        if (fsr != null) Destroy(fsr.gameObject);
    }

    public override void ApplyEffect() { }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, d < cx ? 1f : 0f));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
