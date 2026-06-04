#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class GerenciadorEventosAutoSetup
{
    static GerenciadorEventosAutoSetup()
    {
        EditorApplication.delayCall          += AdicionarEventos;
        EditorSceneManager.sceneOpened       += (s, m) => AdicionarEventos();
        EditorSceneManager.sceneSaved        += (s)    => AdicionarEventos();
    }

    static void AdicionarEventos()
    {
        var managers = Object.FindObjectsByType<GerenciadorEventos>(FindObjectsSortMode.None);
        foreach (var ge in managers)
        {
            if (ge == null || ge.eventos == null) continue;
            bool alterou = false;

            alterou |= Adicionar(ge, new EventoAleatorio
            {
                nome = "Portal",
                descricao = "Dois portais abriram no mapa! Chegue perto de cada um para fechá-los antes que inimigos invadam!",
                tipo = TipoEvento.Portal,
                duracao = 300f, quantidade = 2,
                recompensaDescricao = "+15% de vida recuperada!"
            }, primeiro: true);

            alterou |= Adicionar(ge, new EventoAleatorio
            {
                nome = "⚡ Tempestade Elétrica",
                descricao = "Raios caem pelo mapa! Fique de olho nos círculos de aviso e sobreviva!",
                tipo = TipoEvento.TempestadeEletrica,
                duracao = 300f, quantidade = 0,
                recompensaDescricao = "+15% de vida recuperada!"
            });

            alterou |= Adicionar(ge, new EventoAleatorio
            {
                nome = "Eliminar Slime Colorida",
                descricao = "Encontre e elimine a slime colorida!",
                tipo = TipoEvento.EliminarSlimeColorida,
                duracao = 300f, quantidade = 1,
                recompensaDescricao = "+15% de vida recuperada!"
            });

            alterou |= Adicionar(ge, new EventoAleatorio
            {
                nome = "Ceifador",
                descricao = "Sobreviva ao ataque dos ceifadores!",
                tipo = TipoEvento.Ceifador,
                duracao = 300f, quantidade = 6,
                recompensaDescricao = "+15% de vida recuperada!"
            });

            alterou |= Adicionar(ge, new EventoAleatorio
            {
                nome = "Slime Percurso",
                descricao = "Impeça a slime de atravessar o mapa!",
                tipo = TipoEvento.SlimePercurso,
                duracao = 300f, quantidade = 0,
                recompensaDescricao = "+40% de vida recuperada!"
            });

            alterou |= Adicionar(ge, new EventoAleatorio
            {
                nome = "Zona de Eliminação",
                descricao = "Elimine inimigos dentro da zona marcada!",
                tipo = TipoEvento.ZonaEliminacao,
                duracao = 300f, quantidade = 10,
                recompensaDescricao = "+15% de vida recuperada!"
            });

            alterou |= Adicionar(ge, new EventoAleatorio
            {
                nome = "Colapso",
                descricao = "A zona está fechando! Sobreviva e elimine inimigos!",
                tipo = TipoEvento.Colapso,
                duracao = 300f, quantidade = 15,
                recompensaDescricao = "+15% de vida recuperada!"
            });


            // Garante 5 minutos em todos os eventos
            foreach (var e in ge.eventos)
            {
                if (e.duracao != 300f) { e.duracao = 300f; alterou = true; }
            }

            if (alterou) EditorUtility.SetDirty(ge);
        }
    }

    static bool Adicionar(GerenciadorEventos ge, EventoAleatorio evento, bool primeiro = false)
    {
        if (ge.eventos.Exists(e => e.tipo == evento.tipo)) return false;
        if (primeiro) ge.eventos.Insert(0, evento);
        else ge.eventos.Add(evento);
        return true;
    }
}
#endif
