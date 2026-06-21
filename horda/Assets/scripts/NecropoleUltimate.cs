using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NecropoleUltimate : MonoBehaviour, IUltimateCosmetico
{
    bool cosmetico;
    public void ExecutarCosmetico() { if (ativo) return; cosmetico = true; StartCoroutine(CorotinaAtivacao()); }

    [Header("Configurações")]
    public float raio               = 12f;
    public float duracao            = 10f;
    public float cooldown           = 30f;
    public float duracaoFantasma    = 6f;
    public float danoFantasma       = 20f;
    public float velocidadeFantasma = 3.5f;
    public float intervaloAtaque    = 1.1f;

    public float CooldownRestante => cooldownRestante;
    public bool  Ativo            => ativo;

    float       cooldownRestante;
    bool        ativo;
    PlayerStats playerStats;

    // Paleta necromantica
    static readonly Color COR_ROXO      = new Color(0.50f, 0.00f, 0.85f);
    static readonly Color COR_ROXO_VIVO = new Color(0.72f, 0.20f, 1.00f);
    static readonly Color COR_VERDE_F   = new Color(0.28f, 1.00f, 0.52f);
    static readonly Color COR_ESCURO    = new Color(0.15f, 0.00f, 0.28f);

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            if (playerStats.ultimateSkill != null)
                playerStats.ultimateSkill.isActive = true;
            playerStats.ultimateCooldown   = cooldown;
            playerStats.ultimateChargeTime = 0f;
            playerStats.ultimateReady      = false;
        }
    }

    void Update()
    {
        if (cooldownRestante > 0f) cooldownRestante -= Time.deltaTime;
        if (playerStats != null && playerStats.IsLocalAuthority &&
            InputBindings.UltimateDown() && cooldownRestante <= 0f && !ativo)
            StartCoroutine(CorotinaAtivacao());
        SincronizarUI();
    }

    void SincronizarUI()
    {
        if (playerStats == null) return;
        playerStats.ultimateChargeTime = cooldown - Mathf.Max(0f, cooldownRestante);
        playerStats.ultimateReady      = cooldownRestante <= 0f && !ativo;
    }

    // ─── COROUTINE PRINCIPAL ────────────────────────────────────────────────

    IEnumerator CorotinaAtivacao()
    {
        ativo            = true;
        cooldownRestante = cooldown;

        InimigoController.OnPreMorte += OnInimigoDentroZona;

        // Cena de invocação antes de criar a zona
        yield return StartCoroutine(CenaInvocacao());

        var zonaGO = CriarZona();
        StartCoroutine(ParticulasContínuas(zonaGO));
        yield return StartCoroutine(AnimarEntrada(zonaGO));

        float elapsed = 0f;
        while (elapsed < duracao)
        {
            elapsed += Time.deltaTime;
            AnimarZona(zonaGO, elapsed);
            zonaGO.transform.position = transform.position;
            yield return null;
        }

        InimigoController.OnPreMorte -= OnInimigoDentroZona;
        ativo = false;
        StartCoroutine(FadeOutDestruir(zonaGO, 0.5f));
    }

    void OnInimigoDentroZona(InimigoController ic)
    {
        if (ic == null || ic.gameObject == null) return;
        float dist = Vector2.Distance(transform.position, ic.transform.position);
        if (dist > raio) return;

        Vector2 pos = ic.transform.position;
        StartCoroutine(SpawnarFantasma(pos));

        if (SkillEvolutionManager.Tem(SkillEvolutionType.NecropoleContaminacao))
            StartCoroutine(ContaminarVizinhos(pos));
    }

    IEnumerator ContaminarVizinhos(Vector2 pos)
    {
        float dur = 2f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            foreach (var c in Physics2D.OverlapCircleAll(pos, 3f))
            {
                var icViz = c.GetComponent<InimigoController>() ?? c.GetComponentInParent<InimigoController>();
                if (icViz != null && !cosmetico) icViz.ReceberDano(8f * Time.deltaTime, false, false);
            }
            yield return null;
        }
    }

    // ─── CENA DE INVOCAÇÃO ──────────────────────────────────────────────────

    IEnumerator CenaInvocacao()
    {
        Vector2 pos = transform.position;

        // Flash roxo que expande
        var flash = new GameObject("FlashNecro");
        flash.transform.position = pos;
        var fsr = flash.AddComponent<SpriteRenderer>();
        fsr.sprite = GerarDisco(32); fsr.sortingOrder = 25;
        fsr.color  = new Color(COR_ROXO_VIVO.r, COR_ROXO_VIVO.g, COR_ROXO_VIVO.b, 0.85f);
        flash.transform.localScale = Vector3.one * 0.5f;
        yield return null;

        float durFlash = 0.4f;
        for (float t = 0f; t < durFlash; t += Time.deltaTime)
        {
            float p = t / durFlash;
            flash.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, raio * 2.2f, Mathf.Sqrt(p));
            fsr.color = new Color(COR_ROXO_VIVO.r, COR_ROXO_VIVO.g, COR_ROXO_VIVO.b, Mathf.Lerp(0.7f, 0f, p));
            yield return null;
        }
        Destroy(flash);

        // Anel de choque saindo do centro
        StartCoroutine(AnelChoque(pos, raio * 1.15f, 0.55f, COR_ROXO_VIVO, 44));

        // 14 fragmentos de energia voando para fora
        for (int i = 0; i < 14; i++)
        {
            float ang   = i / 14f * Mathf.PI * 2f + Random.Range(-0.1f, 0.1f);
            float speed = Random.Range(raio * 1.6f, raio * 2.8f);
            Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed;

            var p = new GameObject("FragInvoc");
            p.transform.position = pos;
            p.transform.localScale = Vector3.one * Random.Range(0.08f, 0.2f);
            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite = i % 3 == 0 ? GerarCruz() : GerarDisco(8);
            psr.color  = i % 4 == 0 ? COR_VERDE_F : COR_ROXO_VIVO;
            psr.sortingOrder = 14;
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(vel, Random.Range(0.3f, 0.55f));
            Destroy(p, 0.7f);
        }
    }

    // ─── ZONA — CRIAÇÃO ─────────────────────────────────────────────────────

    GameObject CriarZona()
    {
        var root = new GameObject("ZonaNecropole");
        root.transform.position = transform.position;

        // Disco de névoa no chão
        var disco = new GameObject("DiscoNevoa");
        disco.transform.SetParent(root.transform, false);
        var srD = disco.AddComponent<SpriteRenderer>();
        srD.sprite = GerarDisco(32); srD.sortingOrder = 8;
        srD.color  = new Color(COR_ESCURO.r, COR_ESCURO.g, COR_ESCURO.b, 0f);
        disco.transform.localScale = Vector3.one * raio * 2f;

        // 3 anéis (externo, médio, interno)
        CriarAnelLR(root, raio,         new Color(COR_ROXO.r,      COR_ROXO.g,      COR_ROXO.b,      0f), 0.12f, "AnelExterno", 52);
        CriarAnelLR(root, raio * 0.70f, new Color(COR_ROXO_VIVO.r, COR_ROXO_VIVO.g, COR_ROXO_VIVO.b, 0f), 0.08f, "AnelMedio",   38);
        CriarAnelLR(root, raio * 0.38f, new Color(COR_VERDE_F.r,   COR_VERDE_F.g,   COR_VERDE_F.b,   0f), 0.05f, "AnelInterno", 24);

        // 8 orbes orbitando (alternando roxo e verde)
        for (int i = 0; i < 8; i++)
        {
            bool verde = i % 4 == 0;
            var p  = new GameObject($"Orbe{i}");
            p.transform.SetParent(root.transform, false);
            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = GerarDisco(verde ? 9 : 7);
            sr.color  = new Color(verde ? COR_VERDE_F.r   : COR_ROXO_VIVO.r,
                                  verde ? COR_VERDE_F.g   : COR_ROXO_VIVO.g,
                                  verde ? COR_VERDE_F.b   : COR_ROXO_VIVO.b, 0f);
            sr.sortingOrder = 13;
            p.transform.localScale = Vector3.one * (verde ? 0.2f : 0.14f);
            float ang = (360f / 8f) * i * Mathf.Deg2Rad;
            p.transform.localPosition = new Vector3(Mathf.Cos(ang) * raio * 0.85f, Mathf.Sin(ang) * raio * 0.85f);
        }

        // 4 runas nas diagonais
        for (int i = 0; i < 4; i++)
        {
            float ang = (45f + i * 90f) * Mathf.Deg2Rad;
            var r  = new GameObject($"Runa{i}");
            r.transform.SetParent(root.transform, false);
            r.transform.localScale    = Vector3.one * 0.25f;
            r.transform.localPosition = new Vector3(Mathf.Cos(ang) * raio * 0.52f, Mathf.Sin(ang) * raio * 0.52f);
            var rsr = r.AddComponent<SpriteRenderer>();
            rsr.sprite = GerarCruz(); rsr.sortingOrder = 12;
            rsr.color  = new Color(COR_ROXO_VIVO.r, COR_ROXO_VIVO.g, COR_ROXO_VIVO.b, 0f);
        }

        return root;
    }

    void CriarAnelLR(GameObject parent, float r, Color cor, float largura, string nome, int segs)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false; lr.loop = true; lr.positionCount = segs;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 11; lr.startWidth = lr.endWidth = largura;
        lr.startColor   = lr.endColor = cor;
        for (int i = 0; i < segs; i++)
        {
            float a = (360f / segs) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
        }
    }

    // ─── ZONA — ANIMAÇÃO ────────────────────────────────────────────────────

    IEnumerator AnimarEntrada(GameObject root)
    {
        var lrs = root.GetComponentsInChildren<LineRenderer>();
        var srs = root.GetComponentsInChildren<SpriteRenderer>();

        // Guarda raios originais dos anéis para expansão
        var raiosOrig = new float[lrs.Length];
        for (int i = 0; i < lrs.Length; i++)
            raiosOrig[i] = lrs[i].positionCount > 0 ? lrs[i].GetPosition(0).magnitude : raio;

        float dur = 0.45f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p  = t / dur;
            float pe = 1f - Mathf.Pow(1f - p, 2f);

            // Anéis expandem do centro
            for (int li = 0; li < lrs.Length; li++)
            {
                float r = Mathf.Lerp(0.1f, raiosOrig[li], pe);
                int   n = lrs[li].positionCount;
                for (int i = 0; i < n; i++)
                {
                    float a = (360f / n) * i * Mathf.Deg2Rad;
                    lrs[li].SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
                }
                Color c = lrs[li].startColor; c.a = pe * 0.85f; lrs[li].startColor = lrs[li].endColor = c;
            }

            foreach (var sr in srs)
            {
                Color c = sr.color;
                c.a = sr.gameObject.name == "DiscoNevoa" ? pe * 0.18f : pe * 0.72f;
                sr.color = c;
            }
            yield return null;
        }
    }

    void AnimarZona(GameObject root, float elapsed)
    {
        float pulso  = Mathf.Sin(elapsed * 3.8f) * 0.5f + 0.5f;
        float pulso2 = Mathf.Sin(elapsed * 2.1f + 1.3f) * 0.5f + 0.5f;

        // Anéis giram em velocidades e direções diferentes
        var lrs = root.GetComponentsInChildren<LineRenderer>();
        float[] vels = { 18f, -28f, 50f };
        for (int li = 0; li < lrs.Length && li < vels.Length; li++)
        {
            RotarAnel(lrs[li], elapsed * vels[li]);
            float[] alphas = { 0.38f + pulso * 0.35f, 0.42f + pulso * 0.38f, 0.35f + pulso2 * 0.5f };
            Color c = lrs[li].startColor; c.a = alphas[li]; lrs[li].startColor = lrs[li].endColor = c;
        }

        var srs = root.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in srs)
        {
            string nome = sr.gameObject.name;
            if (nome == "DiscoNevoa")
            {
                sr.color = new Color(COR_ESCURO.r, COR_ESCURO.g, COR_ESCURO.b, 0.14f + pulso2 * 0.11f);
                sr.transform.localScale = Vector3.one * (raio * 2f + pulso2 * raio * 0.06f);
            }
            else if (nome.StartsWith("Orbe") && int.TryParse(nome.Substring(4), out int idx))
            {
                bool verde    = idx % 4 == 0;
                float angRad  = ((360f / 8f) * idx + elapsed * (verde ? 33f : 52f)) * Mathf.Deg2Rad;
                float r       = raio * 0.85f + Mathf.Sin(elapsed * 1.8f + idx) * raio * 0.055f;
                sr.transform.localPosition = new Vector3(Mathf.Cos(angRad) * r, Mathf.Sin(angRad) * r);
                Color c = sr.color; c.a = 0.5f + (verde ? pulso2 : pulso) * 0.45f; sr.color = c;
                // Pulso de escala nos orbes
                float esc = verde ? 0.2f : 0.14f;
                sr.transform.localScale = Vector3.one * (esc * (0.85f + (verde ? pulso2 : pulso) * 0.3f));
            }
            else if (nome.StartsWith("Runa"))
            {
                Color c = sr.color; c.a = 0.30f + pulso * 0.5f; sr.color = c;
                sr.transform.Rotate(0f, 0f, 38f * Time.deltaTime);
            }
        }
    }

    void RotarAnel(LineRenderer lr, float angOffset)
    {
        if (lr.positionCount < 2) return;
        float r = lr.GetPosition(0).magnitude;
        if (r < 0.01f) return;
        int n = lr.positionCount;
        for (int i = 0; i < n; i++)
        {
            float a = ((360f / n) * i + angOffset) * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
        }
    }

    IEnumerator ParticulasContínuas(GameObject zonaGO)
    {
        int frame = 0;
        while (zonaGO != null)
        {
            frame++;
            Vector2 centro = zonaGO.transform.position;

            // Partícula subindo do interior da zona a cada 4 frames
            if (frame % 4 == 0)
            {
                Vector2 offset = Random.insideUnitCircle * raio * 0.88f;
                SpawnParticulaSubindo(centro + offset);
            }

            // Névoa saindo da borda a cada 14 frames
            if (frame % 14 == 0)
            {
                float ang   = Random.Range(0f, Mathf.PI * 2f);
                Vector2 borda = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio;
                var go = new GameObject("Nevoa");
                go.transform.position = borda;
                go.transform.localScale = Vector3.one * Random.Range(0.35f, 0.75f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = GerarDisco(16); sr.sortingOrder = 9;
                sr.color  = new Color(COR_ROXO.r, COR_ROXO.g, COR_ROXO.b, 0.22f);
                go.AddComponent<AutoDestroyFadeMove>().Iniciar(
                    -new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(0.25f, 0.7f)
                    + Vector2.up * 0.25f, Random.Range(0.9f, 1.5f));
                Destroy(go, 1.9f);
            }
            yield return null;
        }
    }

    void SpawnParticulaSubindo(Vector2 pos)
    {
        var go = new GameObject("PartNecro");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * Random.Range(0.04f, 0.13f);
        var sr = go.AddComponent<SpriteRenderer>();
        bool verde = Random.value < 0.28f;
        sr.sprite = GerarDisco(8); sr.sortingOrder = 10;
        sr.color  = verde
            ? new Color(COR_VERDE_F.r,   COR_VERDE_F.g,   COR_VERDE_F.b,   0.8f)
            : new Color(COR_ROXO_VIVO.r, COR_ROXO_VIVO.g, COR_ROXO_VIVO.b, 0.7f);
        go.AddComponent<AutoDestroyFadeMove>().Iniciar(
            Vector2.up * Random.Range(0.9f, 2.2f) + Random.insideUnitCircle * 0.3f,
            Random.Range(0.5f, 1.3f));
        Destroy(go, 1.6f);
    }

    IEnumerator FadeOutDestruir(GameObject go, float dur)
    {
        var lrs = go.GetComponentsInChildren<LineRenderer>();
        var srs = go.GetComponentsInChildren<SpriteRenderer>();
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            foreach (var lr in lrs) { Color c = lr.startColor; c.a = Mathf.Lerp(c.a, 0f, p); lr.startColor = lr.endColor = c; }
            foreach (var sr in srs) { Color c = sr.color;      c.a = Mathf.Lerp(c.a, 0f, p); sr.color = c; }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    // ─── FANTASMA ALIADO ────────────────────────────────────────────────────

    IEnumerator SpawnarFantasma(Vector2 pos)
    {
        var go = new GameObject("FantasmaAliado");
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarFantasma();
        sr.color        = new Color(COR_ROXO_VIVO.r, COR_ROXO_VIVO.g, COR_ROXO_VIVO.b, 0f);
        sr.sortingOrder = 12;
        go.transform.localScale = Vector3.one * 0.62f;

        // Olhos verdes brilhantes
        CriarOlho(go, new Vector2(-0.14f,  0.08f));
        CriarOlho(go, new Vector2( 0.14f,  0.08f));

        // Aura pulsante em coroutine própria
        StartCoroutine(AuraFantasmaLoop(go));

        // Entrada com partículas
        yield return StartCoroutine(EntradaFantasma(go, sr, pos));

        float duracaoEfetiva = duracaoFantasma;
        float velEfetiva     = velocidadeFantasma;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.NecropoleExercito))
        {
            duracaoEfetiva += 3f;
            velEfetiva     *= 1.5f;
        }

        float vida        = duracaoEfetiva;
        float timerAtaque = 0f;
        float tempoBob    = Random.Range(0f, Mathf.PI * 2f);
        int   frame       = 0;

        while (vida > 0f && go != null)
        {
            vida       -= Time.deltaTime;
            timerAtaque -= Time.deltaTime;
            tempoBob    += Time.deltaTime;
            frame++;

            // Flutuação suave
            go.transform.position = (Vector2)go.transform.position
                + Vector2.up * Mathf.Sin(tempoBob * 3.5f) * 0.004f;

            // Trail de partículas a cada 5 frames
            if (frame % 5 == 0 && go != null)
            {
                var tp = new GameObject("TrailF");
                tp.transform.position = go.transform.position + (Vector3)(Random.insideUnitCircle * 0.12f);
                tp.transform.localScale = Vector3.one * Random.Range(0.05f, 0.11f);
                var tsr = tp.AddComponent<SpriteRenderer>();
                tsr.sprite = GerarDisco(6); tsr.sortingOrder = 11;
                tsr.color  = new Color(COR_ROXO_VIVO.r, COR_ROXO_VIVO.g, COR_ROXO_VIVO.b, 0.45f);
                tp.AddComponent<AutoDestroyFadeMove>().Iniciar(Vector2.zero, 0.28f);
                Destroy(tp, 0.4f);
            }

            // Perseguição e ataque
            var alvo = EncontrarInimigoMaisProximo(go.transform.position);
            if (alvo != null)
            {
                Vector2 dir = ((Vector2)alvo.transform.position - (Vector2)go.transform.position).normalized;
                go.transform.position = (Vector2)go.transform.position + dir * velEfetiva * Time.deltaTime;

                if (timerAtaque <= 0f && Vector2.Distance(go.transform.position, alvo.transform.position) < 1.2f)
                {
                    timerAtaque = intervaloAtaque;
                    if (!cosmetico) alvo.ReceberDano(danoFantasma, Random.value < 0.15f);
                    StartCoroutine(AtaqueFantasmaFX(sr, (Vector2)alvo.transform.position));
                }
            }

            // Fade out no fim da vida
            if (vida < 1f)
            {
                Color c = sr.color; c.a = Mathf.Lerp(0f, 0.78f, vida); sr.color = c;
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator EntradaFantasma(GameObject go, SpriteRenderer sr, Vector2 pos)
    {
        // Partículas explodindo do ponto de spawn
        for (int i = 0; i < 8; i++)
        {
            float ang = i / 8f * Mathf.PI * 2f;
            var p = new GameObject("SpawnPart");
            p.transform.position = pos;
            p.transform.localScale = Vector3.one * Random.Range(0.06f, 0.13f);
            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite = GerarDisco(6); psr.sortingOrder = 13;
            psr.color  = COR_ROXO_VIVO;
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(
                new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(1.2f, 2.5f), 0.35f);
            Destroy(p, 0.55f);
        }

        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / 0.4f;
            go.transform.position = pos + Vector2.up * Mathf.Lerp(-0.65f, 0f, p);
            Color c = sr.color; c.a = Mathf.Lerp(0f, 0.78f, p); sr.color = c;
            yield return null;
        }
    }

    GameObject CriarOlho(GameObject parent, Vector2 offset)
    {
        var go = new GameObject("Olho");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = offset;
        go.transform.localScale    = Vector3.one * 0.19f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GerarDisco(8); sr.sortingOrder = 14;
        sr.color  = new Color(COR_VERDE_F.r, COR_VERDE_F.g, COR_VERDE_F.b, 0.95f);
        StartCoroutine(OlhoPulso(sr));
        return go;
    }

    IEnumerator OlhoPulso(SpriteRenderer sr)
    {
        while (sr != null)
        {
            float a = Mathf.Sin(Time.time * 5.5f) * 0.15f + 0.85f;
            if (sr != null) sr.color = new Color(COR_VERDE_F.r, COR_VERDE_F.g, COR_VERDE_F.b, a);
            yield return null;
        }
    }

    IEnumerator AuraFantasmaLoop(GameObject go)
    {
        const int S = 20;
        var auraGO = new GameObject("AuraF");
        auraGO.transform.SetParent(go.transform, false);
        var lr = auraGO.AddComponent<LineRenderer>();
        lr.useWorldSpace = false; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 11;

        while (go != null && auraGO != null)
        {
            float t = Time.time;
            float pulso = Mathf.Sin(t * 6.5f) * 0.5f + 0.5f;
            float r = 0.4f + pulso * 0.07f;
            Color cor = Color.Lerp(COR_ROXO_VIVO, COR_VERDE_F, Mathf.Sin(t * 1.8f) * 0.5f + 0.5f);
            lr.startWidth = lr.endWidth = 0.04f + pulso * 0.04f;
            lr.startColor = lr.endColor = new Color(cor.r, cor.g, cor.b, 0.55f + pulso * 0.4f);
            for (int i = 0; i < S; i++)
            {
                float a = (360f / S) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
            }
            yield return null;
        }
    }

    IEnumerator AtaqueFantasmaFX(SpriteRenderer sr, Vector2 posAlvo)
    {
        // Flash violeta no fantasma
        if (sr != null)
        {
            Color orig = sr.color;
            sr.color = new Color(1f, 0.55f, 1f, 1f);
            yield return new WaitForSeconds(0.07f);
            if (sr != null) sr.color = orig;
        }

        // Anel de impacto no alvo
        StartCoroutine(AnelChoque(posAlvo, 1.3f, 0.22f, COR_ROXO_VIVO, 18));

        // Faíscas no ponto de golpe
        for (int i = 0; i < 6; i++)
        {
            float ang = i / 6f * Mathf.PI * 2f;
            var p = new GameObject("GolpePart");
            p.transform.position = posAlvo;
            p.transform.localScale = Vector3.one * Random.Range(0.05f, 0.11f);
            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite = GerarDisco(6); psr.sortingOrder = 15;
            psr.color  = i % 2 == 0 ? COR_ROXO_VIVO : COR_VERDE_F;
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(
                new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(1.5f, 3.2f), 0.22f);
            Destroy(p, 0.38f);
        }
    }

    InimigoController EncontrarInimigoMaisProximo(Vector3 pos)
    {
        InimigoController melhor = null;
        float distMin = float.MaxValue;
        foreach (var ic in FindObjectsByType<InimigoController>(FindObjectsSortMode.None))
        {
            if (ic == null || ic.gameObject == null) continue;
            if (!ic.enabled) continue;
            if (ic.GetComponentInParent<ProjetilHomingPrincesa>(true)   != null) continue;
            if (ic.GetComponentInParent<ProjetilEspecialPrincesa>(true) != null) continue;
            float d = Vector3.Distance(pos, ic.transform.position);
            if (d < distMin) { distMin = d; melhor = ic; }
        }
        return melhor;
    }

    // ─── HELPERS VISUAIS ─────────────────────────────────────────────────────

    IEnumerator AnelChoque(Vector2 pos, float raioMax, float dur, Color cor, int segs)
    {
        var go = new GameObject("AnelChoque");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = segs;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 17;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p  = t / dur;
            float pe = 1f - Mathf.Pow(1f - p, 2f);
            float r  = Mathf.Lerp(0.05f, raioMax, pe);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.35f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(cor.r, cor.g, cor.b, 1f - p);
            for (int i = 0; i < segs; i++)
            {
                float a = (360f / segs) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            yield return null;
        }
        Destroy(go);
    }

    // ─── SPRITES PROCEDURAIS ─────────────────────────────────────────────────

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, d < cx ? 1f : 0f));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }

    // Forma de fantasma: oval na cabeça afunilando numa cauda ondulada
    static Sprite GerarFantasma()
    {
        const int W = 14, H = 22;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = W * 0.5f;
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
        {
            float nx = Mathf.Abs(x + 0.5f - cx) / cx;
            float ny = y / (float)(H - 1);
            float corpo;
            if (ny > 0.45f)
                corpo = Mathf.Clamp01((1f - nx) * (ny - 0.45f) * 5f * (1f - nx * 0.5f));
            else
            {
                // Cauda ondulada
                float onda = Mathf.Sin(ny * Mathf.PI * 4f) * 0.15f;
                corpo = Mathf.Clamp01((1f - Mathf.Abs(nx + onda) * 2.2f) * ny * 3f);
            }
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(corpo * 1.6f)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.2f), W);
    }

    // Cruz (runa) 4 braços
    static Sprite GerarCruz()
    {
        const int SZ = 10;
        var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        float cx = SZ * 0.5f;
        for (int y = 0; y < SZ; y++) for (int x = 0; x < SZ; x++)
        {
            float nx = Mathf.Abs(x + 0.5f - cx) / cx;
            float ny = Mathf.Abs(y + 0.5f - cx) / cx;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, (nx < 0.22f || ny < 0.22f) ? 1f : 0f));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, SZ, SZ), new Vector2(0.5f, 0.5f), SZ);
    }

    // Mantido para compatibilidade
    static Sprite GerarSprite(int sz, Color cor)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            tex.SetPixel(x, y, new Color(cor.r, cor.g, cor.b, Mathf.Clamp01(1f - d / cx)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
