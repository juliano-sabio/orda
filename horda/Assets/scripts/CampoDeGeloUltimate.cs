using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CampoDeGeloUltimate : MonoBehaviour
{
    [Header("Configurações")]
    public float raio          = 6f;
    public float duracao       = 4f;    // tempo de viagem
    public float duracaoVisual = 8f;    // tempo total do efeito (viagem + congelados parados)
    public float cooldown      = 25f;
    public float velocidade    = 8f;    // unidades/s do campo viajando
    public float raioDeteccao  = 25f;   // raio de busca do inimigo alvo

    public float CooldownRestante => cooldownRestante;
    public bool  Ativo            => ativo;

    private float       cooldownRestante;
    private bool        ativo;
    private PlayerStats playerStats;

    struct EstadoInimigo
    {
        public GameObject          go;
        public List<MonoBehaviour> scripts;
        public Rigidbody2D         rb;
        public SpriteRenderer[]    renderers;
        public Color[]             coresOriginais;
        public GameObject          efeitoVFX;
    }

    readonly List<EstadoInimigo> congelados = new List<EstadoInimigo>();

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
        if (Input.GetKeyDown(KeyCode.R) && cooldownRestante <= 0f && !ativo)
            StartCoroutine(CorotinaAtivacao());
        SincronizarUI();
    }

    void FixedUpdate()
    {
        if (!ativo) return;
        foreach (var e in congelados)
            if (e.rb != null) e.rb.linearVelocity = Vector2.zero;
    }

    void SincronizarUI()
    {
        if (playerStats == null) return;
        playerStats.ultimateChargeTime = cooldown - Mathf.Max(0f, cooldownRestante);
        playerStats.ultimateReady      = cooldownRestante <= 0f && !ativo;
    }

    IEnumerator CorotinaAtivacao()
    {
        ativo            = true;
        cooldownRestante = cooldown;

        Vector2 posicaoCampo = transform.position;
        Vector2 direcao      = EncontrarDirecaoInimigo(posicaoCampo);

        // GeloEterno: +4s de duração total
        float duracaoVisualEfetiva = duracaoVisual;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.GeloEterno))
            duracaoVisualEfetiva += 4f;

        var vfx   = CriarVisual(posicaoCampo);
        float elapsed = 0f;

        // Fase de viagem: campo se move em direção ao inimigo congelando o caminho
        while (elapsed < duracao)
        {
            elapsed += Time.deltaTime;
            Vector2 novaPosicao = posicaoCampo + direcao * velocidade * Time.deltaTime;
            AtualizarCongelados(posicaoCampo, novaPosicao);
            posicaoCampo = novaPosicao;
            if (vfx != null) vfx.transform.position = posicaoCampo;
            yield return null;
        }

        // Fase pós-viagem: campo parado, mas continua congelando quem entrar
        float posElapsed = 0f;
        float posExtra   = Mathf.Max(0f, duracaoVisualEfetiva - duracao);
        while (posElapsed < posExtra)
        {
            posElapsed += Time.deltaTime;
            AtualizarCongelados(posicaoCampo, posicaoCampo);
            foreach (var e in congelados)
                if (e.rb != null) e.rb.linearVelocity = Vector2.zero;

            // GeloAbsoluto: inimigos congelados recebem dano extra (representa 50% de vulnerabilidade)
            if (SkillEvolutionManager.Tem(SkillEvolutionType.GeloAbsoluto))
            {
                foreach (var e in congelados)
                {
                    if (e.go == null) continue;
                    var ic = e.go.GetComponent<InimigoController>();
                    if (ic != null) ic.ReceberDano(12f * Time.deltaTime, false, false);
                }
            }

            yield return null;
        }

        DescongelarTodos();
        ativo = false;
        StartCoroutine(FadeOut(vfx));
    }

    // ─── DIREÇÃO DO ALVO ─────────────────────────────────────────────────

    Vector2 EncontrarDirecaoInimigo(Vector2 origem)
    {
        float   distMin = float.MaxValue;
        Vector2 melhor  = Vector2.right;

        foreach (var col in Physics2D.OverlapCircleAll(origem, raioDeteccao))
        {
            var root = ResolverRootInimigo(col.gameObject);
            if (root == null) continue;
            float d = Vector2.Distance(origem, root.transform.position);
            if (d < distMin) { distMin = d; melhor = ((Vector2)root.transform.position - origem).normalized; }
        }
        return melhor;
    }

    // ─── CONGELAR / DESCONGELAR ──────────────────────────────────────────

    void AtualizarCongelados(Vector2 posAnterior, Vector2 posAtual)
    {
        // Overlap na posição atual
        foreach (var col in Physics2D.OverlapCircleAll(posAtual, raio))
        {
            var root = ResolverRootInimigo(col.gameObject);
            if (root != null && !JaCongelado(root))
                Congelar(root);
        }

        // Sweep do frame anterior — captura inimigos na trajetória do campo
        Vector2 delta = posAtual - posAnterior;
        float   dist  = delta.magnitude;
        if (dist > 0.01f)
        {
            foreach (var hit in Physics2D.CircleCastAll(posAnterior, raio, delta.normalized, dist))
            {
                var root = ResolverRootInimigo(hit.collider.gameObject);
                if (root != null && !JaCongelado(root))
                    Congelar(root);
            }
        }

        // Remove entradas de inimigos destruídos
        for (int i = congelados.Count - 1; i >= 0; i--)
            if (congelados[i].go == null) congelados.RemoveAt(i);
    }

    void DescongelarTodos()
    {
        foreach (var e in congelados) DescongelarUm(e);
        congelados.Clear();
    }

    void Congelar(GameObject go)
    {
        Color gelo = new Color(0.55f, 0.85f, 1f);

        var scripts = new List<MonoBehaviour>();
        var movi1 = go.GetComponent<movi_inimigo>();
        var movi2 = go.GetComponent<movi_inimigo_manter_distancia>();
        var boss1 = go.GetComponent<BossController>();
        var boss2 = go.GetComponent<BossPrincesa>();
        var fant1 = go.GetComponent<FantasmaFogo>();
        var fant2 = go.GetComponent<FantasmaGelo>();
        var fant3 = go.GetComponent<FantasmaVeneno>();
        var fant4 = go.GetComponent<FantasmaVenenoAtirador>();
        var fant5 = go.GetComponent<FantasmaEletrico>();
        if (movi1 != null) { movi1.enabled = false; scripts.Add(movi1); }
        if (movi2 != null) { movi2.enabled = false; scripts.Add(movi2); }
        if (boss1 != null) { boss1.enabled = false; scripts.Add(boss1); }
        if (boss2 != null) { boss2.enabled = false; scripts.Add(boss2); }
        if (fant1 != null) { fant1.enabled = false; scripts.Add(fant1); }
        if (fant2 != null) { fant2.enabled = false; scripts.Add(fant2); }
        if (fant3 != null) { fant3.enabled = false; scripts.Add(fant3); }
        if (fant4 != null) { fant4.enabled = false; scripts.Add(fant4); }
        if (fant5 != null) { fant5.enabled = false; scripts.Add(fant5); }

        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        var srs   = go.GetComponentsInChildren<SpriteRenderer>();
        var cores = new Color[srs.Length];
        for (int i = 0; i < srs.Length; i++)
        {
            cores[i]     = srs[i].color;
            srs[i].color = Color.Lerp(srs[i].color, gelo, 0.65f);
        }

        congelados.Add(new EstadoInimigo
        {
            go             = go,
            scripts        = scripts,
            rb             = rb,
            renderers      = srs,
            coresOriginais = cores,
            efeitoVFX      = CriarEfeitoCongelamento(go),
        });
    }

    void DescongelarUm(EstadoInimigo e)
    {
        foreach (var s in e.scripts)
            if (s != null) s.enabled = true;
        for (int i = 0; i < e.renderers.Length; i++)
            if (e.renderers[i] != null) e.renderers[i].color = e.coresOriginais[i];
        if (e.efeitoVFX != null)
            StartCoroutine(FadeOutEfeito(e.efeitoVFX));
    }

    bool JaCongelado(GameObject go)
    {
        foreach (var e in congelados)
            if (e.go == go) return true;
        return false;
    }

    static GameObject ResolverRootInimigo(GameObject go)
    {
        // Determina o root do inimigo subindo pela hierarquia
        GameObject root = null;

        var ic = go.GetComponent<InimigoController>() ?? go.GetComponentInParent<InimigoController>();
        if (ic != null) root = ic.gameObject;

        if (root == null)
        {
            var mi = go.GetComponent<movi_inimigo>() ?? go.GetComponentInParent<movi_inimigo>();
            if (mi != null) root = mi.gameObject;
        }
        if (root == null)
        {
            var bc = go.GetComponent<BossController>() ?? go.GetComponentInParent<BossController>();
            if (bc != null) root = bc.gameObject;
        }
        if (root == null)
        {
            var bp = go.GetComponent<BossPrincesa>() ?? go.GetComponentInParent<BossPrincesa>();
            if (bp != null) root = bp.gameObject;
        }

        if (root == null) return null;

        // Projéteis da canalização têm ProjetilHomingPrincesa/ProjetilEspecialPrincesa
        // adicionado diretamente no root via AddComponent — GetComponent é suficiente aqui
        if (go.GetComponentInParent<ProjetilHomingPrincesa>(true)   != null) return null;
        if (go.GetComponentInParent<ProjetilEspecialPrincesa>(true) != null) return null;

        return root;
    }

    // ─── EFEITO POR INIMIGO ──────────────────────────────────────────────

    GameObject CriarEfeitoCongelamento(GameObject inimigo)
    {
        var root = new GameObject("FreezeVFX");
        root.transform.SetParent(inimigo.transform, false);
        root.transform.localPosition = Vector3.zero;

        // Anel pulsante ao redor do inimigo
        const int SEGS = 24;
        float r = 0.35f;
        var anelGO = new GameObject("Anel");
        anelGO.transform.SetParent(root.transform, false);
        var lr = anelGO.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 15;
        lr.startWidth    = lr.endWidth = 0.06f;
        lr.startColor    = lr.endColor = new Color(0.7f, 0.95f, 1f, 1f);
        for (int i = 0; i < SEGS; i++)
        {
            float a = (360f / SEGS) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
        }

        // Cristais orbitando (6 pontos)
        var mat = new Material(Shader.Find("Sprites/Default"));
        for (int i = 0; i < 6; i++)
        {
            var cristal = new GameObject($"Cristal_{i}");
            cristal.transform.SetParent(root.transform, false);
            var sr = cristal.AddComponent<SpriteRenderer>();
            sr.sprite       = GerarSpriteCristal();
            sr.color        = new Color(0.8f, 0.97f, 1f, 0.9f);
            sr.sortingOrder = 16;
            cristal.transform.localScale = Vector3.one * 0.18f;
        }

        StartCoroutine(AnimarEfeito(root, lr));
        return root;
    }

    IEnumerator AnimarEfeito(GameObject root, LineRenderer lr)
    {
        const int SEGS = 24;
        float r = 0.35f;
        float angulo = 0f;
        float tempo  = 0f;

        while (root != null && root.activeInHierarchy)
        {
            tempo  += Time.deltaTime;
            angulo += Time.deltaTime * 90f; // graus/s de órbita

            // Pulso do anel
            float pulso = 0.85f + Mathf.Sin(tempo * 4f) * 0.15f;
            if (lr != null)
            {
                for (int i = 0; i < SEGS; i++)
                {
                    float a = (360f / SEGS) * i * Mathf.Deg2Rad;
                    lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r * pulso, Mathf.Sin(a) * r * pulso));
                }
                float brilho = 0.6f + Mathf.Sin(tempo * 3f) * 0.4f;
                lr.startColor = lr.endColor = new Color(0.55f + brilho * 0.3f, 0.88f, 1f, 0.7f + brilho * 0.3f);
            }

            // Órbita dos cristais
            var cristais = new List<Transform>();
            foreach (Transform child in root.transform)
                if (child.name.StartsWith("Cristal")) cristais.Add(child);

            for (int i = 0; i < cristais.Count; i++)
            {
                float a  = (angulo + i * 60f) * Mathf.Deg2Rad;
                float ri = r * (0.9f + Mathf.Sin(tempo * 2f + i) * 0.15f);
                cristais[i].localPosition = new Vector3(Mathf.Cos(a) * ri, Mathf.Sin(a) * ri);
                cristais[i].Rotate(0, 0, Time.deltaTime * 180f);
            }

            yield return null;
        }
    }

    IEnumerator FadeOutEfeito(GameObject root)
    {
        if (root == null) yield break;
        var lr = root.GetComponentInChildren<LineRenderer>();
        var srs = root.GetComponentsInChildren<SpriteRenderer>();
        float t = 0f, dur = 0.3f;
        while (t < dur && root != null)
        {
            t += Time.deltaTime;
            float p = t / dur;
            if (lr != null) { Color c = lr.startColor; c.a = Mathf.Lerp(c.a, 0f, p); lr.startColor = lr.endColor = c; }
            foreach (var sr in srs) { if (sr != null) { Color c = sr.color; c.a = Mathf.Lerp(c.a, 0f, p); sr.color = c; } }
            yield return null;
        }
        if (root != null) Destroy(root);
    }

    static Sprite GerarSpriteCristal()
    {
        const int SZ = 16;
        var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = Color.clear;
        Color c     = new Color(0.85f, 0.97f, 1f, 1f);
        Color dim   = new Color(0.6f, 0.88f, 1f, 0.7f);

        for (int y = 0; y < SZ; y++)
            for (int x = 0; x < SZ; x++)
                tex.SetPixel(x, y, clear);

        int m = SZ / 2;
        // Cruz central
        for (int i = 1; i < SZ - 1; i++) { tex.SetPixel(m, i, i == m ? Color.white : c); tex.SetPixel(i, m, i == m ? Color.white : c); }
        // Diagonais curtas
        for (int d = 1; d <= 3; d++) { tex.SetPixel(m+d, m+d, dim); tex.SetPixel(m-d, m+d, dim); tex.SetPixel(m+d, m-d, dim); tex.SetPixel(m-d, m-d, dim); }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, SZ, SZ), new Vector2(0.5f, 0.5f), SZ);
    }

    // ─── VISUAL ─────────────────────────────────────────────────────────

    GameObject CriarVisual(Vector2 posicaoInicial)
    {
        var root = new GameObject("CampoDeGelo");
        root.transform.position = posicaoInicial;

        const int SEGS = 48;
        var anelGO = new GameObject("Anel");
        anelGO.transform.SetParent(root.transform, false);
        var lr = anelGO.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 12;
        lr.startWidth    = lr.endWidth = 0.12f;
        lr.startColor    = lr.endColor = new Color(0.6f, 0.9f, 1f, 0.9f);
        for (int i = 0; i < SEGS; i++)
        {
            float ang = (360f / SEGS) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * raio, Mathf.Sin(ang) * raio));
        }

        var discoGO = new GameObject("Disco");
        discoGO.transform.SetParent(root.transform, false);
        var sr       = discoGO.AddComponent<SpriteRenderer>();
        sr.sprite    = GerarSpriteDisco(128);
        sr.color     = new Color(0.4f, 0.8f, 1f, 0.14f);
        sr.sortingOrder = 11;
        discoGO.transform.localScale = Vector3.one * (raio * 2f);

        StartCoroutine(AnimarVisual(root, lr));
        return root;
    }

    IEnumerator AnimarVisual(GameObject root, LineRenderer lr)
    {
        float elapsed = 0f;
        while (elapsed < duracaoVisual && root != null)
        {
            elapsed += Time.deltaTime;
            if (lr != null)
            {
                float pulso = Mathf.Sin(elapsed * 2.5f) * 0.5f + 0.5f;
                Color cor   = Color.Lerp(new Color(0.5f, 0.85f, 1f), new Color(0.85f, 0.97f, 1f), pulso);
                cor.a       = elapsed > duracaoVisual - 1f
                    ? Mathf.PingPong(elapsed * 8f, 1f) * 0.4f + 0.5f
                    : 0.9f;
                lr.startColor = lr.endColor = cor;
            }
            yield return null;
        }
    }

    IEnumerator FadeOut(GameObject root)
    {
        if (root == null) yield break;
        var lr   = root.GetComponentInChildren<LineRenderer>();
        var sr   = root.GetComponentInChildren<SpriteRenderer>();
        Color cL = lr != null ? lr.startColor : Color.clear;
        Color cS = sr != null ? sr.color       : Color.clear;

        float t = 0f, dur = 0.4f;
        while (t < dur && root != null)
        {
            t += Time.deltaTime;
            float p = t / dur;
            if (lr != null) { Color c = cL; c.a = Mathf.Lerp(cL.a, 0f, p); lr.startColor = lr.endColor = c; }
            if (sr != null) { Color c = cS; c.a = Mathf.Lerp(cS.a, 0f, p); sr.color = c; }
            yield return null;
        }
        if (root != null) Destroy(root);
    }

    static Sprite GerarSpriteDisco(int sz)
    {
        float cx = sz * 0.5f;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d    = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            float t    = Mathf.Clamp01(d / cx);
            float bord = Mathf.Clamp01(1f - Mathf.Abs(t - 0.88f) / 0.12f);
            float meio = Mathf.Pow(1f - t, 2f) * 0.3f;
            float a    = Mathf.Clamp01(bord + meio) * (t < 1f ? 1f : 0f);
            tex.SetPixel(x, y, new Color(0.55f, 0.88f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
