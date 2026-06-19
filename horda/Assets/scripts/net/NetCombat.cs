// Liga/desliga dano no contexto de rede. No SP2a fica desligado em rede (sem dano);
// o SP2b liga (RedeComDano = true). Em single-player o dano é sempre habilitado.
public static class NetCombat
{
    public static bool RedeComDano = false;

    public static bool DanoHabilitado => !NetSpawn.EmRede || RedeComDano;
}
