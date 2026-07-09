// Helpers de efeito reutilizados pelas evoluções
using UnityEngine;
using System.Collections;

public static class EvolutionFX
{
    // Aplica DoT (veneno) a um inimigo
    public static void AplicarVeneno(InimigoController ic, float danoTick, float duracao, float intervalo = 0.5f)
    {
        if (ic == null || ic.estaMorrendo) return;
        var host = ic.gameObject.AddComponent<VenenoEvolutionFX>();
        host.Iniciar(danoTick, duracao, intervalo);
    }

    // Shockwave visual + dano em área
    public static void SpawnShockwave(Vector2 pos, float raio, float dano, MonoBehaviour owner)
    {
        owner.StartCoroutine(ShockwaveCoroutine(pos, raio, dano));
    }

    static IEnumerator ShockwaveCoroutine(Vector2 pos, float raio, float dano)
    {
        // Dano
        var hits = Physics2D.OverlapCircleAll(pos, raio);
        foreach (var col in hits)
        {
            var ic = col.GetComponent<InimigoController>() ?? col.GetComponentInParent<InimigoController>();
            if (ic != null && !ic.estaMorrendo) ic.ReceberDano(dano, false);
        }

        // Visual anel
        const int S = 32;
        var go = new GameObject("Shockwave");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 12;
        Object.Destroy(go, 1f); // failsafe
        float dur = 0.4f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / dur; float r = Mathf.Lerp(0.2f, raio, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.22f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(1f, 0.7f, 0.1f, Mathf.Lerp(1f, 0f, p));
            for (int i = 0; i < S; i++) { float a = 360f / S * i * Mathf.Deg2Rad; lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
            yield return null;
        }
        if (go != null) Object.Destroy(go);
    }

    // Explosão simples — corre no próprio GO, não depende do owner
    public static void SpawnExplosao(Vector2 pos, float raio, float dano, Color cor, MonoBehaviour owner)
    {
        var hits = Physics2D.OverlapCircleAll(pos, raio);
        foreach (var col in hits)
        {
            var ic = col.GetComponent<InimigoController>() ?? col.GetComponentInParent<InimigoController>();
            if (ic != null && !ic.estaMorrendo) ic.ReceberDano(dano, false);
        }

        var go = new GameObject("Explosao");
        go.transform.position = pos;
        go.AddComponent<ExplosaoSelfAnimator>().Iniciar(pos, raio, cor, 0.35f);
    }

    public static void AplicarLentidao(InimigoController ic, float duracao, float fator = 0.4f)
    {
        if (ic == null || ic.EhBoss()) return; // bosses são IMUNES a lentidão/atordoamento
        var movi = ic.GetComponent<movi_inimigo>();
        if (movi != null) ic.StartCoroutine(LentidaoCoroutine(movi, duracao, fator));
    }

    static IEnumerator LentidaoCoroutine(movi_inimigo movi, float dur, float fator)
    {
        float orig = movi.velocidade;
        movi.velocidade *= fator;
        yield return new WaitForSeconds(dur);
        if (movi != null) movi.velocidade = orig;
    }

    public static void AplicarChamas(InimigoController ic, MonoBehaviour owner, float danoTick = 3f, float dur = 3f)
    {
        if (ic == null || ic.estaMorrendo) return;
        owner.StartCoroutine(ChamasCoroutine(ic, danoTick, dur));
    }

    static IEnumerator ChamasCoroutine(InimigoController ic, float dano, float dur)
    {
        float timer = 0f;
        while (timer < dur && ic != null && !ic.estaMorrendo)
        {
            timer += 0.5f;
            yield return new WaitForSeconds(0.5f);
            if (ic != null && !ic.estaMorrendo) ic.ReceberDano(dano, false);

            // Partícula de chama
            if (ic != null)
            {
                var p = new GameObject("Chama");
                p.transform.position = (Vector2)ic.transform.position + Random.insideUnitCircle * 0.3f;
                var sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = GerarDisco(6); sr.color = new Color(1f, 0.4f, 0.1f, 0.8f); sr.sortingOrder = 14;
                p.transform.localScale = Vector3.one * Random.Range(0.1f, 0.2f);
                p.AddComponent<AutoDestroyFadeMove>().Iniciar(Vector2.up * Random.Range(0.5f, 1.5f), 0.4f);
                Object.Destroy(p, 0.6f);
            }
        }
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear; float cx = sz*0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++) { float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f),new Vector2(cx,cx)); tex.SetPixel(x,y,new Color(1f,1f,1f,d<cx?1f:0f)); }
        tex.Apply(); return Sprite.Create(tex, new Rect(0,0,sz,sz), new Vector2(0.5f,0.5f), sz);
    }

    static Sprite DiscoPublico(int sz) => GerarDisco(sz);

    // Puxa inimigos comuns (não-bosses) em direção a um centro. Chamar por frame.
    public static void PuxarInimigos(Vector2 centro, float raio, float forca, float raioMin = 0.4f)
    {
        var hits = Physics2D.OverlapCircleAll(centro, raio);
        foreach (var col in hits)
        {
            var ic = col.GetComponent<InimigoController>() ?? col.GetComponentInParent<InimigoController>();
            if (ic == null || ic.estaMorrendo || ic.EhBoss()) continue;
            if (Vector2.Distance(ic.transform.position, centro) <= raioMin) continue;
            ic.transform.position = Vector2.MoveTowards(ic.transform.position, centro, forca * Time.deltaTime);
        }
    }

    // Empurra inimigos (knockback) para longe de um centro, com dano de colisão.
    public static void ArremessarInimigos(Vector2 centro, float raio, float dano, MonoBehaviour owner)
    {
        var hits = Physics2D.OverlapCircleAll(centro, raio);
        foreach (var col in hits)
        {
            var ic = col.GetComponent<InimigoController>() ?? col.GetComponentInParent<InimigoController>();
            if (ic == null || ic.estaMorrendo) continue;
            if (!ic.EhBoss())
            {
                Vector2 dir = ((Vector2)ic.transform.position - centro).normalized;
                if (dir == Vector2.zero) dir = Random.insideUnitCircle.normalized;
                owner.StartCoroutine(Empurrao(ic.transform, dir, 4f, 0.2f));
            }
            ic.ReceberDano(dano, false);
        }
    }

    static IEnumerator Empurrao(Transform t, Vector2 dir, float dist, float dur)
    {
        Vector2 ini = t.position, alvo = ini + dir * dist;
        for (float e = 0f; e < dur; e += Time.deltaTime)
        {
            if (t == null) yield break;
            t.position = Vector2.Lerp(ini, alvo, e / dur);
            yield return null;
        }
    }

    // Zona de dano persistente (ex.: cratera em chamas) — tica dano numa área por 'duracao'.
    public static void SpawnZonaFogo(Vector2 pos, float raio, float danoTick, float duracao)
    {
        var go = new GameObject("ZonaFogo");
        go.transform.position = pos;
        go.AddComponent<ZonaFogoFX>().Iniciar(raio, danoTick, duracao);
    }

    // Adiciona uma marca no inimigo; ao acumular 'maxMarcas', ele detona numa explosão.
    public static void AplicarMarca(InimigoController ic, int maxMarcas, float danoDetonar)
    {
        if (ic == null || ic.estaMorrendo) return;
        var m = ic.GetComponent<MarcaMorteFX>();
        if (m == null) m = ic.gameObject.AddComponent<MarcaMorteFX>();
        m.Adicionar(maxMarcas, danoDetonar);
    }

    // Campo elétrico persistente que segue o alvo, arqueia pra inimigos próximos e os danifica.
    public static void SpawnTeiaEletrica(Transform seguir, float raio, float danoTick, float dur)
    {
        var go = new GameObject("TeiaEletrica");
        go.AddComponent<TeiaEletricaFX>().Iniciar(seguir, raio, danoTick, dur);
    }

    // Invoca lâminas que orbitam o player por 'dur' segundos, cortando quem chega perto.
    public static GameObject SpawnEspadasOrbitais(Transform player, int qtd, float dano, float raio, float dur)
    {
        var go = new GameObject("EspadasOrbitais");
        go.AddComponent<EspadasOrbitaisFX>().Iniciar(player, qtd, dano, raio, dur);
        return go;
    }
}

