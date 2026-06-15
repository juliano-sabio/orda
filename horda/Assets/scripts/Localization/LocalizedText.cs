using UnityEngine;
using TMPro;

// Add this component alongside any TextMeshProUGUI to auto-translate.
// Set 'key' to the localization key (e.g. "pause.resume").
[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    public string key;

    TextMeshProUGUI _tmp;

    void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
        Loc.OnLanguageChanged += Apply;
    }

    void Start() => Apply(Loc.Current);

    void OnDestroy() => Loc.OnLanguageChanged -= Apply;

    void Apply(Language _) => _tmp.text = Loc.T(key);
}
