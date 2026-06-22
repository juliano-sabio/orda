using Unity.Netcode;
using UnityEngine;

// Progressão co-op host-autoritativa: pool de XP + nível COMPARTILHADOS. Os orbes de XP
// (coletados no host) somam aqui; ao cruzar o limiar, o GRUPO sobe de nível e cada player
// aplica o level-up no próprio cliente (escolha de skill/carta individual). A barra de XP/
// nível local (UI de hoje) é espelhada do pool compartilhado.
public class CoopProgressao : NetworkBehaviour
{
    public static CoopProgressao Instance { get; private set; }

    public readonly NetworkVariable<int> nivel = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<float> xpAtual = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<float> xpProxNivel = new NetworkVariable<float>(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Co-op: estado do evento atual (host-autoritativo) → o cliente reconstrói a UI de evento.
    public readonly NetworkVariable<bool>  evtAtivo   = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int>   evtTipo    = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int>   evtProg    = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int>   evtQtd     = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<float> evtTimer   = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<float> evtDuracao = new NetworkVariable<float>(1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        Instance = this;
        xpAtual.OnValueChanged     += AoMudarXP;
        nivel.OnValueChanged       += AoMudarNivel;
        xpProxNivel.OnValueChanged += AoMudarProx;
        EspelharUI();
    }

    public override void OnNetworkDespawn()
    {
        xpAtual.OnValueChanged     -= AoMudarXP;
        nivel.OnValueChanged       -= AoMudarNivel;
        xpProxNivel.OnValueChanged -= AoMudarProx;
        if (Instance == this) Instance = null;
    }

    // Co-op: sincroniza o estado do evento. Host publica o que o GerenciadorEventos calcula;
    // cliente (que não roda a lógica de evento) reconstrói o painel a partir disso.
    void Update()
    {
        var ge = GerenciadorEventos.Instance;
        if (ge == null) return;

        if (IsServer)
        {
            bool a = ge.EvtAtivoCoop;
            if (evtAtivo.Value != a) evtAtivo.Value = a;
            if (a)
            {
                if (evtTipo.Value != ge.EvtTipoCoop) evtTipo.Value = ge.EvtTipoCoop;
                if (evtProg.Value != ge.EvtProgCoop) evtProg.Value = ge.EvtProgCoop;
                if (evtQtd.Value  != ge.EvtQtdCoop)  evtQtd.Value  = ge.EvtQtdCoop;
                if (Mathf.Abs(evtTimer.Value   - ge.EvtTimerCoop)   > 0.1f)  evtTimer.Value   = ge.EvtTimerCoop;
                if (Mathf.Abs(evtDuracao.Value - ge.EvtDuracaoCoop) > 0.01f) evtDuracao.Value = ge.EvtDuracaoCoop;
            }
        }
        else
        {
            ge.AplicarEstadoCoop(evtAtivo.Value, evtTipo.Value, evtProg.Value, evtQtd.Value, evtTimer.Value, evtDuracao.Value);
        }
    }

    void AoMudarXP(float _, float __)   => EspelharUI();
    void AoMudarNivel(int _, int __)    => EspelharUI();
    void AoMudarProx(float _, float __) => EspelharUI();

    float MultXP()
    {
        var l = PlayerStats.Local;
        return l != null ? l.xpMultiplier : 1.35f;
    }

    // Host: soma XP ao pool; sobe o nível do grupo quantas vezes for preciso.
    public void AdicionarXP(float xp)
    {
        if (!IsServer || xp <= 0f) return;
        xpAtual.Value += xp;
        while (xpAtual.Value >= xpProxNivel.Value)
        {
            xpAtual.Value -= xpProxNivel.Value;
            nivel.Value++;
            xpProxNivel.Value = 100f * Mathf.Pow(MultXP(), nivel.Value - 1);
            // cada player aplica o level-up no SEU cliente (escolha individual).
            for (int i = 0; i < PlayerStats.All.Count; i++)
            {
                var pn = PlayerStats.All[i] != null ? PlayerStats.All[i].GetComponent<PlayerNet>() : null;
                if (pn != null) pn.SubirNivelOwnerRpc(nivel.Value);
            }
        }
    }

    // Mantém a barra de XP/nível local refletindo o pool compartilhado.
    void EspelharUI()
    {
        var l = PlayerStats.Local;
        if (l != null) l.EspelharProgressoCoop(nivel.Value, xpAtual.Value, xpProxNivel.Value);
    }
}
