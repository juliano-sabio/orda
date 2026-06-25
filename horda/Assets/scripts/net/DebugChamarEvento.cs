using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// [debug temp] Botão IMGUI pra forçar QUALQUER evento da fase no co-op (host dispara,
// o cliente recebe via CoopProgressao). Lista só os tipos presentes na lista 'eventos'
// da fase atual. NÃO é produção — remover junto com o resto do debug.
public class DebugChamarEvento : MonoBehaviour
{
    void OnGUI()
    {
        var ge = GerenciadorEventos.Instance;
        if (ge == null) return;

        var nm = NetworkManager.Singleton;
        bool host = nm == null || !nm.IsListening || nm.IsServer;

        var tipos = ge.TiposDisponiveis();
        int linhas = Mathf.Max(1, tipos.Count);

        float w = 190f, x = 10f, y = 120f;
        GUI.Box(new Rect(x, y, w, 64f + linhas * 28f), "[debug] EVENTOS");
        y += 30f;

        if (!host)
        {
            GUI.Label(new Rect(x + 8f, y, w - 16f, 24f), "só o host dispara");
            return;
        }
        if (tipos.Count == 0)
        {
            GUI.Label(new Rect(x + 8f, y, w - 16f, 24f), "sem eventos na fase");
            return;
        }

        foreach (var tipo in tipos)
        {
            if (GUI.Button(new Rect(x + 6f, y, w - 12f, 24f), tipo.ToString()))
                ge.ForcarEvento(tipo);
            y += 28f;
        }
    }
}
