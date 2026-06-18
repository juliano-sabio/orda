using UnityEngine;

// Teclas remapeáveis do jogo, persistidas em PlayerPrefs.
// Pontos de leitura: moviment_player2 (movimento + dash), player_stats e todas
// as ultimates (ultimate). A UI de configuração está em MenuInicialUI (aba Controles).
public static class InputBindings
{
    public enum Acao { Cima = 0, Baixo = 1, Esquerda = 2, Direita = 3, Dash = 4, Ultimate = 5 }

    static readonly KeyCode[] _padrao =
    {
        KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.Space, KeyCode.R
    };

    static KeyCode[] _teclas;

    static void Init()
    {
        if (_teclas != null) return;
        _teclas = new KeyCode[_padrao.Length];
        for (int i = 0; i < _padrao.Length; i++)
            _teclas[i] = (KeyCode)PlayerPrefs.GetInt(Chave(i), (int)_padrao[i]);
        // [DEBUG-REBIND] remover depois — mostra o que esta sessao carregou do PlayerPrefs
        Debug.Log($"[REBIND] Init em '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}': " +
                  $"Cima={_teclas[0]}, Baixo={_teclas[1]}, Esq={_teclas[2]}, Dir={_teclas[3]}, Dash={_teclas[4]}, Ult={_teclas[5]}");
    }

    static string Chave(int i) => "Bind_" + ((Acao)i);

    public static KeyCode Get(Acao a) { Init(); return _teclas[(int)a]; }

    public static KeyCode Padrao(Acao a) => _padrao[(int)a];

    public static void Set(Acao a, KeyCode k)
    {
        Init();
        _teclas[(int)a] = k;
        PlayerPrefs.SetInt(Chave((int)a), (int)k);
        PlayerPrefs.Save();
        // [DEBUG-REBIND] remover depois — confirma gravacao e releitura do PlayerPrefs
        int relido = PlayerPrefs.GetInt(Chave((int)a), -999);
        Debug.Log($"[REBIND] Set {a} = {k} ({(int)k}); PlayerPrefs relido = {(KeyCode)relido} ({relido})");
    }

    public static void ResetarPadrao()
    {
        Init();
        for (int i = 0; i < _padrao.Length; i++)
            Set((Acao)i, _padrao[i]);
    }

    // ── Leitura de gameplay ──────────────────────────────────────────────────

    public static bool UltimateDown() { Init(); return Input.GetKeyDown(_teclas[(int)Acao.Ultimate]); }
    public static bool DashDown()     { Init(); return Input.GetKeyDown(_teclas[(int)Acao.Dash]); }

    public static Vector2 EixoMovimento()
    {
        Init();
        float x = 0f, y = 0f;
        if (Input.GetKey(_teclas[(int)Acao.Direita]))  x += 1f;
        if (Input.GetKey(_teclas[(int)Acao.Esquerda])) x -= 1f;
        if (Input.GetKey(_teclas[(int)Acao.Cima]))     y += 1f;
        if (Input.GetKey(_teclas[(int)Acao.Baixo]))    y -= 1f;
        return new Vector2(x, y);
    }

    // ── Exibição ─────────────────────────────────────────────────────────────

    // true para teclas válidas de rebind (teclado + mouse principal), excluindo joystick
    public static bool EhTeclaValida(KeyCode k)
    {
        if (k == KeyCode.None) return false;
        if (k >= KeyCode.JoystickButton0) return false; // exclui controles/joystick
        return true;
    }

    public static string NomeTecla(KeyCode k)
    {
        switch (k)
        {
            case KeyCode.Space:        return "Espaço";
            case KeyCode.LeftArrow:    return "←";
            case KeyCode.RightArrow:   return "→";
            case KeyCode.UpArrow:      return "↑";
            case KeyCode.DownArrow:    return "↓";
            case KeyCode.LeftShift:    return "Shift";
            case KeyCode.RightShift:   return "Shift dir.";
            case KeyCode.LeftControl:  return "Ctrl";
            case KeyCode.RightControl: return "Ctrl dir.";
            case KeyCode.LeftAlt:      return "Alt";
            case KeyCode.Return:       return "Enter";
            case KeyCode.Tab:          return "Tab";
            case KeyCode.Mouse0:       return "Mouse Esq.";
            case KeyCode.Mouse1:       return "Mouse Dir.";
            case KeyCode.Mouse2:       return "Mouse Meio";
        }
        string s = k.ToString();
        if (s.StartsWith("Alpha"))   return s.Substring(5);
        if (s.StartsWith("Keypad"))  return "Num " + s.Substring(6);
        return s;
    }
}
