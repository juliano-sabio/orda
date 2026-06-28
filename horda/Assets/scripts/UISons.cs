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
        Tocar(_hover ??= GerarBlip(900f, 0.05f, 70f, 0f), 0.22f);
    }

    public static void Click()
    {
        // Mais suave/quente que o "beep" anterior: grave, pouco ruído, decay um pouco mais longo.
        Tocar(_click ??= GerarBlip(380f, 0.09f, 38f, 0.05f), 0.32f);
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

    // Blip curto: seno + harmônico, envelope com ataque de 2ms (sem "pop") e decay exponencial.
    // 'ruido' > 0 adiciona um ataque ruidoso curtinho (dá a sensação de "click").
    static AudioClip GerarBlip(float freq, float dur, float decay, float ruido)
    {
        const int sr = 44100;
        int n = Mathf.Max(1, (int)(sr * dur));
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sr;
            float env = Mathf.Exp(-t * decay);
            float atk = Mathf.Clamp01(t / 0.002f);
            float w = Mathf.Sin(2f * Mathf.PI * freq * t)
                    + 0.3f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t);
            if (ruido > 0f) w += ruido * (Random.value * 2f - 1f) * Mathf.Exp(-t * 200f);
            data[i] = w * env * atk * 0.5f;
        }
        var clip = AudioClip.Create("uiblip", n, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }
}

// Por-botão: toca hover ao entrar e click ao apertar. Botões não-interativos (disabled) não
// recebem eventos de ponteiro → sem som, como esperado.
[DisallowMultipleComponent]
public class UISomBotao : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public void OnPointerEnter(PointerEventData _) => UISons.Hover();
    public void OnPointerClick(PointerEventData _)  => UISons.Click();
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
