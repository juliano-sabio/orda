// Co-op: defensivas que disparam em evento do player (dano/HP baixo/morte) implementam
// isto pra o fantoche poder reproduzir só o visual quando o DONO ativa de verdade.
// (Análogo ao IUltimateCosmetico, mas pra skills defensivas com gatilho por evento.)
public interface IDefensivaCosmetico
{
    void ExecutarCosmetico();
}
