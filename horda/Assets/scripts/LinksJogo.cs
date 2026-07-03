using UnityEngine;

// Links externos do jogo (funil da demo: wishlist na Steam, Discord, etc.).
// PREENCHA as URLs abaixo quando as páginas existirem — enquanto estiverem vazias,
// os botões correspondentes simplesmente não aparecem (nada quebra).
public static class LinksJogo
{
    // ── PREENCHER ─────────────────────────────────────────────────────────────
    // Ex.: "https://store.steampowered.com/app/SEU_APPID/"
    public const string SteamWishlist = "";
    // Ex.: "https://discord.gg/SEU_CONVITE"
    public const string Discord       = "";
    // ──────────────────────────────────────────────────────────────────────────

    public static bool TemWishlist => !string.IsNullOrEmpty(SteamWishlist);
    public static bool TemDiscord  => !string.IsNullOrEmpty(Discord);

    public static void Abrir(string url)
    {
        if (!string.IsNullOrEmpty(url)) Application.OpenURL(url);
    }
}
