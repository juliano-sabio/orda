using System.Collections.Generic;
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

    // Objetos de evento procedurais (host registra via RPC, cliente reconstrói cópia cosmética).
    readonly Dictionary<int, GameObject> cosmeticos = new Dictionary<int, GameObject>(); // só no cliente
    int proxObjId = 1; // host

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
        foreach (var go in cosmeticos.Values) if (go != null) Destroy(go);
        cosmeticos.Clear();
        if (Instance == this) Instance = null;
    }

    // ── Objetos de evento procedurais ─────────────────────────────────────────

    // Host: registra um objeto de evento; retorna o id (0 fora de rede). O componente
    // chama RemoverObjEvento(id) no OnDestroy. Avisa os clientes via RPC.
    public int RegistrarObjEvento(byte tipo, Vector2 pos, float p1, float p2)
        => RegistrarObjEvento(tipo, pos, p1, p2, Color.white);

    public int RegistrarObjEvento(byte tipo, Vector2 pos, float p1, float p2, Color cor)
    {
        Debug.Log($"[CoopObj-HOST] RegistrarObjEvento tipo={tipo} pos={pos} IsServer={IsServer} IsSpawned={IsSpawned}"); // [debug temp]
        if (!IsServer) return 0;
        int id = proxObjId++;
        ConstruirCosmeticoRpc(tipo, id, pos, p1, p2, cor);
        return id;
    }

    public void RemoverObjEvento(int id)
    {
        if (!IsServer || id == 0) return;
        RemoverCosmeticoRpc(id);
    }

    // Co-op: ações do player REMOTO (P2) roteadas pro host contarem no evento ativo.
    [Rpc(SendTo.Server)]
    public void RegistrarXPEventoServerRpc(float qtd) { GerenciadorEventos.Instance?.XPColetadoCoop(qtd); }

    [Rpc(SendTo.Server)]
    public void RegistrarUltimateEventoServerRpc() { GerenciadorEventos.Instance?.UltimateCoop(); }

    [Rpc(SendTo.Server)]
    public void RegistrarDanoEventoServerRpc() { GerenciadorEventos.Instance?.DanoRecebidoCoop(); }

    // Co-op: host replica o card de RESULTADO do evento (sucesso/falha) no cliente.
    public void BroadcastResultadoEvento(bool sucesso, int tipo) { if (IsServer) MostrarResultadoEventoRpc(sucesso, tipo); }

    [Rpc(SendTo.NotServer)]
    void MostrarResultadoEventoRpc(bool sucesso, int tipo) { GerenciadorEventos.Instance?.AplicarResultadoCoop(sucesso, tipo); }

    // Só nos clientes (não no host, que tem o objeto real).
    [Rpc(SendTo.NotServer)]
    void ConstruirCosmeticoRpc(byte tipo, int id, Vector2 pos, float p1, float p2, Color cor)
    {
        Debug.Log($"[CoopObj-CLIENT] ConstruirCosmeticoRpc tipo={tipo} id={id} pos={pos}"); // [debug temp]
        if (cosmeticos.ContainsKey(id)) return;
        var go = new GameObject("EvtCosmetico_" + tipo);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        switch (tipo)
        {
            case 1: // Zona de eliminação
            {
                var z = go.AddComponent<ZonaEliminacaoEvento>();
                z.raio = p1;
                z.cosmetico = true;
                break;
            }
            case 2: // Colapso (cliente mira o próprio player local)
            {
                var cl = go.AddComponent<EventoColapso>();
                cl.IniciarCosmetico(pos, p1, p2);
                break;
            }
            case 3: // Portal (cada portal é um objeto cosmético próprio, com indicador)
            {
                var pe = go.AddComponent<PortalEvento>();
                pe.IniciarCosmeticoUmPortal(pos, cor);
                break;
            }
            case 4: // Tempestade elétrica (ambiente, por-player; p1=duração)
            {
                var te = go.AddComponent<TempestadeEletricaEvento>();
                te.IniciarCosmetico(p1);
                break;
            }
            case 5: // Núcleo corrompido
            {
                var n = go.AddComponent<NucleoCorrompidoEvento>();
                n.cosmetico = true;
                break;
            }
            case 6: // Vórtice devorador
            {
                var v = go.AddComponent<VorticeDevoradorEvento>();
                v.raioAtracao = p1;
                v.raioDevorar = p2;
                v.corVortice  = cor;
                v.cosmetico   = true;
                break;
            }
            case 7: // Cristal de energia
            {
                var c = go.AddComponent<CristalEvento>();
                c.corBase = cor;
                c.cosmetico = true;
                break;
            }
            case 8: // Borda de sangue (overlay de tela do Ceifador)
            {
                go.AddComponent<BordaSangueEvento>().Mostrar();
                break;
            }
            default:
                Destroy(go);
                return;
        }

        cosmeticos[id] = go;
    }

    [Rpc(SendTo.NotServer)]
    void RemoverCosmeticoRpc(int id)
    {
        if (cosmeticos.TryGetValue(id, out var go))
        {
            if (go != null) Destroy(go);
            cosmeticos.Remove(id);
        }
    }

    // Co-op: sincroniza o estado do evento. Host publica o que o GerenciadorEventos calcula;
    // cliente (que não roda a lógica de evento) reconstrói o painel a partir disso.
    bool geNullLogado, evtLogCli; // [debug temp]

    void Update()
    {
        var ge = GerenciadorEventos.Instance;
        if (ge == null)
        {
            if (!geNullLogado) { geNullLogado = true; Debug.Log($"[CoopEvt] GerenciadorEventos.Instance NULL (IsServer={IsServer})"); }
            return;
        }

        if (IsServer)
        {
            bool a = ge.EvtAtivoCoop;
            if (evtAtivo.Value != a) { evtAtivo.Value = a; Debug.Log($"[CoopEvt-HOST] evtAtivo={a} tipo={ge.EvtTipoCoop}"); }
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
            if (evtAtivo.Value != evtLogCli) { evtLogCli = evtAtivo.Value; Debug.Log($"[CoopEvt-CLIENT] recebeu ativo={evtAtivo.Value} tipo={evtTipo.Value} (vai montar painel)"); }
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
