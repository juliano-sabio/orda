// Marcador para identificar bosses de forma robusta (independente do script específico).
// Todos os scripts de boss (BossController, BossCaveira, BossPrincesa, BossGuarda,
// BossSlimeGuardaElite) implementam esta interface vazia.
// Usado por InimigoController.EhBoss() (pausa do countdown e detecção do boss final).
public interface IBoss { }