// Marca da Morte: acumula marcas no inimigo; ao chegar no limite, detona em explosão sombria.
public class MarcaMorteFX : MonoBehaviour
{
    int   marcas;
    int   max = 3;
    float danoDetonar;
    SpriteRenderer[] pips;

    public void Adicionar(int maxMarcas, float dano)
    {
        max = Mathf.Max(1, maxMarcas);
        danoDetonar = dano;
        marcas++;
        AtualizarVisual();
        if (marcas >= max) Detonar();
    }

    void AtualizarVisual()
    {
        // pequenos pontos roxos girando acima do inimigo indicando as marcas
        if (pips == null)
        {
            pips = new SpriteRenderer[3];
            for (int i = 0; i < 3; i++)
            {
                var p = new GameObject("Marca" + i);
                p.transform.SetParent(transform, false);
                p.transform.localPosition = new Vector3((i - 1) * 0.22f, 0.7f, 0f);
                var sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = Disco(); sr.color = new Color(0.6f, 0.2f, 1f, 0f); sr.sortingOrder = 20;
                p.transform.localScale = Vector3.one * 0.14f;
                pips[i] = sr;
            }
        }
        for (int i = 0; i < pips.Length; i++)
            if (pips[i] != null) pips[i].color = new Color(0.65f, 0.25f, 1f, i < marcas ? 0.95f : 0f);
    }

