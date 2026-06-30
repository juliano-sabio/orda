using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Música de fundo ADAPTATIVA — independente dos SFX (SomSkill/UISons). Toca o tema de batalha nas
// fases, troca pro tema de BOSS enquanto há boss em luta, e um tema calmo no menu/lobby. Os clipes
// ficam em Resources/music/ (placeholders das prévias; trocáveis pelos exports finais do Sonar com
// o MESMO nome). Crossfade suave entre faixas. Tudo LOCAL — cada cliente toca o seu (sem rede).
public class MusicManager : MonoBehaviour
{
    enum Faixa { Nenhuma, Batalha, Boss, Menu }

    static MusicManager _inst;

    AudioSource _a, _b, _ativo;          // dois canais pra crossfade
    AudioClip _batalha, _boss, _menu;
    Faixa _faixaAtual = Faixa.Nenhuma;
    Coroutine _fade;

    [Range(0f, 1f)] public float volume = 0.5f;
    [Tooltip("Multiplicador FIXO do volume da música — deixa o tema mais baixo (sit under os SFX) sem mexer no slider.")]
    [Range(0f, 1f)] public float volumeBase = 0.7f;
    public float fadeDur = 1.5f;

    // Volume real aplicado nas fontes: slider × base. A base baixa a música como um todo.
    float VolAlvo => Mathf.Clamp01(volume) * volumeBase;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCriar()
    {
        if (_inst != null) return;
        var go = new GameObject("MusicManager");
        DontDestroyOnLoad(go);
        _inst = go.AddComponent<MusicManager>();
    }

    void Awake()
    {
        if (_inst != null && _inst != this) { Destroy(gameObject); return; }
        _inst = this;

        // Volume vindo das opções (slider "Música" do PauseManager). Mesmo default do slider (0.6).
        volume = PlayerPrefs.GetFloat("MusicVolume", 0.6f);

        _batalha = Resources.Load<AudioClip>("music/trilha_horda");
        _boss    = Resources.Load<AudioClip>("music/trilha_boss");
        _menu    = Resources.Load<AudioClip>("music/trilha_interludio");

        _a = CriarSource();
        _b = CriarSource();
        _ativo = _a;
    }

    AudioSource CriarSource()
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.loop = true; s.playOnAwake = false; s.volume = 0f; s.spatialBlend = 0f;
        s.ignoreListenerPause = true; // música continua na pausa (não corta no menu de pause)
        return s;
    }

    void Update()
    {
        var desejada = FaixaDesejada();
        if (desejada != _faixaAtual) Trocar(desejada);
        // Mantém o canal ativo no volume-alvo (caso 'volume' mude em runtime via opções).
        if (_fade == null && _ativo != null && _ativo.isPlaying) _ativo.volume = VolAlvo;
    }

    Faixa FaixaDesejada()
    {
        string cena = SceneManager.GetActiveScene().name;
        if (GerenciadorEventos.EhCenaDeJogo(cena))
            // BossesVivos conta o boss nas duas máquinas (host e cliente) → P2 também ouve o tema.
            return InimigoController.BossesVivos > 0 ? Faixa.Boss : Faixa.Batalha;
        return Faixa.Menu;
    }

    AudioClip ClipDe(Faixa f) => f == Faixa.Boss ? _boss : (f == Faixa.Menu ? _menu : _batalha);

    void Trocar(Faixa nova)
    {
        _faixaAtual = nova;
        var clip = ClipDe(nova);
        if (clip == null) return; // placeholder ausente → não troca (sem crash)
        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(Crossfade(clip));
    }

    IEnumerator Crossfade(AudioClip novo)
    {
        var antigo = _ativo;
        var entra  = (antigo == _a) ? _b : _a;
        entra.clip = novo; entra.volume = 0f; entra.Play();
        _ativo = entra;

        float t = 0f;
        float v0 = antigo != null ? antigo.volume : 0f;
        while (t < fadeDur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeDur);
            entra.volume = VolAlvo * p;
            if (antigo != null) antigo.volume = v0 * (1f - p);
            yield return null;
        }
        entra.volume = VolAlvo;
        if (antigo != null) { antigo.Stop(); antigo.volume = 0f; }
        _fade = null;
    }

    // API pública pras opções de áudio (item futuro): ajusta e persiste o volume da música.
    public static void DefinirVolume(float v)
    {
        v = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("MusicVolume", v);
        if (_inst != null) _inst.volume = v;
    }
}
