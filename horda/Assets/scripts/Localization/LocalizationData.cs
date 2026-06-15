using UnityEngine;

[System.Serializable]
public class LanguageEntry
{
    public string key;
    // Index matches Language enum: 0=PT_BR 1=EN 2=ES 3=DE 4=FR 5=IT 6=RU 7=PL 8=TR 9=ZH 10=JA 11=KO 12=NL 13=ID
    public string[] translations;
}

[CreateAssetMenu(fileName = "GameStrings", menuName = "Horda/Localization/GameStrings")]
public class LocalizationData : ScriptableObject
{
    public LanguageEntry[] entries;

    public string Get(string key, Language lang)
    {
        if (entries == null) return key;
        foreach (var e in entries)
        {
            if (e.key != key || e.translations == null) continue;
            int idx = (int)lang;
            if (idx < e.translations.Length && !string.IsNullOrEmpty(e.translations[idx]))
                return e.translations[idx];
            // fallback to PT-BR
            return e.translations.Length > 0 ? e.translations[0] : key;
        }
        return key;
    }
}
