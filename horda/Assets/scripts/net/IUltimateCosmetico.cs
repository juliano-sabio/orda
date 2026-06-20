// Co-op: ultimates implementam isto pra rodar SÓ o visual no cliente do colega (sem dano).
// O PlayerNet detecta o cast do dono (ultimateReady true→false) e chama ExecutarCosmetico()
// no componente do ultimate do fantoche (que existe lá via ApplyCharacterData).
public interface IUltimateCosmetico
{
    void ExecutarCosmetico();
}
