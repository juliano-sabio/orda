using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EscolherTerrenoMenu : MonoBehaviour
{
    [Header("Nomes das Cenas")]
    public string cenaArea1 = "primeira_fase";
    public string cenaArea2 = "segunda_fase";
    public string cenaArea3 = "terceira_fase";
    public string cenaSobrevivencia = "Modo_sobrevivencia";

    [Header("Botões")]
    public Button botaoArea1;
    public Button botaoArea2;
    public Button botaoArea3;
    public Button botaoSobrevivencia;
    public Button botaoVoltar;

    void Start()
    {
        if (botaoArea1) botaoArea1.onClick.AddListener(() => IrPara(cenaArea1));
        if (botaoArea2) botaoArea2.onClick.AddListener(() => IrPara(cenaArea2));
        if (botaoArea3) botaoArea3.onClick.AddListener(() => IrPara(cenaArea3));
        if (botaoSobrevivencia) botaoSobrevivencia.onClick.AddListener(() => IrPara(cenaSobrevivencia));
        if (botaoVoltar) botaoVoltar.onClick.AddListener(Voltar);
    }

    void IrPara(string nomeCena)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nomeCena);
    }

    void Voltar()
    {
        SceneManager.LoadScene("CharacterSelection");
    }
}
