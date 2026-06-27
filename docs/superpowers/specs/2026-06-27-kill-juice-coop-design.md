# Kill Juice (efeito de morte de inimigo, co-op) — Design

**Data:** 2026-06-27
**Objetivo:** Toda morte de inimigo NORMAL ganha um "pop" satisfatório (squash + estilhaços + anel + flash), replicado em co-op, pra elevar o game-feel e o apelo de clipe (wishlist). Hoje o inimigo normal só **some** (`NetSpawn.Despawnar`) — zero juice.

**Motivação (wishlist):** clipe curto vertical é o que mais converte no gênero survivor; com a tela cheia de inimigos morrendo, um micro-pop por morte vira o próprio power-fantasy visual. Bosses já têm efeito de morte próprio; o gap é o inimigo comum.

---

## Escopo

**Dentro:** efeito de morte de inimigos NORMAIS (o ramo `else NetSpawn.Despawnar` de `InimigoController.Morrer`), em SP e co-op.

**Fora:**
- Bosses (mantêm `IniciarEfeitoMorte` próprio — intocados).
- Squash usando o sprite REAL do inimigo (v1 usa blob colorido p/ paridade simples; upgrade futuro).
- Outros elementos de juice (hit-stop, números, level-up) — ficam pra depois.

---

## Componentes (2)

### 1. `KillPopVFX` (novo) — `Assets/scripts/KillPopVFX.cs`
Classe estática com factory + MonoBehaviour auto-destrutível. Self-contained, sem dependência de rede.

```
public static void Tocar(Vector3 pos, Color cor, float escala)
```

Monta 4 camadas como GameObjects leves filhos de um root "KillPop", todas animadas por **transform/cor lerpados** (sem `Physics2D`, sem `ParticleSystem`), e o root se auto-destrói após `DUR_TOTAL` (~0.4s):

1. **Squash-pop** (~0.12s): um disco/blob (sprite de disco gerado/cacheado) na cor `cor`. Frame inicial: esmagado (scaleX ~1.4, scaleY ~0.5) → estoura (scale → ~1.6×`escala`) + alpha 1→0.
2. **Estilhaços** (~0.4s): `NUM_CACOS` (5) discos pequenos na cor, posição inicial = `pos`, velocidade radial aleatória + leve gravidade, alpha 0.9→0 + encolhe.
3. **Anel** (~0.25s): `LineRenderer` loop (anel fino), raio 0.1→`RAIO_ANEL`×`escala`, cor `cor` com alpha 0.8→0, largura afinando.
4. **Flash** (~0.08s): disco branco, escala ~`escala`, alpha 0.9→0 (o "confirm" do acerto).

Sorting: `sortingOrder` alto (acima do chão/inimigos). Texturas de disco/anel **cacheadas estáticas** (geradas uma vez), iguais ao padrão dos outros efeitos do projeto (ex.: `SlimeColorida.EfeitoMorte`, `CampoEspinhos.GerarDisco`).

**Constantes tunáveis no topo** (dialar "explosivo vs limpo"): `NUM_CACOS`, `RAIO_ANEL`, `MULT_ESCALA`, durações por camada, velocidade dos cacos.

### 2. `EnemyNet.BroadcastMorteVFX` (em `Assets/scripts/net/EnemyNet.cs`)
Mesmo padrão de `ReplicarNumeroDano`:

```
public void BroadcastMorteVFX(Vector2 pos, Color cor, float escala)  // host → if IsServer && IsSpawned
[Rpc(SendTo.NotServer)] void MorteVFXClientRpc(Vector2 pos, float r, float g, float b, float escala)
    => KillPopVFX.Tocar(pos, new Color(r,g,b), escala);  // só no cliente
```

---

## Fluxo

Em `InimigoController.Morrer()`, no ramo **não-boss** (onde hoje só chama `NetSpawn.Despawnar(gameObject)`):

```
Color cor = spriteRenderer != null ? spriteRenderer.color : Color.white;
float esc = Mathf.Abs(transform.localScale.x);
KillPopVFX.Tocar(transform.position, cor, esc);        // local (host em co-op; e SP)
GetComponent<EnemyNet>()?.BroadcastMorteVFX(transform.position, cor, esc);  // co-op → P2 vê igual
NetSpawn.Despawnar(gameObject);
```

- **SP:** `EnemyNet` é null/não-spawnado → só o `Tocar` local. Funciona.
- **Co-op host:** `Tocar` local + broadcast → cliente toca o mesmo.
- **Co-op cliente:** `Morrer` não roda no fantoche (host-autoritativo) → o VFX vem só pelo RPC. Sem duplicação.

`spriteRenderer.color` é o tint: branco/claro na maioria (pop de poeira clássico), e combina quando o inimigo está tintado (status/slime colorida). Suficiente p/ v1.

---

## Corretude co-op
- O VFX é **local e não-networkizado** em cada máquina (sem NetworkObject por morte → sem custo de spawn/despawn de rede).
- Custo de rede = 1 RPC pequeno por morte de inimigo normal, **menos frequente** que o `ReplicarNumeroDano` (que é por acerto) — já aceito no projeto. Fila do transport já foi pra 512.
- Bosses: não passam por esse ramo (mantêm efeito próprio).

## Performance
- ~8 GameObjects leves por morte, auto-destruídos em ≤0.4s.
- Sem `ParticleSystem`/`Physics2D`; só `LineRenderer`/`SpriteRenderer` + lerp em `Update`/coroutine.
- Texturas cacheadas (geradas 1×). Alvo: 40 mortes/s sem stutter.

## Verificação
- **Compila** 0 erros (`refresh_unity`).
- **SP (execute_code ou play):** matar inimigo → pop aparece, some em ~0.4s, sem erro de console, sem leak (objetos destruídos).
- **Co-op (MPPM):** matar inimigo no host → pop aparece nos DOIS; matar via P2 (dano roteado) → pop nos dois; bosses mantêm efeito próprio; sem pop em despawn não-morte.
- **Perf:** horda densa morrendo em massa sem queda perceptível de FPS.

## Riscos / mitigação
- **Spam de RPC em morte em massa:** morte < acerto em frequência; fila 512. Se pesar, throttle/batch é evolução fácil.
- **Cor branca pouco vistosa:** aceitável p/ v1 (poeira). Upgrade: campo `corMorte` em `dadosInimigo` ou amostra do sprite.
