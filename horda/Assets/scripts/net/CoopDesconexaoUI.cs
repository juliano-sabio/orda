using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Co-op: quando o CLIENTE perde a conexão sem querer (host caiu, Relay caiu, timeout),
// mostra uma mensagem por alguns segundos e SÓ ENTÃO manda pro menu inicial — antes o
// cliente ficava travado na partida congelada. Saídas voluntárias (botão Sair) setam
// SaidaIntencional pra não disparar a mensagem (vão direto pro menu, sem alarde).
//
// Auto-anexa (não precisa estar em cena). Só age no cliente puro (o host não é chutado
// pra lugar nenhum quando um parceiro cai).
public class CoopDesconexaoUI : MonoBehaviour
{
    const string CENA_MENU = "menu_inicial";

    // LobbyUI/menu setam isto antes de um Shutdown proposital pra suprimir a mensagem.
    public static bool SaidaIntencional;

    static CoopDesconexaoUI _i;
    NetworkManager _nmInscrito;
    bool _souClienteCoop;   // virei cliente (não-host) numa sessão de rede
    bool _tratando;         // já estou mostrando a tela de queda
    string _msg;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Auto()
    {
        if (_i != null) return;
        var go = new GameObject("CoopDesconexaoUI");
        DontDestroyOnLoad(go);
        _i = go.AddComponent<CoopDesconexaoUI>();
    }

    void Update()
    {
        var nm = NetworkManager.Singleton;
        if (nm == _nmInscrito) return;
        Desinscrever();
        if (nm != null) Inscrever(nm);
    }

    void OnDestroy() => Desinscrever();

    void Inscrever(NetworkManager nm)
    {
        _nmInscrito = nm;
        nm.OnClientConnectedCallback  += AoConectar;
        nm.OnClientDisconnectCallback += AoDesconectar;
        nm.OnTransportFailure         += AoFalhaTransporte;
    }

    void Desinscrever()
    {
        if (_nmInscrito == null) return;
        _nmInscrito.OnClientConnectedCallback  -= AoConectar;
        _nmInscrito.OnClientDisconnectCallback -= AoDesconectar;
        _nmInscrito.OnTransportFailure         -= AoFalhaTransporte;
        _nmInscrito = null;
    }

    void AoConectar(ulong id)
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsClient && !nm.IsServer && id == nm.LocalClientId)
        {
            _souClienteCoop = true;
            SaidaIntencional = false; // sessão nova começa limpa (não herda flag antigo)
        }
    }

    void AoDesconectar(ulong id)
    {
        var nm = NetworkManager.Singleton;
        // Só nos importa a queda do PRÓPRIO cliente (host não é redirecionado).
        if (!_souClienteCoop) return;
        if (nm != null && nm.IsServer) return;
        Tratar();
    }

    void AoFalhaTransporte()
    {
        if (_souClienteCoop) Tratar();
    }

    void Tratar()
    {
        if (_tratando) return;
        _souClienteCoop = false;

        // Saída voluntária (o jogador clicou em Sair): vai pro menu sem a mensagem de queda.
        if (SaidaIntencional)
        {
            SaidaIntencional = false;
            IrParaMenu();
            return;
        }

        _tratando = true;
        _msg = Loc.Current == Language.PT_BR
            ? "Conexão perdida.\nO host encerrou a partida ou a conexão caiu.\n\nVoltando ao menu..."
            : "Connection lost.\nThe host ended the match or the connection dropped.\n\nReturning to menu...";
        StartCoroutine(FecharDepois());
    }

    IEnumerator FecharDepois()
    {
        yield return new WaitForSecondsRealtime(3.5f);
        IrParaMenu();
    }

    void IrParaMenu()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening) nm.Shutdown();
        LobbyState.EmLobby = false;
        Time.timeScale = 1f; // garante que não vá pro menu com o jogo pausado
        _tratando = false;
        _msg = null;
        if (SceneManager.GetActiveScene().name != CENA_MENU)
            SceneManager.LoadScene(CENA_MENU);
    }

    void OnGUI()
    {
        if (!_tratando || string.IsNullOrEmpty(_msg)) return;

        // Cortina escura sobre a tela toda
        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var estilo = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = Mathf.Max(16, Screen.height / 28),
            wordWrap  = true,
        };
        estilo.normal.textColor = new Color(0.94f, 0.88f, 0.75f); // creme (paleta do jogo)

        float w = Mathf.Min(Screen.width * 0.8f, 720f);
        var r = new Rect((Screen.width - w) / 2f, Screen.height * 0.35f, w, Screen.height * 0.3f);
        GUI.Label(r, _msg, estilo);

        // Botão pra pular a espera
        string txtBtn = Loc.Current == Language.PT_BR ? "Ir ao menu" : "Go to menu";
        float bw = 220f, bh = 44f;
        if (GUI.Button(new Rect((Screen.width - bw) / 2f, r.yMax + 10f, bw, bh), txtBtn))
            IrParaMenu();
    }
}
