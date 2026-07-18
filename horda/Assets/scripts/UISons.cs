using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sons de UI gerados PROCEDURALMENTE, mirando a vibe do The Spell Brigade (Austin Wintory): NÃO
// é beep eletrônico — mistura RUÍDO de madeira/pergaminho (crackle/swish) com CHIME/sino mágico
// (parciais inharmônicos). Hover = "flick/swish + estalo de papel"; click = "clack + blip mágico".
// Toca em 2D ignorando o pause. (Aproximação procedural; pra ficar idêntico precisaria de um .wav.)
public static class UISons
{
    const int SR = 44100;
    static AudioClip _hover, _click;
    static float _ultimoHover; // anti-spam (varredura de botões dispara vários PointerEnter)

    public static void Hover()
    {
        if (Time.unscaledTime - _ultimoHover < 0.04f) return;
        _ultimoHover = Time.unscaledTime;
        if (_hover == null)
            _hover = Mixar("uihover",
                Ruido(0.05f, 50f, 16, 0.18f));     // swish macio e abafado (papel/madeira), sem brilho metálico
        Tocar(_hover, 0.45f);
    }

    public static void Click()
    {
        if (_click == null)
            _click = Mixar("uiclick",
                Ruido(0.009f, 150f, 12, 0.08f),    // estalinho quase imperceptível
                Tom(210f, 175f, 0.05f, 62f, 0.26f)); // "tup" de madeira grave, curto e bem baixo (discreto)
        Tocar(_click, 0.3f);
    }

    static void Tocar(AudioClip clip, float vol)
    {
        if (clip == null) return;
        var go = new GameObject("SomUI");
        Object.DontDestroyOnLoad(go);   // sobrevive à troca de cena → o som não corta/buga
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = vol * AudioBus.SfxVolume;   // respeita o slider de SFX
        src.spatialBlend = 0f;          // 2D
        src.ignoreListenerPause = true; // toca mesmo com o jogo pausado (menus)
        src.Play();
        Object.Destroy(go, clip.length + 0.05f);
    }

    // ── Síntese ────────────────────────────────────────────────────────────────

    // Ruído com envelope + passa-baixa (média móvel de 'suav' amostras p/ lado). suav maior =
    // mais abafado/grave (madeira/pergaminho); menor = mais brilhante (estalo).
    static float[] Ruido(float dur, float decay, int suav, float ganho)
    {
        int n = Mathf.Max(1, (int)(SR * dur));
        var raw = new float[n];
        for (int i = 0; i < n; i++) raw[i] = Random.value * 2f - 1f;
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float acc = 0f; int cnt = 0;
            for (int k = -suav; k <= suav; k++) { int j = i + k; if (j >= 0 && j < n) { acc += raw[j]; cnt++; } }
            float t = i / (float)SR;
            float env = Mathf.Exp(-t * decay) * Mathf.Clamp01(t / 0.003f);
            data[i] = (cnt > 0 ? acc / cnt : 0f) * env * ganho;
        }
        return data;
    }

    // Batida de MADEIRA: harmônicos INTEIROS (1,2,3 → soa "de madeira", não metálico) com decay
    // rápido = um "tok"/"bok" curto, não um sino que ressoa. Glide de tom (freqIni→freqFim, fase
    // acumulada) dá um arredondamento de "confirm" satisfatório.
    static readonly float[] _ratios = { 1f, 2f, 3f };
    static readonly float[] _amps   = { 1f, 0.4f, 0.15f };
    static float[] Tom(float freqIni, float freqFim, float dur, float decay, float ganho)
    {
        int n = Mathf.Max(1, (int)(SR * dur));
        var data = new float[n];
        var fase = new float[_ratios.Length];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float p = t / dur;
            float freq = Mathf.Lerp(freqIni, freqFim, p);
            float env = Mathf.Exp(-t * decay) * Mathf.Clamp01(t / 0.002f);
            float w = 0f;
            for (int q = 0; q < _ratios.Length; q++)
            {
                fase[q] += 2f * Mathf.PI * freq * _ratios[q] / SR;
                w += _amps[q] * Mathf.Sin(fase[q]);
            }
            data[i] = w * env * ganho;
        }
        return data;
    }

    // Soma as camadas (comprimentos podem diferir) + clamp; cria o AudioClip.
    static AudioClip Mixar(string nome, params float[][] camadas)
    {
        int n = 0; foreach (var c in camadas) n = Mathf.Max(n, c.Length);
        var data = new float[n];
        foreach (var c in camadas) for (int i = 0; i < c.Length; i++) data[i] += c[i];
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
        var clip = AudioClip.Create(nome, n, 1, SR, false);
        clip.SetData(data, 0);
        return clip;
    }
}

