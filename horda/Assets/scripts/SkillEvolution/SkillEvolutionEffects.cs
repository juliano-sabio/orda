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
        if (ic == null) return;
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
