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
    AudioClip[] _batalhas;               // variações do tema de batalha (sorteadas)
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

        // Variações do tema de batalha — sorteia uma ao ENTRAR em batalha (runs mais variadas;
        // também troca ao voltar da luta de boss). Basta soltar mais "trilha_hordaN" na pasta.
        var vars = new System.Collections.Generic.List<AudioClip>();
        foreach (var n in new[] { "trilha_horda", "trilha_horda2", "trilha_horda3" })
        {
            var c = Resources.Load<AudioClip>("music/" + n);
            if (c != null) vars.Add(c);
        }
        _batalhas = vars.ToArray();
        _batalha  = _batalhas.Length > 0 ? _batalhas[0] : null;

        _boss    = Resources.Load<AudioClip>("music/trilha_boss");
        _menu    = Resources.Load<AudioClip>("music/trilha_interludio");

        _a = CriarSource();
        _b = CriarSource();
        _ativo = _a;

        // Reinicia a música do zero sempre que uma cena (re)carrega — se a fase resetar (morrer/
        // recomeçar) ou trocar, a faixa recomeça em vez de continuar de onde parou.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_fade != null) { StopCoroutine(_fade); _fade = null; }
        if (_a != null) _a.Stop();
        if (_b != null) _b.Stop();
        _faixaAtual = Faixa.Nenhuma;   // força o Update a recomeçar a faixa certa do início
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
        // Entrando em batalha (início de run, ou voltando da luta de boss): sorteia a variação.
        if (nova == Faixa.Batalha && _batalhas != null && _batalhas.Length > 0)
            _batalha = _batalhas[Random.Range(0, _batalhas.Length)];
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
