using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LanguageSelectorUI : MonoBehaviour
{
    public Button btnPrev;
    public Button btnNext;
    public Button btnConfirm;
    public TextMeshProUGUI langNameText;

    int _previewIdx;
    static readonly int LangCount = System.Enum.GetValues(typeof(Language)).Length;

    void Start()
    {
        _previewIdx = (int)Loc.Current;
        if (btnPrev    != null) btnPrev.onClick.AddListener(Prev);
        if (btnNext    != null) btnNext.onClick.AddListener(Next);
        if (btnConfirm != null) btnConfirm.onClick.AddListener(Confirmar);
        Loc.OnLanguageChanged += OnLangChanged;
        AtualizarDisplay();
    }

    void OnDestroy() => Loc.OnLanguageChanged -= OnLangChanged;

    void OnLangChanged(Language lang)
    {
        _previewIdx = (int)lang;
        AtualizarDisplay();
    }

    void Prev()
    {
        _previewIdx = (_previewIdx - 1 + LangCount) % LangCount;
        AtualizarDisplay();
    }

    void Next()
    {
        _previewIdx = (_previewIdx + 1) % LangCount;
        AtualizarDisplay();
    }

    void Confirmar()
    {
        Loc.Current = (Language)_previewIdx;
    }

    void AtualizarDisplay()
    {
        if (langNameText != null)
            langNameText.text = Loc.NativeName((Language)_previewIdx);
    }
}
