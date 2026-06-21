// Co-op: bosses implementam isto pra o BossHudNet (que roda no cliente, preservado pelo
// EnemyNet) reconstruir a UI PRÓPRIA do boss e atualizá-la com a vida sincronizada — UI
// idêntica à do host, sem duplicar o visual. O host continua criando/atualizando a sua
// (no Start/Update do boss); o BossHudNet só dirige a cópia do cliente.
public interface IBossHud
{
    void CriarBossUI();      // monta o canvas/barra/nome/fase custom do boss
    void AtualizarBarraUI(); // atualiza fill/HP/fase a partir do controller (vida sincronizada no cliente)
}