    void Detonar()
    {
        Vector2 pos = transform.position;
        marcas = 0;
        if (pips != null) foreach (var p in pips) if (p != null) p.color = new Color(0.65f, 0.25f, 1f, 0f);
        EvolutionFX.SpawnExplosao(pos, 2.2f, danoDetonar, new Color(0.55f, 0.2f, 1f), this);
        SomSkill.Tocar(SomSkill.Tipo.CorteImpactoDark, pos, 0.5f);
    }

    static Texture2D _t;
    static Sprite Disco()
    {
        if (_t == null)
        {
            int sz = 8; _t = new Texture2D(sz, sz, TextureFormat.RGBA32, false); _t.filterMode = FilterMode.Bilinear; float cx = sz * 0.5f;
            for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++) { float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f), new Vector2(cx,cx)); _t.SetPixel(x,y,new Color(1f,1f,1f,d<cx?1f:0f)); }
            _t.Apply();
        }
        return Sprite.Create(_t, new Rect(0,0,8,8), new Vector2(0.5f,0.5f), 8);
    }
}

// Chicote Condutor: campo elétrico que segue o player, arqueia pros inimigos próximos e os danifica.
public class TeiaEletricaFX : MonoBehaviour
{
    Transform seguir; float raio, danoTick, dur;
    public void Iniciar(Transform s, float r, float d, float du) { seguir = s; raio = r; danoTick = d; dur = du; StartCoroutine(Run()); }

    IEnumerator Run()
    {
        float t = 0f, prox = 0f;
        var arcos = new System.Collections.Generic.List<GameObject>();
        while (t < dur && seguir != null)
        {
            t += Time.deltaTime; prox -= Time.deltaTime;
            transform.position = seguir.position;
            if (prox <= 0f)
            {
                prox = 0.35f;
                foreach (var a in arcos) if (a != null) Destroy(a);
                arcos.Clear();
                var hits = Physics2D.OverlapCircleAll(transform.position, raio);
                foreach (var col in hits)
                {
                    var ic = col.GetComponent<InimigoController>() ?? col.GetComponentInParent<InimigoController>();
                    if (ic == null || ic.estaMorrendo) continue;
                    ic.ReceberDano(danoTick, false);
                    var ago = new GameObject("Arco");
                    var lr = ago.AddComponent<LineRenderer>();
                    lr.useWorldSpace = true; lr.positionCount = 5;
                    lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 13;
                    lr.startWidth = lr.endWidth = 0.06f;
                    lr.startColor = lr.endColor = new Color(0.3f, 0.85f, 1f, 0.9f);
                    Vector2 a0 = transform.position, a1 = ic.transform.position;
                    for (int i = 0; i < 5; i++)
                    {
                        float f = i / 4f;
                        Vector2 p = Vector2.Lerp(a0, a1, f);
                        if (i > 0 && i < 4) p += Random.insideUnitCircle * 0.22f;
                        lr.SetPosition(i, p);
                    }
                    arcos.Add(ago);
                    Destroy(ago, 0.34f);
                }
            }
            yield return null;
        }
        foreach (var a in arcos) if (a != null) Destroy(a);
        Destroy(gameObject);
    }
}

