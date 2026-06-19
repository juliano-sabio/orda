// Implementada pelo componente de rede do player (PlayerNet), presente só na
// variante NetworkPlayer. Permite o PlayerStats (MonoBehaviour puro) consultar
// ownership sem depender de tipos do NGO.
public interface INetOwnership
{
    bool IsNetworked { get; }   // true quando há NetworkObject spawnado
    bool IsLocalOwner { get; }  // true quando esta instância é do jogador local
}
