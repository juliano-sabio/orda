using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Co-op: a UI/lógica do boss roda só no host (EnemyNet desliga os scripts no cliente).
// Este NetworkBehaviour (que o EnemyNet preserva) sincroniza nome + fração de vida do boss
// e renderiza a barra do boss PRO CLIENTE (o host mantém a UI própria dele). A barra é
// estilo painel no topo (parecida com a do host), com fill por âncora (não depende de sprite).
public class BossHudNet : NetworkBehaviour
{
    readonly NetworkVariable<float> vidaFrac = new NetworkVariable<float>(
        1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<FixedString64Bytes> nomeBoss = new NetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    InimigoController ic;
    GameObject hud;
    RectTransform fillRT;   // fill por âncora: anchorMax.x = fração de vida
    TextMeshProUGUI nomeTxt;

    public override void OnNetworkSpawn()
    {
        ic = GetComponent<InimigoController>();

        if (IsServer)
        {
            nomeBoss.Value = LimparNome(gameObject.name);
        }
        else
        {
            CriarHud();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (hud != null) Destroy(hud);
    }

    void Update()
    {
        if (IsServer)
        {
            if (ic != null) vidaFrac.Value = ic.GetPorcentagemVida();
            return;
        }

        // cliente: reflete a vida sincronizada na barra
        if (fillRT != null)
        {
            float f = Mathf.Clamp01(vidaFrac.Value);
            fillRT.anchorMax = new Vector2(f, 1f);
        }
        if (nomeTxt != null)
        {
            string n = nomeBoss.Value.ToString();
            if (!string.IsNullOrEmpty(n) && nomeTxt.text != n) nomeTxt.text = n;
        }
    }

    static FixedString64Bytes LimparNome(string n)
    {
        if (string.IsNullOrEmpty(n)) return new FixedString64Bytes("BOSS");
        n = n.Replace("(Clone)", "").Trim();
        return new FixedString64Bytes(n.Length > 60 ? n.Substring(0, 60) : n);
    }

    void CriarHud()
    {
        hud = new GameObject("BossHudCoop");
        var cv = hud.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 60;
        var scaler = hud.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // painel
        var painel = NovoUI("Painel", hud.transform);
        Ancorar(painel, new Vector2(0.2f, 0.825f), new Vector2(0.8f, 0.935f));
        painel.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.03f, 0.04f, 0.9f);

        // nome
        var nomeGO = NovoUI("Nome", painel);
        Ancorar(nomeGO, new Vector2(0.01f, 0.55f), new Vector2(0.99f, 0.95f));
        nomeTxt = nomeGO.gameObject.AddComponent<TextMeshProUGUI>();
        nomeTxt.text = nomeBoss.Value.ToString();
        nomeTxt.fontSize = 20; nomeTxt.fontStyle = FontStyles.Bold;
        nomeTxt.alignment = TextAlignmentOptions.Center;
        nomeTxt.color = new Color(0.94f, 0.82f, 0.55f);

        // fundo da barra
        var barBG = NovoUI("BarBG", painel);
        Ancorar(barBG, new Vector2(0.02f, 0.10f), new Vector2(0.98f, 0.50f));
        barBG.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.05f, 0.05f, 1f);

        // fill (filho do BarBG): largura = fração de vida via anchorMax.x
        var fillGO = NovoUI("Fill", barBG);
        fillRT = fillGO;
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(Mathf.Clamp01(vidaFrac.Value), 1f);
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        fillGO.gameObject.AddComponent<Image>().color = new Color(0.78f, 0.13f, 0.13f, 1f);
    }

    static RectTransform NovoUI(string nome, Transform pai)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        return go.AddComponent<RectTransform>();
    }

    static void Ancorar(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