// Espadas Orbitais: lâminas girando ao redor do player que cortam inimigos próximos por um tempo.
public class EspadasOrbitaisFX : MonoBehaviour
{
    Transform player; int qtd; float dano, raio, dur;
    Transform[] laminas;
    readonly System.Collections.Generic.Dictionary<int, float> cd = new System.Collections.Generic.Dictionary<int, float>();

    public void Iniciar(Transform p, int q, float d, float r, float du)
    { player = p; qtd = Mathf.Max(1, q); dano = d; raio = r; dur = du; Criar(); StartCoroutine(Run()); }

    void Criar()
    {
        laminas = new Transform[qtd];
        for (int i = 0; i < qtd; i++)
        {
            var g = new GameObject("EspadaOrbital");
            g.transform.SetParent(transform, false);
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteLamina(); sr.color = new Color(0.85f, 0.88f, 1f, 0.95f); sr.sortingOrder = 14;
            g.transform.localScale = new Vector3(0.35f, 0.95f, 1f);
            laminas[i] = g.transform;
        }
    }

    IEnumerator Run()
    {
        float t = 0f, ang = 0f;
        while (t < dur && player != null)
        {
            t += Time.deltaTime; ang += Time.deltaTime * 230f;
            transform.position = player.position;
            float fade = t > dur - 0.5f ? (dur - t) / 0.5f : 1f;
            for (int i = 0; i < qtd; i++)
            {
                if (laminas[i] == null) continue;
                float a = (ang + 360f / qtd * i) * Mathf.Deg2Rad;
                Vector2 off = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * raio;
                laminas[i].localPosition = off;
                laminas[i].localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg + 90f);
                var srr = laminas[i].GetComponent<SpriteRenderer>();
                if (srr != null) { var cc = srr.color; srr.color = new Color(cc.r, cc.g, cc.b, 0.95f * fade); }

                var hits = Physics2D.OverlapCircleAll((Vector2)player.position + off, 0.5f);
                foreach (var col in hits)
                {
                    var ic = col.GetComponent<InimigoController>() ?? col.GetComponentInParent<InimigoController>();
                    if (ic == null || ic.estaMorrendo) continue;
                    int id = ic.gameObject.GetInstanceID();
                    if (cd.TryGetValue(id, out float q) && Time.time < q) continue;
                    cd[id] = Time.time + 0.4f;
                    ic.ReceberDano(dano, false);
                }
            }
            yield return null;
        }
        Destroy(gameObject);
    }

    static Texture2D _tx;
    static Sprite SpriteLamina()
    {
        if (_tx == null)
        {
            int w = 6, h = 20; _tx = new Texture2D(w, h, TextureFormat.RGBA32, false); _tx.filterMode = FilterMode.Bilinear;
            float cx = w * 0.5f;
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float nx = Mathf.Abs(x + 0.5f - cx) / cx; float ny = y / (float)(h - 1);
                float larg = ny < 0.75f ? (1f - nx) : (1f - nx) * (1f - ny) * 4f;
                _tx.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(larg + Mathf.Max(0f, 0.7f - nx * 5f))));
            }
            _tx.Apply();
        }
        return Sprite.Create(_tx, new Rect(0, 0, 6, 20), new Vector2(0.5f, 0.5f), 20);
    }
}

