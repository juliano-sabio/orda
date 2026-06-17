using UnityEngine;

public static class Loc
{
    const string ResourcePath = "Localization/GameStrings";
    const string PrefKey      = "horda_language";

    static Language _current;
    static LocalizationData _data;
    static bool _initialized;

    // Names displayed in their own language, index = (int)Language
    public static readonly string[] NativeNames =
    {
        "Português (BR)", "English", "Español", "Deutsch", "Français",
        "Italiano", "Русский", "Polski", "Türkçe", "中文",
        "日本語", "한국어", "Nederlands", "Bahasa Indonesia"
    };

    public static Language Current
    {
        get { Init(); return _current; }
        set
        {
            Init();
            _current = value;
            PlayerPrefs.SetInt(PrefKey, (int)value);
            if (OnLanguageChanged != null) OnLanguageChanged(value);
        }
    }

    public static System.Action<Language> OnLanguageChanged;

    static void Init()
    {
        if (_initialized) return;
        _initialized = true;
        _current = (Language)PlayerPrefs.GetInt(PrefKey, (int)Language.PT_BR);
        _data = Resources.Load<LocalizationData>(ResourcePath);
        if (_data == null)
            Debug.LogWarning("[Loc] GameStrings não encontrado em Resources/" + ResourcePath);
    }

    public static string T(string key)
    {
        Init();
        return _data != null ? _data.Get(key, _current) : key;
    }

    public static string NativeName(Language l)
    {
        int idx = (int)l;
        return idx < NativeNames.Length ? NativeNames[idx] : l.ToString();
    }

    // Mapa fixo de nomes literais (PT) das skills básicas embutidas em
    // PlayerStats (AttackSkill/DefenseSkill/UltimateSkill) para suas chaves
    // de localização. Esses objetos não são ScriptableObjects e não têm
    // nameKey próprio, então o nome literal continua sendo usado como
    // identificador interno (ex.: matching de SkillModifier) — esta função
    // serve só para resolver o texto exibido na UI.
    static readonly System.Collections.Generic.Dictionary<string, string> _builtinSkillKeys =
        new System.Collections.Generic.Dictionary<string, string>
    {
        { "Ataque Automático",  "builtin_skill.auto_attack" },
        { "Golpe Contínuo",     "builtin_skill.continuous_strike" },
        { "Proteção Passiva",   "builtin_skill.passive_protection" },
        { "Escudo Automático",  "builtin_skill.auto_shield" },
        { "Fúria do Herói",     "builtin_skill.hero_fury" },
    };

    public static string SkillLabel(string nomeLiteral)
    {
        if (!string.IsNullOrEmpty(nomeLiteral) && _builtinSkillKeys.TryGetValue(nomeLiteral, out var key))
            return T(key);
        return nomeLiteral;
    }

    public static void Reload()
    {
        _initialized = false;
        _data = null;
        Init();
    }
}
