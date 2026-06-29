# Máscara ao Cair (downed/morte) + revive na máscara — Design

**Data:** 2026-06-28
**Status:** aprovado (aguardando review do spec)

## Objetivo

Quando o player "morre", o **corpo do personagem some** e a **máscara do servo cai no chão**
com um pequeno quique. Em **co-op**, a máscara fica no chão como **marcador de revive** (o
revive por proximidade já existe e já é centrado na posição do caído). Em **single-player**, é
só uma **animação de morte** (a máscara cai e some junto com o game over — sem revive).

100% cosmético: não altera a lógica de revive/game over, só o visual.

## Asset — a máscara (extraída do personagem)

O servo usa uma máscara branca com chifres (a região clara no topo do sprite) sobre um manto
escuro. Vou **recortar essa região** do sprite do servo (`assets/player/...`) num sprite
`mascara_servo`, exportar como PNG e importar em `Resources/ui/mascara_servo.png` como Sprite
(build-safe, igual fizemos com a logo/fundos). O recorte será iterado com o usuário vendo o
resultado in-game (a região exata dos pixels claros).

Fallback: se o recorte não ficar bom, uso o sprite inteiro do personagem como "máscara"
provisória até ajustar.

## Componentes

### `MascaraCaido` (novo, MonoBehaviour cosmético no player)
- **O que faz:** anima o corpo sumindo + a máscara caindo, e o reverso ao reviver.
- **Como usa:** anexado em runtime a todo player (via `moviment_player2.Start`, igual o
  `MovementDust`) — cobre SP e co-op sem editar prefab.
- **Depende de:** `SpriteRenderer` do player (pra esconder/mostrar o corpo), o sprite
  `mascara_servo` (Resources), e o estado de "caído".

  API:
  - `Cair(bool persistente)` — toca: fade-out + encolhe do corpo; cria objeto `MascaraChao`
    na posição dos pés que cai com quique/rotação e assenta. `persistente=true` (co-op) deixa a
    máscara no chão; `persistente=false` (SP) some depois da animação.
  - `Levantar()` — máscara brilha/sobe e some; corpo faz fade-in. (só co-op)

### `MascaraChao` (objeto da máscara no chão)
- Sprite `mascara_servo`; coroutine de queda (lerp de Y com pequeno quique + leve rotação até
  parar). Em co-op fica parado como marcador; em SP auto-destrói após ~1s.

## Fluxo de dados (co-op-safe por construção)

- **Co-op:** dirigido pelo `PlayerNet.downed` (NetworkVariable já sincronizado). No
  `OnValueChanged` de `downed`: `true` → `MascaraCaido.Cair(persistente:true)`; `false`
  (revive) → `MascaraCaido.Levantar()`. Como `downed` é sincronizado, a animação roda em
  **todas as cópias** (dono + fantoche) sem nenhum RPC novo. A máscara aparece na posição
  sincronizada do caído.
- **Single-player:** `player_stats.Die()` chama `MascaraCaido.Cair(persistente:false)` antes
  de seguir pro game over (a animação é curta; o game over continua normal).

## Não-objetivos (YAGNI)

- Não mexer na lógica/raio/tempo de revive (já funciona).
- Não criar máscara por-personagem (só existe o servo).
- Sem física real (queda é lerp/coroutine, padrão dos outros efeitos do projeto).

## Riscos

- **Recorte da máscara:** principal incerteza visual — iterar com o usuário.
- **Esconder o corpo:** o player tem 1 `SpriteRenderer` principal; esconder/mostrar ele cobre
  o caso. Se houver sprites-filho (sombra/efeito), trato no Cair.
- **Co-op fantoche:** o fantoche tem `SpriteRenderer` e `downed` sincronizado → a animação roda
  igual. O `BloquearAcoesCaido` já congela ações; a máscara é só visual em cima disso.
