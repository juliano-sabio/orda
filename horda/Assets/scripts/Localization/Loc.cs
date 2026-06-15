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

    public static void Reload()
    {
        _initialized = false;
        _data = null;
        Init();
    }
}