// Marca um container (e seus filhos) pra NÃO tocar os sons de UI. Ex.: panels de passiva/ultimate/status.
public class SemSomUI : MonoBehaviour { }

// Por-botão: som de hover + click + efeito de "subir/crescer" ao passar o mouse. Botões dentro
// de um SemSomUI não tocam som (mas mantêm o efeito visual).
[DisallowMultipleComponent]
public class UISomBotao : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    const float ALTURA = 8f;     // sobe X px no hover
    const float ESCALA = 1.05f;  // cresce um tico
    const float VEL    = 12f;    // suavidade do lerp

    RectTransform rt;
    Canvas canvas;
    Vector2 posBase;
    Vector3 escBase;
    bool baseOk, hover, semSom, semCalc;
    bool temAnimadorProprio;   // objeto já tem hover próprio (CardHover/EvoCardAnimador) → não mexemos na escala/posição

    void Awake() { rt = transform as RectTransform; canvas = GetComponentInParent<Canvas>(); }

    // Reabrir um painel/cena com o mouse em cima deixava o botão preso no tamanho de hover (a base
    // era capturada no OnPointerEnter, que dispara cedo/errado, e o OnPointerExit às vezes não vem).
    // Ao (re)ativar, reseta: a base é recapturada só num frame de REPOUSO → nunca fica bugado.
    void OnEnable() { hover = false; baseOk = false; }

    void GarantirSemSom()
    {
        if (semCalc) return;
        semSom = GetComponentInParent<SemSomUI>() != null;
        semCalc = true;
    }

    public void OnPointerEnter(PointerEventData _)
    {
        GarantirSemSom();
        hover = true;
        if (!semSom) UISons.Hover();
    }

    public void OnPointerExit(PointerEventData _) => hover = false;

    public void OnPointerClick(PointerEventData _)
    {
        GarantirSemSom();
        if (!semSom) UISons.Click();
    }

    Camera CamUI()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        return (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
    }

    void Update()
    {
        if (rt == null) return;

        // Se o objeto já tem um animador de hover próprio (ex.: CardHover nos botões de pause,
        // EvoCardAnimador nas cartas de evolução), NÃO aplicamos o efeito visual — senão os dois
        // escrevem localScale/posição no mesmo frame e brigam (a carta/botão "cresce e encolhe").
        // O som continua funcionando (tratado nos handlers de ponteiro). Re-checa enquanto não achou,
        // pois o animador pode ser adicionado depois deste componente.
        if (!temAnimadorProprio)
            temAnimadorProprio = GetComponentInParent<CardHover>() != null
                              || GetComponentInParent<EvoCardAnimador>() != null
                              || GetComponentInParent<CartaSkillAnimador>() != null;
        if (temAnimadorProprio) return;

        // Captura a base SÓ em repouso (sem hover). Se o botão abriu com o mouse em cima, não captura
        // um valor bugado — só aplica o efeito depois de ter uma base limpa.
        if (!baseOk)
        {
            if (hover) return;
            posBase = rt.anchoredPosition; escBase = rt.localScale; baseOk = true;
        }

        // Auto-corrige hover preso: se acha que está em hover mas o mouse NÃO está sobre o botão, solta
        // (cobre o OnPointerExit que não dispara quando o painel abre sob o cursor).
        if (hover && !RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, CamUI()))
            hover = false;

        Vector2 ap = hover ? posBase + Vector2.up * ALTURA : posBase;
        Vector3 ae = hover ? escBase * ESCALA : escBase;
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
