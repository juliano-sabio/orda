using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Co-op: a UI/lógica do boss roda só no host (EnemyNet desliga os scripts no cliente).
// Este NetworkBehaviour (que o EnemyNet preserva) sincroniza nome + fração de vida do
// boss e mostra uma barra no topo da tela PRO CLIENTE (o host mantém a UI própria dele).
public class BossHudNet : NetworkBehaviour
{
    readonly NetworkVariable<float> vidaFrac = new NetworkVariable<float>(
        1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<FixedString64Bytes> nomeBoss = new NetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    InimigoController ic;
    GameObject hud;
    Image fill;

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
            vidaFrac.OnValueChanged += (_, v) => { if (fill != null) fill.fillAmount = Mathf.Clamp01(v); };
        }
    }

    public override void OnNetworkDespawn()
    {
        if (hud != null) Destroy(hud);
    }

    void Update()
    {
        // host: empurra a vida do boss pra NetVar (a UI própria dele continua na lógica do boss).
        if (IsServer && ic != null)
            vidaFrac.Value = ic.GetPorcentagemVida();
    }

    static FixedString64Bytes LimparNome(string n)
    {
        if (string.IsNullOrEmpty(n)) return new FixedString64Bytes("BOSS");
        n = n.Replace("(Clone)", "").Trim();
        return new FixedString64Bytes(n.Length > 60 ? n.Substring(0, 60) : n);
    }

    // Barra de boss simples no topo da tela (só no cliente; o host tem a UI própria).
    void CriarHud()
    {
        hud = new GameObject("BossHudCoop");
        var cv = hud.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 90;
        var scaler = hud.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // moldura
        var bgGO = new GameObject("BG");
        bgGO.transform.SetParent(hud.transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.25f, 0.90f); bgRT.anchorMax = new Vector2(0.75f, 0.935f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        bgGO.AddComponent<Image>().color = new Color(0.08f, 0.02f, 0.02f, 0.85f);

        // preenchimento (vida)
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(bgGO.transform, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0f, 0f); fillRT.anchorMax = new Vector2(1f, 1f);
        fillRT.offsetMin = new Vector2(2f, 2f); fillRT.offsetMax = new Vector2(-2f, -2f);
        fill = fillGO.AddComponent<Image>();
        fill.color = new Color(0.72f, 0.12f, 0.12f, 0.95f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = Mathf.Clamp01(vidaFrac.Value);

        // nome
        var txtGO = new GameObject("Nome");
        txtGO.transform.SetParent(bgGO.transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = new Vector2(0f, 1f); txtRT.anchorMax = new Vector2(1f, 1f);
        txtRT.offsetMin = new Vector2(0f, 2f); txtRT.offsetMax = new Vector2(0f, 28f);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text = nomeBoss.Value.ToString();
        txt.fontSize = 20; txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(0.94f, 0.88f, 0.75f);
    }
}
