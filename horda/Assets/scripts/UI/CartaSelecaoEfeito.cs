using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CartaSelecaoEfeito : MonoBehaviour
{
    static CartaSelecaoEfeito _inst;
    static Sprite _glow;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCriar()
    {
        if (_inst != null) return;
        var go = new GameObject("CartaSelecaoEfeito");
        DontDestroyOnLoad(go);
        _inst = go.AddComponent<CartaSelecaoEfeito>();
    }

    void Awake()
    {
        if (_inst != null && _inst != this) { Destroy(gameObject); return; }
        _inst = this;
        _glow = CriarGlow();
    }

    // Gradiente radial suave para o brilho das bolinhas
    static Sprite CriarGlow()
    {
        int sz = 64;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        var pixels = new Color[sz * sz];
        float mid = sz * 0.5f;
        for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float d = Mathf.Clamp01(new Vector2(x - mid, y - mid).magnitude / mid);
                float a = Mathf.Pow(1f - d, 1.6f);
                pixels[y * sz + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f);
    }

    public static void Executar(GameObject cartaSelecionada, List<GameObject> todasCartas, System.Action callback)
    {
        if (_inst == null || cartaSelecionada == null) { callback?.Invoke(); return; }
        _inst.StartCoroutine(_inst.Animar(cartaSelecionada, todasCartas, callback));
    }

    IEnumerator Animar(GameObject cartaGO, List<GameObject> todas, System.Action callback)
    {
        var carta = cartaGO.GetComponent<RectTransform>();
        if (carta == null) { callback?.Invoke(); yield break; }

        // Para coroutines E desativa animadores (coroutines continuam mesmo com enabled=false)
        foreach (var g in todas)
        {
            if (g == null) continue;
            var e = g.GetComponent<EvoCardAnimador>();
            if (e != null) { e.StopAllCoroutines(); e.enabled = false; }
            var s = g.GetComponent<CartaSkillAnimador>();
            if (s != null) { s.StopAllCoroutines(); s.enabled = false; }
        }
        // Garante o card selecionado (pode não estar em 'todas')
        {
            var e = cartaGO.GetComponent<EvoCardAnimador>();
            if (e != null) { e.StopAllCoroutines(); e.enabled = false; }
            var s = cartaGO.GetComponent<CartaSkillAnimador>();
            if (s != null) { s.StopAllCoroutines(); s.enabled = false; }
        }

        // Outras cartas
        var outras = new List<RectTransform>();
        var outrasCG = new List<CanvasGroup>();
        foreach (var g in todas)
        {
            if (g == null || g == cartaGO) continue;
            var rt = g.GetComponent<RectTransform>();
            if (rt == null) continue;
            outras.Add(rt);
            var cg = g.GetComponent<CanvasGroup>();
            if (cg == null) cg = g.AddComponent<CanvasGroup>();
            outrasCG.Add(cg);
        }

        // Canvas que contém a carta (respeita painelEvo com sortingOrder alto)
        Canvas canvas = carta.GetComponentInParent<Canvas>();
        var canvasRT = canvas?.GetComponent<RectTransform>();

        // ── Fase 1: carta cresce (×1.2), outras somem (0.8 s) ───────────
        float t = 0f, dur = 0.8f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / dur);
            carta.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.2f, p);
            for (int i = 0; i < outras.Count; i++)
            {
                if (outras[i] == null) continue;
                outras[i].localScale = Vector3.Lerp(Vector3.one, Vector3.zero, p);
                outrasCG[i].alpha = Mathf.Lerp(1f, 0f, p);
            }
            yield return null;
        }
        foreach (var o in outras) if (o != null) Destroy(o.gameObject);

        // ── Fase 2: ir ao centro da tela (1.0 s) ────────────────────────
        Vector3[] corners = new Vector3[4];
        carta.GetWorldCorners(corners);
        Vector2 screenPos = (corners[0] + corners[2]) / 2f;

        // Ignora qualquer LayoutGroup pai para não brigar com anchoredPosition
        var le = cartaGO.GetComponent<LayoutElement>();
        if (le != null) le.ignoreLayout = true;

        carta.SetParent(canvasRT, false);
        carta.anchorMin = carta.anchorMax = new Vector2(0.5f, 0.5f);
        carta.pivot = new Vector2(0.5f, 0.5f);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, screenPos, canvas.worldCamera, out Vector2 posInicial);
        carta.anchoredPosition = posInicial;
        carta.localScale = Vector3.one * 1.2f;

        t = 0f; dur = 1.0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / dur);
            carta.anchoredPosition = Vector2.Lerp(posInicial, Vector2.zero, p);
            carta.localScale = Vector3.one * Mathf.Lerp(1.2f, 1.0f, p);
            yield return null;
        }
        carta.anchoredPosition = Vector2.zero;
        carta.localScale = Vector3.one;

        // ── Fase 3: pulso de chegada (0.45 s) ───────────────────────────
        t = 0f; dur = 0.45f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            carta.localScale = Vector3.one * (1f + 0.22f * Mathf.Sin(t / dur * Mathf.PI));
            yield return null;
        }
        carta.localScale = Vector3.one;

        // ── Fase 4: explodir em bolinhas de energia ──────────────────────
        Vector2 playerCanvasPos = Vector2.zero;
        var playerGO = GameObject.FindWithTag("Player")
                    ?? FindAnyObjectByType<PlayerStats>()?.gameObject;
        if (playerGO != null && Camera.main != null && canvasRT != null)
        {
            Vector3 ps = Camera.main.WorldToScreenPoint(playerGO.transform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRT, ps, canvas.worldCamera, out playerCanvasPos);
        }

        foreach (var img in cartaGO.GetComponentsInChildren<Image>(true))
            img.enabled = false;
        foreach (var txt in cartaGO.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
            txt.enabled = false;

        // Paleta azul e branca
        Color[] cores = {
            new Color(0.4f,  0.82f, 1f),    // azul claro
            new Color(0.6f,  0.92f, 1f),    // azul pálido
            new Color(1f,    1f,    1f),    // branco puro
            new Color(0.2f,  0.6f,  1f),   // azul médio
            new Color(0.85f, 0.96f, 1f),   // quase branco azulado
            new Color(0.5f,  0.85f, 1f),   // azul suave
        };

        // Grade de origem dos fragmentos (onde a carta estava)
        int cols = 5, rows = 7;
        int numFrags = cols * rows;
        Vector2 tamanho = carta.sizeDelta;
        float lf = tamanho.x / cols;
        float af = tamanho.y / rows;

        var frags     = new RectTransform[numFrags];
        var imgCore   = new Image[numFrags];       // círculo interno brilhante
        var imgAura   = new Image[numFrags];       // aura externa suave
        var fragPos0  = new Vector2[numFrags];
        var fragVel   = new Vector2[numFrags];
        var tamanhos  = new float[numFrags];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int idx = r * cols + c;
                float px = (c - cols * 0.5f + 0.5f) * lf + Random.Range(-lf * 0.2f, lf * 0.2f);
                float py = (r - rows * 0.5f + 0.5f) * af + Random.Range(-af * 0.2f, af * 0.2f);
                Color cor = cores[idx % cores.Length];
                float sz  = Random.Range(14f, 30f);
                tamanhos[idx] = sz;

                // Container da bolinha
                var fGO = new GameObject($"Orb{idx}");
                fGO.transform.SetParent(canvasRT, false);
                var fRT = fGO.AddComponent<RectTransform>();
                fRT.anchorMin = fRT.anchorMax = new Vector2(0.5f, 0.5f);
                fRT.pivot = new Vector2(0.5f, 0.5f);
                fRT.sizeDelta = Vector2.one * sz;
                fRT.anchoredPosition = new Vector2(px, py);

                // Aura externa (2.5× o tamanho, muito transparente)
                var auraGO = new GameObject("Aura");
                auraGO.transform.SetParent(fGO.transform, false);
                var auraRT = auraGO.AddComponent<RectTransform>();
                auraRT.anchorMin = auraRT.anchorMax = new Vector2(0.5f, 0.5f);
                auraRT.pivot = new Vector2(0.5f, 0.5f);
                auraRT.sizeDelta = Vector2.one * sz * 2.6f;
                auraRT.anchoredPosition = Vector2.zero;
                var aImg = auraGO.AddComponent<Image>();
                aImg.sprite = _glow;
                aImg.color  = new Color(cor.r, cor.g, cor.b, 0.28f);
                aImg.raycastTarget = false;

                // Núcleo brilhante (tamanho normal)
                var coreGO = new GameObject("Core");
                coreGO.transform.SetParent(fGO.transform, false);
                var coreRT = coreGO.AddComponent<RectTransform>();
                coreRT.anchorMin = coreRT.anchorMax = new Vector2(0.5f, 0.5f);
                coreRT.pivot = new Vector2(0.5f, 0.5f);
                coreRT.sizeDelta = Vector2.one * sz;
                coreRT.anchoredPosition = Vector2.zero;
                var cImg = coreGO.AddComponent<Image>();
                cImg.sprite = _glow;
                cImg.color  = new Color(cor.r, cor.g, cor.b, 0.95f);
                cImg.raycastTarget = false;

                // Velocidade de burst radial + jitter
                Vector2 dir = new Vector2(px, py);
                if (dir.sqrMagnitude < 1f) dir = Random.insideUnitCircle;
                dir.Normalize();
                fragVel[idx]  = dir * Random.Range(100f, 260f) + (Vector2)Random.insideUnitCircle * 70f;
                fragPos0[idx] = new Vector2(px, py);

                frags[idx]   = fRT;
                imgCore[idx] = cImg;
                imgAura[idx] = aImg;
            }
        }

        Destroy(cartaGO);

        // Guarda posição de tela do player para o efeito de absorção
        Vector2 playerScreenPos = Vector2.zero;
        if (playerGO != null && Camera.main != null)
            playerScreenPos = Camera.main.WorldToScreenPoint(playerGO.transform.position);

        float durBurst = 0.5f;
        float durVoo   = 2.0f;
        float durTotal = durBurst + durVoo;
        t = 0f;

        while (t < durTotal)
        {
            t += Time.unscaledDeltaTime;

            for (int i = 0; i < numFrags; i++)
            {
                if (frags[i] == null) continue;

                Vector2 pos;
                float   alpha;
                float   escala;

                if (t <= durBurst)
                {
                    float pb = Mathf.SmoothStep(0f, 1f, t / durBurst);
                    pos    = fragPos0[i] + fragVel[i] * pb;
                    alpha  = 1f;
                    // Pulsa levemente durante o burst
                    escala = 1f + 0.15f * Mathf.Sin(t * 20f + i);
                }
                else
                {
                    float pv   = Mathf.SmoothStep(0f, 1f, (t - durBurst) / durVoo);
                    Vector2 p0 = fragPos0[i] + fragVel[i];
                    pos    = Vector2.Lerp(p0, playerCanvasPos, pv);
                    alpha  = Mathf.Clamp01(1f - Mathf.Pow(pv, 1.4f));
                    escala = Mathf.Lerp(1f, 0.05f, Mathf.Pow(pv, 2f));
                }

                frags[i].anchoredPosition = pos;
                frags[i].localScale       = Vector3.one * escala;

                var cc = imgCore[i].color;
                imgCore[i].color = new Color(cc.r, cc.g, cc.b, alpha * 0.95f);
                var ca = imgAura[i].color;
                imgAura[i].color = new Color(ca.r, ca.g, ca.b, alpha * 0.28f);
            }

            yield return null;
        }

        for (int i = 0; i < numFrags; i++)
            if (frags[i] != null) Destroy(frags[i].gameObject);

        // Efeito de absorção no player
        StartCoroutine(EfeitoAbsorcaoPlayer(playerScreenPos, playerGO));

        callback?.Invoke();
    }

    // Canvas próprio para o efeito no player — não é destruído pelo LimparTudo do painelEvo
    IEnumerator EfeitoAbsorcaoPlayer(Vector2 playerScreenPos, GameObject playerGO)
    {
        var containerGO = new GameObject("AbsorcaoEfeito");
        var cvs = containerGO.AddComponent<Canvas>();
        cvs.renderMode   = RenderMode.ScreenSpaceOverlay;
        cvs.sortingOrder = 300;
        containerGO.AddComponent<GraphicRaycaster>();
        var cvsRT = containerGO.GetComponent<RectTransform>();

        Vector2 canvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            cvsRT, playerScreenPos, null, out canvasPos);

        // Flash central que expande
        StartCoroutine(AnimarFlashCentral(cvsRT, canvasPos));

        // 2 anéis expansivos com delay entre eles
        for (int i = 0; i < 2; i++)
            StartCoroutine(AnimarAnel(cvsRT, canvasPos, i * 0.2f));

        // Flash no sprite do player
        if (playerGO != null)
        {
            var sr = playerGO.GetComponent<SpriteRenderer>();
            if (sr != null) StartCoroutine(FlashPlayerSprite(sr));
        }

        yield return new WaitForSecondsRealtime(1.4f);
        if (containerGO != null) Destroy(containerGO);
    }

    IEnumerator AnimarFlashCentral(RectTransform pai, Vector2 pos)
    {
        var go = new GameObject("Flash");
        go.transform.SetParent(pai, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = Vector2.one * 10f;
        var img = go.AddComponent<Image>();
        img.sprite = _glow;
        img.color = new Color(0.7f, 0.95f, 1f, 1f);
        img.raycastTarget = false;

        float dur = 0.45f;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            if (go == null) yield break;
            float p = t / dur;
            rt.sizeDelta = Vector2.one * Mathf.Lerp(8f, 70f, Mathf.Pow(p, 0.4f));
            img.color = new Color(0.7f, 0.95f, 1f, Mathf.Lerp(0.7f, 0f, Mathf.Pow(p, 1.5f)));
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator AnimarAnel(RectTransform pai, Vector2 pos, float delay)
    {
        for (float d = 0f; d < delay; d += Time.unscaledDeltaTime) yield return null;

        var go = new GameObject("Anel");
        go.transform.SetParent(pai, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = Vector2.one * 20f;
        var img = go.AddComponent<Image>();
        img.sprite = _glow;
        img.color = new Color(0.4f, 0.82f, 1f, 0.75f);
        img.raycastTarget = false;

        float dur = 0.65f;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            if (go == null) yield break;
            float p = Mathf.SmoothStep(0f, 1f, t / dur);
            rt.sizeDelta = Vector2.one * Mathf.Lerp(15f, 120f, p);
            img.color = new Color(0.4f, 0.82f, 1f, Mathf.Lerp(0.45f, 0f, p));
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator FlashPlayerSprite(SpriteRenderer sr)
    {
        Color original = sr.color;
        Color flash    = new Color(0.7f, 0.95f, 1f);
        float dur      = 0.35f;

        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            if (sr == null) yield break;
            sr.color = Color.Lerp(original, flash, Mathf.Sin(t / dur * Mathf.PI));
            yield return null;
        }

        if (sr != null) sr.color = original;
    }
}
