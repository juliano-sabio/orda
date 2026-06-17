using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorrenteSombriaSkillBehavior : SkillBehavior, ISkillComRecarga, IEvoluivel
{
    float baseDano      = 6f;
    float multiplicador = 0.25f;
    float intervalo     = 10f;
    float duracaoAtiva  = 3f;
    int   qtdAlvos      = 3;
    float raioDeteccao  = 12f;
    float timer;
    public bool  EmRecarga    => timer > 0f;
    public float TimerRecarga => timer;
    public float RecargaTotal => intervalo;

    readonly List<GameObject> linhasAtivas = new List<GameObject>();
    readonly Dictionary<InimigoController, float> velOriginal     = new Dictionary<InimigoController, float>();
    readonly Dictionary<InimigoController, float> velOriginalDist = new Dictionary<InimigoController, float>();

    float DanoAtual => baseDano + (playerStats != null ? playerStats.attack * multiplicador : 0f);

        public void OnEvolucaoAplicada(SkillEvolutionType tipo) { if (tipo == SkillEvolutionType.CorrenteAlcance) raioDeteccao *= 1.6f; }
    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    void OnDestroy()
    {
        foreach (var go in linhasAtivas)
            if (go != null) Destroy(go);
        linhasAtivas.Clear();
    }

    static readonly Color COR_ORIG = new Color(0.5f, 0.1f, 0.9f);
    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? COR_ORIG;
        return COR_ORIG;
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        baseDano    = data.attackBonus > 0f          ? data.attackBonus        : 12f;
        intervalo   = data.activationInterval > 0f   ? data.activationInterval : 5f;
        duracaoAtiva = data.duration > 0f            ? data.duration           : 3f;
        qtdAlvos    = data.projectileCount > 0       ? data.projectileCount    : 3;
        timer       = intervalo;
    }

    void Update()
    {
        if (playerStats == null) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) { timer = intervalo; StartCoroutine(AtivarCorrente()); }
    }

    public override void ApplyEffect() => StartCoroutine(AtivarCorrente());

    IEnumerator AtivarCorrente()
    {
        // Limpa resíduos de ativações anteriores
        foreach (var go in linhasAtivas)
            if (go != null) Destroy(go);
        linhasAtivas.Clear();

        int qtdReal  = SkillEvolutionManager.Tem(SkillEvolutionType.CorrenteReforcada) ? qtdAlvos + 1 : qtdAlvos;
        float danoMult = SkillEvolutionManager.Tem(SkillEvolutionType.CorrenteReforcada) ? 2f : 1f;
        var alvos = EncontrarAlvos(qtdReal);
        if (alvos.Count == 0) yield break;

        // ── Canalização ──────────────────────────────────────────────────────
        yield return StartCoroutine(Canalizacao());
        if (playerStats == null) yield break;

        // ── Disparo das correntes ─────────────────────────────────────────────
        CameraShaker.Tremer(0.1f, 0.2f);
        StartCoroutine(BurstLancamento());

        var linhas = new List<LineRenderer>();
        var pontos = new List<Transform>();
        pontos.Add(playerStats.transform);
        foreach (var ic in alvos)
            if (ic != null && !ic.estaMorrendo) pontos.Add(ic.transform);

        for (int i = 0; i < pontos.Count - 1; i++)
        {
            var lgo = new GameObject($"Corrente{i}");
            linhasAtivas.Add(lgo);
            Destroy(lgo, duracaoAtiva + 0.12f); // failsafe
            var lr = lgo.AddComponent<LineRenderer>();
            lr.useWorldSpace  = true; lr.positionCount = 3;
            lr.material       = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder   = 12; lr.numCapVertices = 4;
            linhas.Add(lr);

            // Flash no inimigo alvo
            if (i + 1 < pontos.Count && pontos[i + 1] != null)
                StartCoroutine(FlashAlvo(pontos[i + 1]));
        }

        float proxDano = 0f, ang = 0f, proxParticula = 0f;

        for (float t = 0f; t < duracaoAtiva; t += Time.deltaTime)
        {
            ang += Time.deltaTime * 220f;

            for (int i = 0; i < linhas.Count; i++)
            {
                if (linhas[i] == null) continue;

                // Se o alvo foi destruído, oculta a linha imediatamente
                if (pontos[i] == null || pontos[i + 1] == null)
                {
                    linhas[i].positionCount = 0;
                    continue;
                }

                Vector2 de  = pontos[i].position;
                Vector2 ate = pontos[i + 1].position;
                Vector2 perp = new Vector2(-(ate - de).y, (ate - de).x).normalized;
                Vector2 meio = Vector2.Lerp(de, ate, 0.5f)
                             + perp * Mathf.Sin(ang * 0.07f + i * 1.5f) * 0.45f;

                linhas[i].positionCount = 5;
                linhas[i].SetPosition(0, de);
                linhas[i].SetPosition(1, Vector2.Lerp(de, meio, 0.4f) + perp * Mathf.Sin(ang * 0.1f) * 0.2f);
                linhas[i].SetPosition(2, meio);
                linhas[i].SetPosition(3, Vector2.Lerp(meio, ate, 0.6f) + perp * Mathf.Sin(ang * 0.09f + 1f) * 0.2f);
                linhas[i].SetPosition(4, ate);

                float pulso = Mathf.Sin(t * 10f + i * 2f) * 0.5f + 0.5f;
                float vidaPct = 1f - t / duracaoAtiva;
                Color ce = CorElemento();
                Color cor = Color.Lerp(ce, Color.white, pulso * 0.25f);
                cor.a = (0.55f + pulso * 0.45f) * vidaPct;
                linhas[i].startColor = linhas[i].endColor = cor;
                linhas[i].startWidth = linhas[i].endWidth = (0.06f + pulso * 0.1f) * vidaPct;
            }

            // Dano
            proxDano -= Time.deltaTime;
            if (proxDano <= 0f)
            {
                proxDano = 0.5f;
                foreach (var ic in alvos)
                    if (ic != null && !ic.estaMorrendo && ic.gameObject != null)
                    {
                        ic.ReceberDano(DanoAtual * danoMult, false);
                        SkillElementEffect.Aplicar(skillData, ic.gameObject, DanoAtual * danoMult, this);
                        StartCoroutine(FlashAlvo(ic.transform));
                        if (SkillEvolutionManager.Tem(SkillEvolutionType.CorrenteParalisante))
                        {
                            var movi = ic.GetComponent<movi_inimigo>();
                            if (movi != null)
                            {
                                if (!velOriginal.ContainsKey(ic))
                                    velOriginal[ic] = movi.velocidade;
                                movi.velocidade = 0f;
                            }
                            var moviDist = ic.GetComponent<movi_inimigo_manter_distancia>();
                            if (moviDist != null)
                            {
                                if (!velOriginalDist.ContainsKey(ic))
                                    velOriginalDist[ic] = moviDist.velocidade;
                                moviDist.velocidade = 0f;
                            }
                            var rb2 = ic.GetComponent<Rigidbody2D>();
                            if (rb2 != null) rb2.linearVelocity = Vector2.zero;
                        }
                    }
            }

            // Partículas ao longo das correntes
            proxParticula -= Time.deltaTime;
            if (proxParticula <= 0f)
            {
                proxParticula = 0.12f;
                foreach (var lr in linhas)
                    if (lr != null && lr.positionCount > 0)
                        SpawnParticulaCorrente(lr.GetPosition(Random.Range(0, lr.positionCount)));
            }

            yield return null;
        }

        // Fade de saída rápido
        yield return StartCoroutine(FadeLinhas(linhas, 0.08f));

        // Restaura velocidade dos alvos paralisados
        if (SkillEvolutionManager.Tem(SkillEvolutionType.CorrenteParalisante))
            foreach (var ic in alvos)
                if (ic != null && !ic.estaMorrendo)
                {
                    var movi = ic.GetComponent<movi_inimigo>();
                    if (movi != null && velOriginal.TryGetValue(ic, out float velSalva))
                        movi.velocidade = velSalva;
                    var moviDist = ic.GetComponent<movi_inimigo_manter_distancia>();
                    if (moviDist != null && velOriginalDist.TryGetValue(ic, out float velSalvaDist))
                        moviDist.velocidade = velSalvaDist;
                }
        velOriginal.Clear();
        velOriginalDist.Clear();

        // Destrói todas as linhas imediatamente
        foreach (var lr in linhas)
            if (lr != null) { linhasAtivas.Remove(lr.gameObject); Destroy(lr.gameObject); }

        // Limpa qualquer resíduo restante na lista
        foreach (var go in linhasAtivas)
            if (go != null) Destroy(go);
        linhasAtivas.Clear();
    }

    IEnumerator Canalizacao()
    {
        float dur = 0.6f;
        var sr = playerStats?.GetComponent<SpriteRenderer>();

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float prog  = t / dur;
            float pulso = Mathf.Sin(t * 20f) * 0.5f + 0.5f;

            // Player pulsa roxo
            if (sr != null)
                sr.color = Color.Lerp(Color.white, new Color(0.6f, 0.2f, 1f), prog * pulso * 0.7f);

            // Anel de canalização contraindo
            SpawnAnelCanalizacao(playerStats.transform.position,
                Mathf.Lerp(3f, 0.5f, prog), pulso);

            // Partículas sendo sugadas para o player
            if (Time.frameCount % 3 == 0)
                SpawnParticulaSugada(playerStats.transform.position, Mathf.Lerp(3f, 0.5f, prog));

            yield return null;
        }

        if (sr != null) sr.color = Color.white;
    }

    void SpawnAnelCanalizacao(Vector2 centro, float raio, float pulso)
    {
        const int S = 32;
        var go = new GameObject("AnelCan");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 11;
        lr.startWidth = lr.endWidth = 0.04f + pulso * 0.04f;
        lr.startColor = lr.endColor = new Color(0.5f, 0.1f, 0.9f, 0.5f + pulso * 0.4f);
        for (int i = 0; i < S; i++)
        {
            float ang = 360f / S * i * Mathf.Deg2Rad;
            lr.SetPosition(i, centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio);
        }
        Destroy(go, 0.06f); // failsafe garantido
    }

    void SpawnParticulaSugada(Vector2 centro, float raioOrig)
    {
        float ang = Random.Range(0f, Mathf.PI * 2f);
        Vector2 pos = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raioOrig;
        var go = new GameObject("PS");
        go.transform.position = pos;
        var sr2 = go.AddComponent<SpriteRenderer>();
        sr2.sprite = GerarDisco(6); sr2.color = new Color(0.5f, 0.1f, 0.9f, 0.8f);
        sr2.sortingOrder = 13;
        go.transform.localScale = Vector3.one * Random.Range(0.08f, 0.18f);
        Vector2 vel = (centro - pos).normalized * Random.Range(3f, 7f);
        go.AddComponent<AutoDestroyFadeMove>().Iniciar(vel, 0.2f);
        Destroy(go, 0.5f); // failsafe
    }

    IEnumerator BurstLancamento()
    {
        for (int i = 0; i < 12; i++)
        {
            float ang = i / 12f * Mathf.PI * 2f;
            var go = new GameObject("BL");
            go.transform.position = (Vector2)playerStats.transform.position;
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite = GerarDisco(8); sr2.color = new Color(0.6f, 0.2f, 1f);
            sr2.sortingOrder = 14;
            go.transform.localScale = Vector3.one * Random.Range(0.1f, 0.22f);
            Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(3f, 7f);
            go.AddComponent<AutoDestroyFadeMove>().Iniciar(vel, 0.4f);
            Destroy(go, 0.8f); // failsafe
        }
        yield return null;
    }

    IEnumerator FlashAlvo(Transform alvo)
    {
        if (alvo == null) yield break;
        var sr = alvo.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        Color orig = sr.color;
        sr.color = new Color(0.6f, 0.2f, 1f);
        yield return new WaitForSecondsRealtime(0.08f); // real-time para não travar
        if (sr != null) sr.color = orig; // sempre reseta
    }

    void SpawnParticulaCorrente(Vector3 pos)
    {
        var go = new GameObject("PC");
        go.transform.position = pos;
        var sr2 = go.AddComponent<SpriteRenderer>();
        sr2.sprite = GerarDisco(5); sr2.color = new Color(0.6f, 0.2f, 1f, 0.7f);
        sr2.sortingOrder = 11;
        go.transform.localScale = Vector3.one * Random.Range(0.06f, 0.14f);
        go.AddComponent<AutoDestroyFadeMove>().Iniciar(Random.insideUnitCircle * 0.8f, 0.2f);
        Destroy(go, 0.5f); // failsafe
    }

    IEnumerator FadeLinhas(List<LineRenderer> linhas, float dur)
    {
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            foreach (var lr in linhas)
            {
                if (lr == null) continue;
                Color c = lr.startColor; c.a = Mathf.Lerp(c.a, 0f, p);
                lr.startColor = lr.endColor = c;
                lr.startWidth = lr.endWidth = Mathf.Lerp(lr.startWidth, 0f, p);
            }
            yield return null;
        }
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear; float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
        { float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f),new Vector2(cx,cx)); tex.SetPixel(x,y,new Color(1f,1f,1f,d<cx?1f:0f)); }
        tex.Apply(); return Sprite.Create(tex, new Rect(0,0,sz,sz), new Vector2(0.5f,0.5f), sz);
    }

    List<InimigoController> EncontrarAlvos(int qtd)
    {
        var todos = FindObjectsByType<InimigoController>(FindObjectsSortMode.None);
        var lista = new List<InimigoController>(todos);
        Vector2 orig = playerStats.transform.position;
        lista.RemoveAll(ic => ic.estaMorrendo || Vector2.Distance(ic.transform.position, orig) > raioDeteccao);
        lista.Sort((a, b) => Vector2.Distance(a.transform.position, orig).CompareTo(Vector2.Distance(b.transform.position, orig)));
        if (lista.Count > qtd) lista.RemoveRange(qtd, lista.Count - qtd);
        return lista;
    }
}


