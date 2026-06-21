using System.Collections;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Co-op: a UI/lógica do boss roda só no host (EnemyNet desliga os scripts no cliente).
// Este NetworkBehaviour (preservado pelo EnemyNet) sincroniza a vida do boss e, no cliente,
// reconstrói a UI PRÓPRIA do boss (via IBossHud) — idêntica à do host. Para bosses que ainda
// não implementam IBossHud, cai numa barra genérica (fallback).
public class BossHudNet : NetworkBehaviour
{
    readonly NetworkVariable<float> vidaAtualNet = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<float> vidaMaxNet = new NetworkVariable<float>(
        1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<FixedString64Bytes> nomeBoss = new NetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<int> faseUINet = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    InimigoController ic;
    IBossHud bossHud;

    // fallback genérico (bosses sem IBossHud)
    GameObject hud;
    RectTransform fillRT;
    TextMeshProUGUI nomeTxt;

    public override void OnNetworkSpawn()
    {
        ic = GetComponent<InimigoController>();
        bossHud = GetComponent<IBossHud>();

        if (IsServer)
        {
            nomeBoss.Value = LimparNome(gameObject.name);
        }
        else if (bossHud != null)
        {
            // cliente: monta a UI própria do boss (mesma do host). O boss é destruído pelo
            // NetworkObject no despawn → o OnDestroy dele limpa o canvas.
            bossHud.CriarBossUI();
        }
        else
        {
            CriarHudGenerico();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (hud != null) Destroy(hud);
    }

    // Co-op: o boss anuncia algo (ex.: "MODO FÚRIA!") via MostrarTextoTela no host. Isto
    // propaga o mesmo banner pros clientes (que não rodam o script do boss).
    public void BroadcastMensagem(string msg, Color cor, float duracao)
    {
        if (!IsServer || !IsSpawned || string.IsNullOrEmpty(msg)) return;
        var fs = new FixedString128Bytes(msg.Length > 120 ? msg.Substring(0, 120) : msg);
        MostrarMensagemClientRpc(fs, cor.r, cor.g, cor.b, duracao);
    }

    [Rpc(SendTo.NotServer)]
    void MostrarMensagemClientRpc(FixedString128Bytes msg, float r, float g, float b, float duracao)
    {
        StartCoroutine(Banner(msg.ToString(), new Color(r, g, b), duracao));
    }

    IEnumerator Banner(string msg, Color cor, float duracao)
    {
        var go = new GameObject("BossMsgCoop");
        var cv = go.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 200;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var tGO = new GameObject("T");
        tGO.transform.SetParent(go.transform, false);
        var rt = tGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.62f); rt.anchorMax = new Vector2(0.9f, 0.78f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var txt = tGO.AddComponent<TextMeshProUGUI>();
        txt.text = msg; txt.fontSize = 46; txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;

        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime) { txt.color = new Color(cor.r, cor.g, cor.b, t / 0.3f); yield return null; }
        txt.color = cor;
        yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, duracao));
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime) { txt.color = new Color(cor.r, cor.g, cor.b, 1f - t / 0.4f); yield return null; }
        Destroy(go);
    }

    void Update()
    {
        if (IsServer)
        {
            if (ic != null)
            {
                vidaAtualNet.Value = ic.vidaAtual;
                vidaMaxNet.Value   = ic.vidaMaxima;
            }
            if (bossHud != null) faseUINet.Value = bossHud.FaseUI;
            return;
        }

        // cliente
        if (bossHud != null && ic != null)
        {
            // alimenta o controller (desligado no cliente) com a vida sincronizada,
            // pra a UI própria do boss renderizar idêntica à do host.
            ic.vidaAtual  = vidaAtualNet.Value;
            ic.vidaMaxima = vidaMaxNet.Value;
            bossHud.FaseUI = faseUINet.Value;   // reflete a fase sincronizada (cor/texto)
            bossHud.AtualizarBarraUI();
        }
        else if (fillRT != null)
        {
            float max = Mathf.Max(1f, vidaMaxNet.Value);
            fillRT.anchorMax = new Vector2(Mathf.Clamp01(vidaAtualNet.Value / max), 1f);
            if (nomeTxt != null)
            {
                string n = nomeBoss.Value.ToString();
                if (!string.IsNullOrEmpty(n) && nomeTxt.text != n) nomeTxt.text = n;
            }
        }
    }

    static FixedString64Bytes LimparNome(string n)
    {
        if (string.IsNullOrEmpty(n)) return new FixedString64Bytes("BOSS");
        n = n.Replace("(Clone)", "").Trim();
        return new FixedString64Bytes(n.Length > 60 ? n.Substring(0, 60) : n);
    }

    // ── Fallback genérico (bosses sem IBossHud) ──────────────────────────
    void CriarHudGenerico()
    {
        hud = new GameObject("BossHudCoop");
        var cv = hud.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 60;
        var scaler = hud.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var painel = NovoUI("Painel", hud.transform);
        Ancorar(painel, new Vector2(0.2f, 0.825f), new Vector2(0.8f, 0.935f));
        painel.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.03f, 0.04f, 0.9f);

        var nomeGO = NovoUI("Nome", painel);
        Ancorar(nomeGO, new Vector2(0.01f, 0.55f), new Vector2(0.99f, 0.95f));
        nomeTxt = nomeGO.gameObject.AddComponent<TextMeshProUGUI>();
        nomeTxt.text = nomeBoss.Value.ToString();
        nomeTxt.fontSize = 20; nomeTxt.fontStyle = FontStyles.Bold;
        nomeTxt.alignment = TextAlignmentOptions.Center;
        nomeTxt.color = new Color(0.94f, 0.82f, 0.55f);

        var barBG = NovoUI("BarBG", painel);
        Ancorar(barBG, new Vector2(0.02f, 0.10f), new Vector2(0.98f, 0.50f));
        barBG.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.05f, 0.05f, 1f);

        var fillGO = NovoUI("Fill", barBG);
        fillRT = fillGO;
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(1f, 1f);
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