// Cratera em chamas: anel visual + brasas subindo + dano periódico em área.
public class ZonaFogoFX : MonoBehaviour
{
    public void Iniciar(float raio, float danoTick, float dur) => StartCoroutine(Run(raio, danoTick, dur));

    IEnumerator Run(float raio, float danoTick, float dur)
    {
        const int S = 36;
        var lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 6;
        lr.startWidth = lr.endWidth = 0.12f;
        Vector2 c = transform.position;
        for (int i = 0; i < S; i++) { float a = 360f / S * i * Mathf.Deg2Rad; lr.SetPosition(i, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * raio); }

        float t = 0f, prox = 0f, embers = 0f;
        while (t < dur)
        {
            t += Time.deltaTime; prox -= Time.deltaTime; embers -= Time.deltaTime;
            float pulso = Mathf.Sin(Time.time * 8f) * 0.5f + 0.5f;
            float fade = t > dur - 0.6f ? (dur - t) / 0.6f : 1f;
            lr.startColor = lr.endColor = new Color(1f, 0.4f + pulso * 0.2f, 0.05f, (0.5f + pulso * 0.35f) * fade);

            if (prox <= 0f)
            {
                prox = 0.4f;
                var hits = Physics2D.OverlapCircleAll(c, raio);
                foreach (var col in hits)
                {
                    var ic = col.GetComponent<InimigoController>() ?? col.GetComponentInParent<InimigoController>();
                    if (ic != null && !ic.estaMorrendo) ic.ReceberDano(danoTick, false);
                }
            }
            if (embers <= 0f)
            {
                embers = 0.06f;
                var p = new GameObject("Brasa");
                p.transform.position = c + Random.insideUnitCircle * raio * 0.9f;
                var sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFogo(); sr.color = new Color(1f, Random.Range(0.3f, 0.6f), 0.05f, 0.8f); sr.sortingOrder = 8;
                p.transform.localScale = Vector3.one * Random.Range(0.12f, 0.26f);
                p.AddComponent<AutoDestroyFadeMove>().Iniciar(Vector2.up * Random.Range(0.8f, 1.8f), 0.5f);
                Destroy(p, 0.7f);
            }
            yield return null;
        }
        Destroy(gameObject);
    }

    static Texture2D _tex;
    static Sprite SpriteFogo()
    {
        if (_tex == null)
        {
            int sz = 8; _tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false); _tex.filterMode = FilterMode.Bilinear; float cx = sz * 0.5f;
            for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++) { float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f), new Vector2(cx,cx)); _tex.SetPixel(x,y,new Color(1f,1f,1f,d<cx?1f:0f)); }
            _tex.Apply();
        }
        return Sprite.Create(_tex, new Rect(0,0,8,8), new Vector2(0.5f,0.5f), 8);
    }
}

// Component self-managed para DoT de veneno
public class VenenoEvolutionFX : MonoBehaviour
{
    public void Iniciar(float dano, float dur, float intervalo)
        => StartCoroutine(Run(dano, dur, intervalo));

    IEnumerator Run(float dano, float dur, float intervalo)
    {
        var ic = GetComponent<InimigoController>();
        float t = 0f;
        while (t < dur && ic != null && !ic.estaMorrendo)
        {
            yield return new WaitForSeconds(intervalo);
            t += intervalo;
            if (ic != null && !ic.estaMorrendo) ic.ReceberDano(dano, false);
        }
        Destroy(this);
    }
}

// Anima e destrói o próprio GO da explosão — não depende de nenhum owner externo
public class ExplosaoSelfAnimator : MonoBehaviour
{
    public void Iniciar(Vector2 pos, float raio, Color cor, float dur)
        => StartCoroutine(Animar(pos, raio, cor, dur));

    IEnumerator Animar(Vector2 pos, float raio, Color cor, float dur)
    {
        const int S = 40;
        var lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 13;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur; float r = Mathf.Lerp(0.1f, raio, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.3f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(1f, 0f, p));
            for (int i = 0; i < S; i++) { float a = 360f / S * i * Mathf.Deg2Rad; lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
            yield return null;
        }
        Destroy(gameObject);
    }
}
