using Unity.Netcode;
using UnityEngine;

// UI do lobby co-op (IMGUI funcional). Conecta via Relay, mostra roster dos
// players spawnados, seleção de personagem, fase (host) e ready. Repolimento
// visual com UIDark/uGUI é um passo cosmético posterior.
public class LobbyCoopUI : MonoBehaviour
{
    public CharacterData[] characters;

    bool souHost;
    string joinCode = "";
    string status = "";
    int charSel;

    void Start()
    {
        // Co-op: registra a lista de personagens pra a fase de rede resolver o CharacterData pelo índice.
        if (characters != null && characters.Length > 0) PlayerStats.RegistroPersonagens = characters;

        LobbyState.EmLobby = true;
        souHost = PlayerPrefs.GetInt("LobbyHost", 1) == 1;
        charSel = PlayerPrefs.GetInt("SelectedCharacter", 0);
        Conectar();
    }

    async void Conectar()
    {
        status = "Conectando...";
        try
        {
            await NetBootstrap.InitAsync();
            if (souHost)
            {
                joinCode = await RelayConnector.HostAsync(4);
                status = "Sala criada. Código: " + joinCode;
            }
            else
            {
                joinCode = PlayerPrefs.GetString("LobbyCode", "");
                status = "Digite o código e clique Entrar.";
            }
        }
        catch (System.Exception e) { status = "Falha: " + e.Message; }
    }

    async void Entrar()
    {
        try { await RelayConnector.JoinAsync(joinCode.Trim()); status = "Conectado."; }
        catch (System.Exception e) { status = "Falha ao entrar: " + e.Message; }
    }

    PlayerNet Meu()
    {
        var l = PlayerStats.Local;
        return l != null ? l.GetComponent<PlayerNet>() : null;
    }

    void OnGUI()
    {
        var nm = NetworkManager.Singleton;
        GUI.Box(new Rect(10, 10, 460, 620), "LOBBY CO-OP");
        float y = 40;
        GUI.Label(new Rect(20, y, 440, 24), status); y += 28;

        // cliente: campo de código + entrar (antes de conectar)
        if (!souHost && nm != null && !nm.IsConnectedClient && !nm.IsListening)
        {
            GUI.Label(new Rect(20, y, 70, 24), "Código:");
            joinCode = GUI.TextField(new Rect(90, y, 200, 24), joinCode);
            if (GUI.Button(new Rect(300, y - 2, 120, 28), "Entrar")) Entrar();
            y += 34;
        }
        if (souHost && !string.IsNullOrEmpty(joinCode))
        {
            if (GUI.Button(new Rect(20, y, 240, 26), "Copiar código: " + joinCode))
                GUIUtility.systemCopyBuffer = joinCode;
            y += 34;
        }

        // roster
        GUI.Label(new Rect(20, y, 440, 22), "JOGADORES:"); y += 26;
        for (int i = 0; i < PlayerStats.All.Count; i++)
        {
            var pn = PlayerStats.All[i] != null ? PlayerStats.All[i].GetComponent<PlayerNet>() : null;
            if (pn == null) continue;
            string nome = pn.Nome;
            string perso = (characters != null && pn.CharIndexLobby >= 0 && pn.CharIndexLobby < characters.Length && characters[pn.CharIndexLobby] != null)
                ? characters[pn.CharIndexLobby].characterName : ("#" + pn.CharIndexLobby);
            GUI.Label(new Rect(30, y, 440, 22), (pn.Pronto ? "[PRONTO] " : "[ ] ") + nome + " - " + perso);
            y += 24;
        }
        y += 10;

        // seleção de personagem
        GUI.Label(new Rect(20, y, 440, 22), "PERSONAGEM:"); y += 26;
        if (characters != null)
        {
            for (int i = 0; i < characters.Length; i++)
            {
                bool sel = i == charSel;
                if (GUI.Button(new Rect(30 + (i % 4) * 105, y + (i / 4) * 30, 100, 26),
                    (sel ? "> " : "") + (characters[i] != null ? characters[i].characterName : "?")))
                {
                    charSel = i;
                    PlayerPrefs.SetInt("SelectedCharacter", i);
                    var m = Meu(); if (m != null) m.SetChar(i);
                }
            }
            y += ((characters.Length + 3) / 4) * 30 + 10;
        }

        // host: fase + iniciar
        if (souHost && LobbyManager.Instance != null)
        {
            GUI.Label(new Rect(20, y, 440, 22), "FASE:"); y += 26;
            for (int i = 0; i < LobbyManager.Fases.Length; i++)
            {
                bool sel = LobbyManager.Instance.faseEscolhida.Value == i;
                if (GUI.Button(new Rect(30 + i * 160, y, 150, 26), (sel ? "> " : "") + LobbyManager.Fases[i]))
                    LobbyManager.Instance.EscolherFaseServerRpc(i);
            }
            y += 36;
            GUI.enabled = LobbyManager.Instance.TodosProntos();
            if (GUI.Button(new Rect(20, y, 200, 32), "INICIAR")) LobbyManager.Instance.IniciarServerRpc();
            GUI.enabled = true;
        }

        // ready (todos)
        var meu = Meu();
        if (meu != null)
        {
            if (GUI.Button(new Rect(240, y, 160, 32), meu.Pronto ? "CANCELAR PRONTO" : "PRONTO"))
                meu.SetPronto(!meu.Pronto);
        }
        y += 40;

        if (GUI.Button(new Rect(20, y, 120, 28), "Sair"))
        {
            if (nm != null) nm.Shutdown();
            LobbyState.EmLobby = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene("menu_inicial");
        }
    }
}
