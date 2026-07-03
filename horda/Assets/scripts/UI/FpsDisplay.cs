using UnityEngine;

// Contador de FPS no canto da tela. Aparece só quando a opção "Mostrar FPS"
// (pref ShowFPS==1, aba Jogo das opções) está ligada. Auto-anexa — não precisa
// estar em cena. O toggle chama AtualizarPreferencia() pra refletir ao vivo.
public class FpsDisplay : MonoBehaviour
{
    static FpsDisplay _i;
    static int _ativoCache = -1; // -1 = ainda não lido

    static bool Ativo
    {
        get { if (_ativoCache < 0) _ativoCache = PlayerPrefs.GetInt("ShowFPS", 0); return _ativoCache == 1; }
    }
    public static void AtualizarPreferencia() => _ativoCache = PlayerPrefs.GetInt("ShowFPS", 0);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Auto()
    {
        if (_i != null) return;
        var go = new GameObject("FpsDisplay");
        DontDestroyOnLoad(go);
        _i = go.AddComponent<FpsDisplay>();
    }

    float _fps;
    GUIStyle _estilo;

    void Update()
    {
        // Média exponencial — número estável, sem tremer a cada frame.
        float atual = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        _fps = Mathf.Lerp(_fps <= 0f ? atual : _fps, atual, 0.1f);
    }

    void OnGUI()
    {
        if (!Ativo) return;

        if (_estilo == null)
        {
            _estilo = new GUIStyle(GUI.skin.label)
            {
                fontSize  = Mathf.Max(12, Screen.height / 54),
                alignment = TextAnchor.UpperRight,
                fontStyle = FontStyle.Bold,
            };
        }
        // Cor por faixa (verde ≥55, amarelo ≥30, vermelho abaixo).
        _estilo.normal.textColor = _fps >= 55f ? new Color(0.5f, 1f, 0.5f)
                                 : _fps >= 30f ? new Color(1f, 0.9f, 0.4f)
                                 :               new Color(1f, 0.45f, 0.4f);

        int fps = Mathf.RoundToInt(_fps);
        var r = new Rect(Screen.width - 118f, 6f, 110f, 26f);
        // Sombra pra legibilidade sobre qualquer fundo
        var corAntiga = _estilo.normal.textColor;
        _estilo.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
        GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), $"{fps} FPS", _estilo);
        _estilo.normal.textColor = corAntiga;
        GUI.Label(r, $"{fps} FPS", _estilo);
    }
}
