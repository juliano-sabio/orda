// Estado global: true enquanto estamos na cena de lobby (gameplay congelado).
// Setado true pela LobbyCoopUI no lobby; false quando a fase carrega.
public static class LobbyState
{
    public static bool EmLobby = false;
}
