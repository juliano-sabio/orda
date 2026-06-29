using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sons de UI (hover/click) gerados PROCEDURALMENTE — sem arquivo .wav, sem depender dos assets
// de áudio do parceiro. Tocam em 2D ignorando o pause (menus pausam o jogo). Auto-plugados em
// todos os Button via UISomRunner (re-scan leve, pega também os criados dinamicamente).
public static class UISons
{
    static AudioClip _hover, _click;
    static float _ultimoHover; // anti-spam: ao varrer botões o mouse dispara vários PointerEnter

    public static void Hover()
    {
        if (Time.unscaledTime - _ultimoHover < 0.04f) return;
        _ultimoHover = Time.unscaledTime;
        // "pop" macio com leve queda de tom (estilo UI de survivor moderno), não um beep seco.
        Tocar(_hover ??= GerarBlip(820f, 600f, 0.055f, 55f, 0f), 0.18f);
    }

    public static void Click()
    {
        // Confirma macio/quente: tom grave com glide pra baixo + pouco ruído de corpo.
        Tocar(_click ??= GerarBlip(460f, 320f, 0.10f, 32f, 0.04f), 0.30f);
    }

    static void Tocar(AudioClip clip, float vol)
    {
        if (clip == null) return;
        var go = new GameObject("SomUI");
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = vol;
        src.spatialBlend = 0f;          // 2D
        src.ignoreListenerPause = true; // toca mesmo com o jogo pausado (menus)
        src.Play();
        Object.Destroy(go, clip.length + 0.05f);
    }

    // Blip curto com GLIDE de tom (freqIni → freqFim) pra dar sensação de "pop" suave em vez de
    // beep. Seno + harmônico fraco, ataque ~4ms (macio) e decay exponencial. Fase acumulada pra
    // o glide não estalar. 'ruido' > 0 = ataque ruidoso curto (corpo do "click").
    static AudioClip GerarBlip(float freqIni, float freqFim, float dur, float decay, float ruido)
    {
        const int sr = 44100;
        int n = Mathf.Max(1, (int)(sr * dur));
        var data = new float[n];
        float fase = 0f, fase2 = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sr;
            float p = t / dur;
            float freq = Mathf.Lerp(freqIni, freqFim, p);
            fase  += 2f * Mathf.PI * freq        / sr;
            fase2 += 2f * Mathf.PI * freq * 2f   / sr;
            float env = Mathf.Exp(-t * decay);
            float atk = Mathf.Clamp01(t / 0.004f);   // ataque mais macio (4ms)
            float w = Mathf.Sin(fase) + 0.18f * Mathf.Sin(fase2);
            if (ruido > 0f) w += ruido * (Random.value * 2f - 1f) * Mathf.Exp(-t * 200f);
            data[i] = w * env * atk * 0.5f;
        }
        var clip = AudioClip.Create("uiblip", n, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }
}

// Marca um container (e seus filhos) pra NÃO tocar o som de hover. Ex.: panels de passiva/ultimate.
public class SemSomUI : MonoBehaviour { }

// Por-botão: som de hover + efeito de "subir/crescer" ao passar o mouse. (Sem som de click —
// removido a pedido.) Botões dentro de um SemSomUI não tocam o hover (mas ainda têm o efeito).
[DisallowMultipleComponent]
public class UISomBotao : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    const float ALTURA = 8f;     // sobe X px no hover
    const float ESCALA = 1.05f;  // cresce um tico
    const float VEL    = 12f;    // suavidade do lerp

    RectTransform rt;
    Vector2 posBase;
    Vector3 escBase;
    bool baseOk, hover, semSom, semCalc;

    void Awake() => rt = transform as RectTransform;

    public void OnPointerEnter(PointerEventData _)
    {
        if (!semCalc) { semSom = GetComponentInParent<SemSomUI>() != null; semCalc = true; }
        if (!baseOk && rt != null) { posBase = rt.anchoredPosition; escBase = rt.localScale; baseOk = true; }
        hover = true;
        if (!semSom) UISons.Hover();
    }

    public void OnPointerExit(PointerEventData _) => hover = false;

    void Update()
    {
        if (!baseOk || rt == null) return;
        Vector2 ap = hover ? posBase + Vector2.up * ALTURA : posBase;
        Vector3 ae = hover ? escBase * ESCALA : escBase;
        // já assentado e sem hover → não fica lerpando à toa
        if (!hover && (rt.anchoredPosition - posBase).sqrMagnitude < 0.01f
                   && (rt.localScale - escBase).sqrMagnitude < 0.0001f) return;
        rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, ap, Time.unscaledDeltaTime * VEL);
        rt.localScale       = Vector3.Lerp(rt.localScale,       ae, Time.unscaledDeltaTime * VEL);
    }
}

// Bootstrap persistente: a cada meio segundo varre os Button e pluga o UISomBotao onde faltar
// (cobre botões criados dinamicamente em runtime — cartas, roster do lobby, etc.).
public class UISomRunner : MonoBehaviour
{
    static UISomRunner _i;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (_i != null) return;
        var go = new GameObject("UISomRunner");
        DontDestroyOnLoad(go);
        _i = go.AddComponent<UISomRunner>();
    }

    void OnEnable()  => SceneManager.sceneLoaded += AoCarregarCena;
    void OnDisable() => SceneManager.sceneLoaded -= AoCarregarCena;
    void AoCarregarCena(Scene s, LoadSceneMode m) => Plugar();

    void Start() => StartCoroutine(Loop());

    IEnumerator Loop()
    {
        var espera = new WaitForSecondsRealtime(0.5f);
        while (true) { Plugar(); yield return espera; }
    }

    static void Plugar()
    {
        var botoes = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in botoes)
            if (b.GetComponent<UISomBotao>() == null)
                b.gameObject.AddComponent<UISomBotao>();
    }
}
