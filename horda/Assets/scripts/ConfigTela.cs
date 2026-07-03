using UnityEngine;

// Centraliza a troca de fullscreen ↔ janela pra ficar consistente em todos os lugares
// (menu, pausa, game over). Ao sair do fullscreen, usa uma resolução de janela decente
// (1280×720) em vez de deixar a janela do tamanho do desktop (parecia borderless).
public static class ConfigTela
{
    const int JANELA_W = 1280;
    const int JANELA_H = 720;

    public static void AplicarFullscreen(bool fs, bool salvar = true)
    {
        if (fs)
        {
            // Borderless na resolução nativa — mais compatível que exclusivo.
            int w = Display.main.systemWidth;
            int h = Display.main.systemHeight;
            Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(JANELA_W, JANELA_H, FullScreenMode.Windowed);
        }
        if (salvar) PlayerPrefs.SetInt("Fullscreen", fs ? 1 : 0);
    }
}
