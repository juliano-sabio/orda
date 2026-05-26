# CharacterSelection Dark Fantasy UI — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Criar 5 assets pixel art dark fantasy no Aseprite, exportar como PNG, integrá-los ao CharacterSelectionUI.cs via campos de sprite com 9-slice e fallback para cor sólida.

**Architecture:** Assets criados via Aseprite MCP → exportados como PNG → importados no Unity como Sprites com 9-slice borders → CharacterSelectionUI.cs recebe campos públicos `Sprite` e aplica nos `Image` components com `Image.Type.Sliced` / `Simple`. Se o campo for `null`, comportamento atual (cor sólida) é mantido.

**Tech Stack:** Aseprite MCP tools (`create_canvas`, `fill_area`, `draw_rectangle`, `draw_pixels`, `draw_line`, `export_sprite`), Unity UI (uGUI Image, 9-slice), C# / TextMeshPro, Unity MCP (`manage_script`, `read_console`).

---

## File Map

| Ação | Caminho |
|---|---|
| Criar | `horda/Assets/assets/UI/charselection/bg_charselection.ase` |
| Criar | `horda/Assets/assets/UI/charselection/panel_stone.ase` |
| Criar | `horda/Assets/assets/UI/charselection/frame_preview.ase` |
| Criar | `horda/Assets/assets/UI/charselection/bar_charselect.ase` |
| Criar | `horda/Assets/assets/UI/charselection/btn_stone.ase` |
| Criar | `horda/Assets/assets/UI/charselection/*.png` (exportados) |
| Modificar | `horda/Assets/scripts/UI/CharacterSelectionUI.cs` |

---

## Task 1: Criar pasta e asset de fundo (`bg_charselection.ase`)

**Files:**
- Criar: `horda/Assets/assets/UI/charselection/bg_charselection.ase`

- [ ] **Step 1: Criar a pasta e o canvas**

```
mcp__aseprite__create_canvas(
  sprite_path: "j:/unity/projetos/horda/orda/horda/Assets/assets/UI/charselection/bg_charselection.ase",
  width: 384, height: 216,
  color_mode: "rgb",
  background_color: "#0D0505"
)
```

- [ ] **Step 2: Camada base — escuridão total**

```
fill_area(sprite_path: "...bg_charselection.ase", layer_name: "Layer 1",
  frame_number: 1, x: 0, y: 0, color: "#0D0505", tolerance: 0)
```

- [ ] **Step 3: Parede traseira — luz avermelhada central (faixas do centro para a borda)**

```
draw_rectangle(..., x:80, y:20, width:224, height:140, color:"#3D1010", filled:true)
draw_rectangle(..., x:120, y:40, width:144, height:100, color:"#5A1A1A", filled:true)
draw_rectangle(..., x:152, y:60, width:80,  height:70,  color:"#6B1A1A", filled:true)
draw_rectangle(..., x:168, y:75, width:48,  height:50,  color:"#7A2020", filled:true)
```

- [ ] **Step 4: Piso de pedra (rodapé)**

```
draw_rectangle(..., x:0,   y:170, width:384, height:46, color:"#1A0808", filled:true)
draw_rectangle(..., x:0,   y:172, width:384, height:2,  color:"#3A2020", filled:false)
draw_rectangle(..., x:20,  y:176, width:60,  height:12, color:"#2D1515", filled:true)
draw_rectangle(..., x:100, y:178, width:80,  height:10, color:"#251010", filled:true)
draw_rectangle(..., x:200, y:175, width:70,  height:13, color:"#2D1515", filled:true)
draw_rectangle(..., x:290, y:177, width:60,  height:11, color:"#251010", filled:true)
draw_line(..., x1:0, y1:174, x2:384, y2:174, color:"#5A3030")
draw_line(..., x1:0, y1:196, x2:384, y2:196, color:"#3A2020")
```

- [ ] **Step 5: Colunas/paredes laterais**

```
draw_rectangle(..., x:0,  y:0, width:72, height:216, color:"#140606", filled:true)
draw_rectangle(..., x:312,y:0, width:72, height:216, color:"#140606", filled:true)
draw_line(..., x1:70, y1:0, x2:70, y2:170, color:"#3A2020")
draw_line(..., x1:71, y1:0, x2:71, y2:170, color:"#1A0808")
draw_line(..., x1:312,y1:0, x2:312,y2:170, color:"#1A0808")
draw_line(..., x1:313,y1:0, x2:313,y2:170, color:"#3A2020")
```

- [ ] **Step 6: Blocos de pedra nas paredes laterais**

```
# Parede esquerda — linhas de bloco
draw_line(..., x1:10, y1:40,  x2:68, y2:40,  color:"#2D1515")
draw_line(..., x1:10, y1:80,  x2:68, y2:80,  color:"#2D1515")
draw_line(..., x1:10, y1:120, x2:68, y2:120, color:"#2D1515")
draw_line(..., x1:40, y1:0,   x2:40, y2:40,  color:"#2D1515")
draw_line(..., x1:40, y1:80,  x2:40, y2:120, color:"#2D1515")
# Parede direita — espelho
draw_line(..., x1:316, y1:40,  x2:374, y2:40,  color:"#2D1515")
draw_line(..., x1:316, y1:80,  x2:374, y2:80,  color:"#2D1515")
draw_line(..., x1:316, y1:120, x2:374, y2:120, color:"#2D1515")
draw_line(..., x1:344, y1:0,   x2:344, y2:40,  color:"#2D1515")
draw_line(..., x1:344, y1:80,  x2:344, y2:120, color:"#2D1515")
```

- [ ] **Step 7: Arco gótico central ao fundo**

```
# Base do arco
draw_rectangle(..., x:148, y:60, width:88, height:110, color:"#0A0303", filled:true)
draw_rectangle(..., x:152, y:64, width:80, height:106, color:"#1A0808", filled:true)
# Luz dentro do arco
draw_rectangle(..., x:156, y:68, width:72, height:100, color:"#3D1010", filled:true)
draw_rectangle(..., x:164, y:75, width:56, height:90,  color:"#5A1818", filled:true)
# Curvatura do topo do arco (simula arco com retângulo levemente menor)
draw_rectangle(..., x:156, y:68, width:72, height:20, color:"#0A0303", filled:true)
draw_rectangle(..., x:162, y:68, width:60, height:18, color:"#1A0808", filled:true)
draw_rectangle(..., x:168, y:70, width:48, height:12, color:"#3D1010", filled:true)
```

- [ ] **Step 8: Teia de aranha — canto superior esquerdo**

```
draw_pixels(..., pixels: [
  # Centro da teia
  {x:8,  y:8,  color:"#8B8B8B"},
  # Raios principais
  {x:0,  y:0,  color:"#6A6A6A"}, {x:4,  y:0,  color:"#6A6A6A"},
  {x:16, y:0,  color:"#6A6A6A"}, {x:24, y:4,  color:"#6A6A6A"},
  {x:0,  y:16, color:"#6A6A6A"}, {x:0,  y:24, color:"#6A6A6A"},
  # Fios horizontais/verticais
  {x:2,  y:2,  color:"#555555"}, {x:6,  y:4,  color:"#555555"},
  {x:4,  y:6,  color:"#555555"}, {x:10, y:3,  color:"#555555"},
  {x:3,  y:10, color:"#555555"}, {x:14, y:2,  color:"#555555"},
  {x:2,  y:14, color:"#555555"}, {x:18, y:5,  color:"#555555"},
  {x:5,  y:18, color:"#555555"}, {x:20, y:10, color:"#555555"},
  {x:10, y:20, color:"#555555"}, {x:22, y:14, color:"#555555"},
  {x:14, y:22, color:"#555555"}
])
```

- [ ] **Step 9: Teia de aranha — canto superior direito (espelhada)**

```
draw_pixels(..., pixels: [
  {x:375, y:8,  color:"#8B8B8B"},
  {x:383, y:0,  color:"#6A6A6A"}, {x:379, y:0,  color:"#6A6A6A"},
  {x:367, y:0,  color:"#6A6A6A"}, {x:359, y:4,  color:"#6A6A6A"},
  {x:383, y:16, color:"#6A6A6A"}, {x:383, y:24, color:"#6A6A6A"},
  {x:381, y:2,  color:"#555555"}, {x:377, y:4,  color:"#555555"},
  {x:379, y:6,  color:"#555555"}, {x:373, y:3,  color:"#555555"},
  {x:380, y:10, color:"#555555"}, {x:369, y:2,  color:"#555555"},
  {x:381, y:14, color:"#555555"}, {x:365, y:5,  color:"#555555"},
  {x:378, y:18, color:"#555555"}, {x:363, y:10, color:"#555555"},
  {x:373, y:20, color:"#555555"}, {x:361, y:14, color:"#555555"},
  {x:369, y:22, color:"#555555"}
])
```

- [ ] **Step 10: Exportar para PNG**

```
export_sprite(
  sprite_path: "...bg_charselection.ase",
  output_path: "...charselection/bg_charselection.png",
  format: "png", frame_number: 1
)
```

- [ ] **Step 11: Ler o PNG e verificar visualmente que o fundo tem profundidade e atmosfera**

```
Read("...charselection/bg_charselection.png")
```

- [ ] **Step 12: Commit**

```bash
git add "horda/Assets/assets/UI/charselection/bg_charselection.ase"
git add "horda/Assets/assets/UI/charselection/bg_charselection.png"
git commit -m "asset: fundo dark fantasy CharacterSelection"
```

---

## Task 2: Criar painel lateral 9-slice (`panel_stone.ase`)

**Files:**
- Criar: `horda/Assets/assets/UI/charselection/panel_stone.ase`

- [ ] **Step 1: Criar canvas 160×160**

```
create_canvas(sprite_path: "...panel_stone.ase",
  width: 160, height: 160, color_mode: "rgb", background_color: "#2D1515")
```

- [ ] **Step 2: Fill center com pedra base**

```
fill_area(..., x: 0, y: 0, color: "#2D1515", tolerance: 0)
```

- [ ] **Step 3: Borda externa escura (1px)**

```
draw_rectangle(..., x:0, y:0, width:160, height:160, color:"#0A0303", filled:false)
```

- [ ] **Step 4: Faixas da borda (do exterior para o interior) — simula entalhe de pedra**

```
# Faixa 1-2px: sombra de pedra escura
draw_rectangle(..., x:1, y:1, width:158, height:158, color:"#1A0808", filled:false)
draw_rectangle(..., x:2, y:2, width:156, height:156, color:"#1A0808", filled:false)

# Faixa 3-4px: pedra base
draw_rectangle(..., x:3, y:3, width:154, height:154, color:"#2D1515", filled:false)
draw_rectangle(..., x:4, y:4, width:152, height:152, color:"#3A2020", filled:false)

# Faixa 5-7px: highlight de cinzel (topo/esquerda mais claro)
draw_rectangle(..., x:5, y:5, width:150, height:150, color:"#5A3030", filled:false)
draw_rectangle(..., x:6, y:6, width:148, height:148, color:"#7A4040", filled:false)
draw_rectangle(..., x:7, y:7, width:146, height:146, color:"#9A6060", filled:false)

# Faixa 8px: pico do highlight (aresta superior e esquerda mais brilhantes)
draw_line(..., x1:8, y1:8, x2:151, y2:8,   color:"#BF8080")  # topo
draw_line(..., x1:8, y1:8, x2:8,   y2:151, color:"#BF8080")  # esquerda

# Faixa 8px base/direita escura (sombra oposta — entalhe 3D)
draw_line(..., x1:8,   y1:151, x2:151, y2:151, color:"#1A0808")  # base
draw_line(..., x1:151, y1:8,   x2:151, y2:151, color:"#1A0808")  # direita

# Faixa 9-11px: retorno ao escuro (fundo do entalhe)
draw_rectangle(..., x:9,  y:9,  width:142, height:142, color:"#5A3030", filled:false)
draw_rectangle(..., x:10, y:10, width:140, height:140, color:"#3A2020", filled:false)
draw_rectangle(..., x:11, y:11, width:138, height:138, color:"#2D1515", filled:false)

# Faixa 12-13px: sombra interior
draw_rectangle(..., x:12, y:12, width:136, height:136, color:"#1A0808", filled:false)
draw_rectangle(..., x:13, y:13, width:134, height:134, color:"#140606", filled:false)
```

- [ ] **Step 5: Centro fill limpo (área que vai escalar)**

```
draw_rectangle(..., x:14, y:14, width:132, height:132, color:"#2D1515", filled:true)
```

- [ ] **Step 6: Ornamento dourado nos 4 cantos (cruz 5×5 em cada canto 14×14)**

```
draw_pixels(..., pixels: [
  # Canto superior esquerdo (centro em 7,7)
  {x:7,  y:5,  color:"#C8A840"}, {x:7,  y:6,  color:"#E8C050"},
  {x:5,  y:7,  color:"#C8A840"}, {x:6,  y:7,  color:"#E8C050"},
  {x:7,  y:7,  color:"#F0D060"}, {x:8,  y:7,  color:"#E8C050"},
  {x:9,  y:7,  color:"#C8A840"}, {x:7,  y:8,  color:"#E8C050"},
  {x:7,  y:9,  color:"#C8A840"}, {x:6,  y:6,  color:"#9A7E30"},
  {x:8,  y:6,  color:"#9A7E30"}, {x:6,  y:8,  color:"#9A7E30"},
  {x:8,  y:8,  color:"#9A7E30"},

  # Canto superior direito (centro em 152,7)
  {x:152, y:5,  color:"#C8A840"}, {x:152, y:6,  color:"#E8C050"},
  {x:150, y:7,  color:"#C8A840"}, {x:151, y:7,  color:"#E8C050"},
  {x:152, y:7,  color:"#F0D060"}, {x:153, y:7,  color:"#E8C050"},
  {x:154, y:7,  color:"#C8A840"}, {x:152, y:8,  color:"#E8C050"},
  {x:152, y:9,  color:"#C8A840"}, {x:151, y:6,  color:"#9A7E30"},
  {x:153, y:6,  color:"#9A7E30"}, {x:151, y:8,  color:"#9A7E30"},
  {x:153, y:8,  color:"#9A7E30"},

  # Canto inferior esquerdo (centro em 7,152)
  {x:7,  y:150, color:"#C8A840"}, {x:7,  y:151, color:"#E8C050"},
  {x:5,  y:152, color:"#C8A840"}, {x:6,  y:152, color:"#E8C050"},
  {x:7,  y:152, color:"#F0D060"}, {x:8,  y:152, color:"#E8C050"},
  {x:9,  y:152, color:"#C8A840"}, {x:7,  y:153, color:"#E8C050"},
  {x:7,  y:154, color:"#C8A840"}, {x:6,  y:151, color:"#9A7E30"},
  {x:8,  y:151, color:"#9A7E30"}, {x:6,  y:153, color:"#9A7E30"},
  {x:8,  y:153, color:"#9A7E30"},

  # Canto inferior direito (centro em 152,152)
  {x:152, y:150, color:"#C8A840"}, {x:152, y:151, color:"#E8C050"},
  {x:150, y:152, color:"#C8A840"}, {x:151, y:152, color:"#E8C050"},
  {x:152, y:152, color:"#F0D060"}, {x:153, y:152, color:"#E8C050"},
  {x:154, y:152, color:"#C8A840"}, {x:152, y:153, color:"#E8C050"},
  {x:152, y:154, color:"#C8A840"}, {x:151, y:151, color:"#9A7E30"},
  {x:153, y:151, color:"#9A7E30"}, {x:151, y:153, color:"#9A7E30"},
  {x:153, y:153, color:"#9A7E30"}
])
```

- [ ] **Step 7: Exportar PNG e verificar**

```
export_sprite(...output_path: "...panel_stone.png", format:"png", frame_number:1)
Read("...panel_stone.png")
```

- [ ] **Step 8: Commit**

```bash
git add "horda/Assets/assets/UI/charselection/panel_stone.ase"
git add "horda/Assets/assets/UI/charselection/panel_stone.png"
git commit -m "asset: painel 9-slice pedra dark fantasy"
```

---

## Task 3: Criar moldura do preview (`frame_preview.ase`)

**Files:**
- Criar: `horda/Assets/assets/UI/charselection/frame_preview.ase`

- [ ] **Step 1: Criar canvas 128×192 com centro transparente**

```
create_canvas(sprite_path: "...frame_preview.ase",
  width: 128, height: 192, color_mode: "rgba",
  background_color: "#00000000")
```

- [ ] **Step 2: Desenhar borda externa (16px) com pedra esculpida**

```
# Borda externa 1px
draw_rectangle(..., x:0, y:0, width:128, height:192, color:"#0A0303", filled:false)

# Faixas de pedra (1-15px)
draw_rectangle(..., x:1,  y:1,  width:126, height:190, color:"#1A0808", filled:false)
draw_rectangle(..., x:2,  y:2,  width:124, height:188, color:"#2D1515", filled:false)
draw_rectangle(..., x:3,  y:3,  width:122, height:186, color:"#3A2020", filled:false)
draw_rectangle(..., x:4,  y:4,  width:120, height:184, color:"#5A3030", filled:false)
draw_rectangle(..., x:5,  y:5,  width:118, height:182, color:"#7A4040", filled:false)
draw_rectangle(..., x:6,  y:6,  width:116, height:180, color:"#9A6060", filled:false)
draw_rectangle(..., x:7,  y:7,  width:114, height:178, color:"#BF8080", filled:false)
# Topo e esquerda - destaque dourado (mais ornamentada que o painel simples)
draw_line(..., x1:8, y1:8, x2:119, y2:8,   color:"#C8A840")
draw_line(..., x1:8, y1:8, x2:8,   y2:183, color:"#C8A840")
# Base e direita - sombra
draw_line(..., x1:8,   y1:183, x2:119, y2:183, color:"#1A0808")
draw_line(..., x1:119, y1:8,   x2:119, y2:183, color:"#1A0808")
# Retorno interior
draw_rectangle(..., x:9,  y:9,  width:110, height:174, color:"#9A6060", filled:false)
draw_rectangle(..., x:10, y:10, width:108, height:172, color:"#7A4040", filled:false)
draw_rectangle(..., x:11, y:11, width:106, height:170, color:"#5A3030", filled:false)
draw_rectangle(..., x:12, y:12, width:104, height:168, color:"#3A2020", filled:false)
draw_rectangle(..., x:13, y:13, width:102, height:166, color:"#2D1515", filled:false)
draw_rectangle(..., x:14, y:14, width:100, height:164, color:"#1A0808", filled:false)
draw_rectangle(..., x:15, y:15, width:98,  height:162, color:"#0A0303", filled:false)
```

- [ ] **Step 3: Centro completamente transparente**

```
draw_rectangle(..., x:16, y:16, width:96, height:160, color:"#00000000", filled:true)
```

- [ ] **Step 4: Ornamentos dourados nos cantos (maiores — 16×16)**

```
draw_pixels(..., pixels: [
  # Canto superior esquerdo — ornamento em L dourado
  {x:2,  y:2,  color:"#9A7E30"}, {x:3,  y:2,  color:"#C8A840"},
  {x:4,  y:2,  color:"#E8C050"}, {x:5,  y:2,  color:"#C8A840"},
  {x:6,  y:2,  color:"#9A7E30"}, {x:2,  y:3,  color:"#C8A840"},
  {x:2,  y:4,  color:"#E8C050"}, {x:2,  y:5,  color:"#C8A840"},
  {x:2,  y:6,  color:"#9A7E30"}, {x:4,  y:4,  color:"#E8C050"},
  # Canto superior direito
  {x:121, y:2,  color:"#9A7E30"}, {x:122, y:2,  color:"#C8A840"},
  {x:123, y:2,  color:"#E8C050"}, {x:124, y:2,  color:"#C8A840"},
  {x:125, y:2,  color:"#9A7E30"}, {x:125, y:3,  color:"#C8A840"},
  {x:125, y:4,  color:"#E8C050"}, {x:125, y:5,  color:"#C8A840"},
  {x:125, y:6,  color:"#9A7E30"}, {x:123, y:4,  color:"#E8C050"},
  # Canto inferior esquerdo
  {x:2,  y:185, color:"#9A7E30"}, {x:3,  y:185, color:"#C8A840"},
  {x:4,  y:185, color:"#E8C050"}, {x:5,  y:185, color:"#C8A840"},
  {x:6,  y:185, color:"#9A7E30"}, {x:2,  y:186, color:"#C8A840"},
  {x:2,  y:187, color:"#E8C050"}, {x:2,  y:188, color:"#C8A840"},
  {x:2,  y:189, color:"#9A7E30"}, {x:4,  y:187, color:"#E8C050"},
  # Canto inferior direito
  {x:121, y:189, color:"#9A7E30"}, {x:122, y:189, color:"#C8A840"},
  {x:123, y:189, color:"#E8C050"}, {x:124, y:189, color:"#C8A840"},
  {x:125, y:189, color:"#9A7E30"}, {x:125, y:188, color:"#C8A840"},
  {x:125, y:187, color:"#E8C050"}, {x:125, y:186, color:"#C8A840"},
  {x:125, y:185, color:"#9A7E30"}, {x:123, y:187, color:"#E8C050"}
])
```

- [ ] **Step 5: Detalhe dourado no centro do topo e base (ornamento linear)**

```
draw_pixels(..., pixels: [
  # Centro topo (x=64, y=4..8)
  {x:60, y:4, color:"#9A7E30"}, {x:61, y:4, color:"#C8A840"},
  {x:62, y:3, color:"#C8A840"}, {x:63, y:3, color:"#E8C050"},
  {x:64, y:2, color:"#F0D060"}, {x:65, y:3, color:"#E8C050"},
  {x:66, y:3, color:"#C8A840"}, {x:67, y:4, color:"#C8A840"},
  {x:68, y:4, color:"#9A7E30"},
  # Centro base (x=64, y=183..188)
  {x:60, y:188, color:"#9A7E30"}, {x:61, y:188, color:"#C8A840"},
  {x:62, y:189, color:"#C8A840"}, {x:63, y:189, color:"#E8C050"},
  {x:64, y:190, color:"#F0D060"}, {x:65, y:189, color:"#E8C050"},
  {x:66, y:189, color:"#C8A840"}, {x:67, y:188, color:"#C8A840"},
  {x:68, y:188, color:"#9A7E30"}
])
```

- [ ] **Step 6: Exportar e verificar**

```
export_sprite(...output_path: "...frame_preview.png", format:"png", frame_number:1)
Read("...frame_preview.png")
```

- [ ] **Step 7: Commit**

```bash
git add "horda/Assets/assets/UI/charselection/frame_preview.ase"
git add "horda/Assets/assets/UI/charselection/frame_preview.png"
git commit -m "asset: moldura preview 9-slice dark fantasy"
```

---

## Task 4: Criar faixa de seleção de personagens (`bar_charselect.ase`)

**Files:**
- Criar: `horda/Assets/assets/UI/charselection/bar_charselect.ase`

- [ ] **Step 1: Criar canvas 256×64**

```
create_canvas(sprite_path: "...bar_charselect.ase",
  width: 256, height: 64, color_mode: "rgb", background_color: "#1A0808")
```

- [ ] **Step 2: Fill base**

```
fill_area(..., x:0, y:0, color:"#1A0808", tolerance:0)
```

- [ ] **Step 3: Borda superior — entalhe de pedra (12px)**

```
draw_line(..., x1:0, y1:0,  x2:256, y2:0,  color:"#0A0303")
draw_line(..., x1:0, y1:1,  x2:256, y2:1,  color:"#1A0808")
draw_line(..., x1:0, y1:2,  x2:256, y2:2,  color:"#2D1515")
draw_line(..., x1:0, y1:3,  x2:256, y2:3,  color:"#3A2020")
draw_line(..., x1:0, y1:4,  x2:256, y2:4,  color:"#5A3030")
draw_line(..., x1:0, y1:5,  x2:256, y2:5,  color:"#7A4040")
draw_line(..., x1:0, y1:6,  x2:256, y2:6,  color:"#9A6060")
draw_line(..., x1:0, y1:7,  x2:256, y2:7,  color:"#BF8080")
draw_line(..., x1:0, y1:8,  x2:256, y2:8,  color:"#9A6060")
draw_line(..., x1:0, y1:9,  x2:256, y2:9,  color:"#5A3030")
draw_line(..., x1:0, y1:10, x2:256, y2:10, color:"#3A2020")
draw_line(..., x1:0, y1:11, x2:256, y2:11, color:"#1A0808")
```

- [ ] **Step 4: Borda inferior — espelho**

```
draw_line(..., x1:0, y1:63, x2:256, y2:63, color:"#0A0303")
draw_line(..., x1:0, y1:62, x2:256, y2:62, color:"#1A0808")
draw_line(..., x1:0, y1:61, x2:256, y2:61, color:"#2D1515")
draw_line(..., x1:0, y1:60, x2:256, y2:60, color:"#3A2020")
draw_line(..., x1:0, y1:59, x2:256, y2:59, color:"#5A3030")
draw_line(..., x1:0, y1:58, x2:256, y2:58, color:"#7A4040")
draw_line(..., x1:0, y1:57, x2:256, y2:57, color:"#9A6060")
draw_line(..., x1:0, y1:56, x2:256, y2:56, color:"#BF8080")
draw_line(..., x1:0, y1:55, x2:256, y2:55, color:"#9A6060")
draw_line(..., x1:0, y1:54, x2:256, y2:54, color:"#5A3030")
draw_line(..., x1:0, y1:53, x2:256, y2:53, color:"#3A2020")
draw_line(..., x1:0, y1:52, x2:256, y2:52, color:"#1A0808")
```

- [ ] **Step 5: Bordas laterais (12px cada lado)**

```
draw_rectangle(..., x:0,   y:0, width:12,  height:64, color:"#1A0808", filled:true)
draw_rectangle(..., x:244, y:0, width:12,  height:64, color:"#1A0808", filled:true)
draw_line(..., x1:0,   y1:0, x2:0,   y2:64, color:"#0A0303")
draw_line(..., x1:11,  y1:0, x2:11,  y2:64, color:"#3A2020")
draw_line(..., x1:244, y1:0, x2:244, y2:64, color:"#3A2020")
draw_line(..., x1:255, y1:0, x2:255, y2:64, color:"#0A0303")
```

- [ ] **Step 6: Exportar e verificar**

```
export_sprite(...output_path:"...bar_charselect.png", format:"png", frame_number:1)
Read("...bar_charselect.png")
```

- [ ] **Step 7: Commit**

```bash
git add "horda/Assets/assets/UI/charselection/bar_charselect.ase"
git add "horda/Assets/assets/UI/charselection/bar_charselect.png"
git commit -m "asset: faixa seleção 9-slice dark fantasy"
```

---

## Task 5: Criar botão de pedra (`btn_stone.ase`)

**Files:**
- Criar: `horda/Assets/assets/UI/charselection/btn_stone.ase`

- [ ] **Step 1: Criar canvas 128×32**

```
create_canvas(sprite_path: "...btn_stone.ase",
  width: 128, height: 32, color_mode: "rgb", background_color: "#2D1515")
```

- [ ] **Step 2: Fill base interior**

```
fill_area(..., x:0, y:0, color:"#2D1515", tolerance:0)
draw_rectangle(..., x:10, y:10, width:108, height:12, color:"#251212", filled:true)
```

- [ ] **Step 3: Borda com bisel 3D (topo/esquerda claro, base/direita escuro)**

```
# Exterior escuro
draw_rectangle(..., x:0, y:0, width:128, height:32, color:"#0A0303", filled:false)

# Faixas da borda (10px)
draw_line(..., x1:1, y1:1, x2:127, y2:1,  color:"#BF8080")  # topo claro
draw_line(..., x1:1, y1:1, x2:1,   y2:31, color:"#BF8080")  # esq claro
draw_line(..., x1:1, y1:30, x2:127, y2:30, color:"#0A0303") # base escuro
draw_line(..., x1:126, y1:1, x2:126, y2:30, color:"#0A0303") # dir escuro

draw_line(..., x1:2, y1:2, x2:126, y2:2,  color:"#9A6060")
draw_line(..., x1:2, y1:2, x2:2,   y2:30, color:"#9A6060")
draw_line(..., x1:2, y1:29, x2:126, y2:29, color:"#1A0808")
draw_line(..., x1:125, y1:2, x2:125, y2:29, color:"#1A0808")

draw_line(..., x1:3, y1:3, x2:125, y2:3,  color:"#7A4040")
draw_line(..., x1:3, y1:3, x2:3,   y2:29, color:"#7A4040")
draw_line(..., x1:3, y1:28, x2:125, y2:28, color:"#2D1515")
draw_line(..., x1:124, y1:3, x2:124, y2:28, color:"#2D1515")

draw_line(..., x1:4, y1:4, x2:124, y2:4,  color:"#5A3030")
draw_line(..., x1:4, y1:4, x2:4,   y2:28, color:"#5A3030")
draw_line(..., x1:4, y1:27, x2:124, y2:27, color:"#3A2020")
draw_line(..., x1:123, y1:4, x2:123, y2:27, color:"#3A2020")

draw_line(..., x1:5, y1:5, x2:123, y2:5,  color:"#3A2020")
draw_line(..., x1:5, y1:5, x2:5,   y2:27, color:"#3A2020")
draw_line(..., x1:5, y1:26, x2:123, y2:26, color:"#3A2020")
draw_line(..., x1:122, y1:5, x2:122, y2:26, color:"#3A2020")

draw_rectangle(..., x:6,  y:6, width:116, height:20, color:"#2D1515", filled:false)
draw_rectangle(..., x:7,  y:7, width:114, height:18, color:"#251212", filled:false)
draw_rectangle(..., x:8,  y:8, width:112, height:16, color:"#1A0808", filled:false)
draw_rectangle(..., x:9,  y:9, width:110, height:14, color:"#140606", filled:false)
```

- [ ] **Step 4: Centro do botão**

```
draw_rectangle(..., x:10, y:10, width:108, height:12, color:"#2D1515", filled:true)
```

- [ ] **Step 5: Pequenos ornamentos dourados nos cantos**

```
draw_pixels(..., pixels: [
  {x:2, y:2,  color:"#9A7E30"}, {x:3, y:2,  color:"#C8A840"},
  {x:2, y:3,  color:"#C8A840"},
  {x:124, y:2, color:"#9A7E30"}, {x:125, y:2, color:"#C8A840"},
  {x:125, y:3, color:"#C8A840"},
  {x:2,   y:29, color:"#9A7E30"}, {x:3,  y:29, color:"#C8A840"},
  {x:2,   y:28, color:"#C8A840"},
  {x:124, y:29, color:"#9A7E30"}, {x:125, y:29, color:"#C8A840"},
  {x:125, y:28, color:"#C8A840"}
])
```

- [ ] **Step 6: Exportar e verificar**

```
export_sprite(...output_path:"...btn_stone.png", format:"png", frame_number:1)
Read("...btn_stone.png")
```

- [ ] **Step 7: Commit**

```bash
git add "horda/Assets/assets/UI/charselection/btn_stone.ase"
git add "horda/Assets/assets/UI/charselection/btn_stone.png"
git commit -m "asset: botao 9-slice pedra dark fantasy"
```

---

## Task 6: Modificar `CharacterSelectionUI.cs` para suportar sprites

**Files:**
- Modificar: `horda/Assets/scripts/UI/CharacterSelectionUI.cs`

- [ ] **Step 1: Adicionar campos de sprite após os campos de animação (linha ~67, antes de `corAtualGlow`)**

```csharp
[Header("Sprites Dark Fantasy")]
public Sprite spriteFundo;
public Sprite spritePainel;
public Sprite spriteMolduraPreview;
public Sprite spriteBarraTopo;
public Sprite spriteBotao;
```

- [ ] **Step 2: Modificar `CriarFundo()` — aplicar sprite de fundo com fallback**

Substituir a linha `Img(root, "Fundo", Vector2.zero, Vector2.one, corFundo);` por:

```csharp
void CriarFundo(GameObject root)
{
    var fundoGO = Img(root, "Fundo", Vector2.zero, Vector2.one, corFundo);
    if (spriteFundo != null)
    {
        var img = fundoGO.GetComponent<Image>();
        img.sprite = spriteFundo;
        img.type   = Image.Type.Simple;
        img.color  = Color.white;
    }

    var g = Img(root, "GlowFundo", new Vector2(0.15f,0.15f), new Vector2(0.85f,0.85f),
        new Color(corAcento.r, corAcento.g, corAcento.b, 0.07f));
    glowFundo = g.GetComponent<Image>();

    Img(root, "FaixaTopo", new Vector2(0f,0.92f), Vector2.one,
        new Color(corAcento.r, corAcento.g, corAcento.b, 0.12f));

    Img(root, "FaixaBot", Vector2.zero, new Vector2(1f, 0.08f),
        new Color(0f, 0f, 0f, 0.50f));
}
```

- [ ] **Step 3: Modificar `CriarPainelIcones()` — aplicar sprite na faixa**

Após `var painel = Img(...)`, adicionar:

```csharp
if (spriteBarraTopo != null)
{
    var img = painel.GetComponent<Image>();
    img.sprite = spriteBarraTopo;
    img.type   = Image.Type.Sliced;
    img.color  = Color.white;
}
```

- [ ] **Step 4: Modificar `CriarAreaCentral()` — painéis laterais e moldura**

Após `var info = Img(root, "PainelInfo", ...)`, adicionar:
```csharp
if (spritePainel != null)
{
    var img = info.GetComponent<Image>();
    img.sprite = spritePainel;
    img.type   = Image.Type.Sliced;
    img.color  = Color.white;
}
```

Após `var status = Img(root, "PainelStatus", ...)`, adicionar:
```csharp
if (spritePainel != null)
{
    var img = status.GetComponent<Image>();
    img.sprite = spritePainel;
    img.type   = Image.Type.Sliced;
    img.color  = Color.white;
}
```

Após `Anchors(prevGO, ...)` (onde previewRawImage é configurado), adicionar moldura:
```csharp
if (spriteMolduraPreview != null)
{
    var molduraGO  = Img(painelPreview, "MolduraPreview", Vector2.zero, Vector2.one, Color.white);
    var molduraImg = molduraGO.GetComponent<Image>();
    molduraImg.sprite     = spriteMolduraPreview;
    molduraImg.type       = Image.Type.Sliced;
    molduraImg.fillCenter = false;
    molduraImg.color      = Color.white;
    // Moldura fica na frente do preview
    molduraGO.transform.SetAsLastSibling();
}
```

- [ ] **Step 5: Modificar `CriarBotao()` — aplicar sprite nos botões**

Substituir `var img = go.AddComponent<Image>(); img.color = cor;` por:

```csharp
var img = go.AddComponent<Image>();
if (spriteBotao != null)
{
    img.sprite = spriteBotao;
    img.type   = Image.Type.Sliced;
    img.color  = Color.white;
}
else
{
    img.color = cor;
}
```

- [ ] **Step 6: Verificar compilação**

```
mcp__unity-mcp__read_console(types: ["error"])
```
Esperado: 0 erros relacionados a CharacterSelectionUI.

- [ ] **Step 7: Commit**

```bash
git add "horda/Assets/scripts/UI/CharacterSelectionUI.cs"
git commit -m "feat: CharacterSelectionUI suporta sprites 9-slice dark fantasy"
```

---

## Task 7: Configurar 9-slice no Unity e atribuir sprites no Inspector

**Files:**
- Unity Editor: Sprite Editor de cada PNG importado
- Unity Inspector: `gerenciadoUI` GameObject → componente `CharacterSelectionUI`

- [ ] **Step 1: Reimportar assets via Unity MCP**

```
mcp__unity-mcp__refresh_unity()
```

- [ ] **Step 2: Configurar borders de cada sprite via execute_code**

```csharp
// Executar para cada sprite — ajustar nome conforme o arquivo
var ti = AssetImporter.GetAtPath("Assets/assets/UI/charselection/panel_stone.png")
    as TextureImporter;
ti.spriteImportMode  = SpriteImportMode.Single;
ti.spriteBorder      = new Vector4(14, 14, 14, 14); // left, bottom, right, top
ti.textureType       = TextureImporterType.Sprite;
ti.filterMode        = FilterMode.Point;
ti.textureCompression = TextureImporterCompression.Uncompressed;
AssetDatabase.ImportAsset("Assets/assets/UI/charselection/panel_stone.png",
    ImportAssetOptions.ForceUpdate);
return "panel_stone configurado";
```

Repetir para cada asset com seus borders:
- `bg_charselection.png` → border `(0,0,0,0)`, type `Default` (não é 9-slice)
- `panel_stone.png` → border `(14,14,14,14)`
- `frame_preview.png` → border `(16,16,16,16)`
- `bar_charselect.png` → border `(12,12,12,12)`
- `btn_stone.png` → border `(10,10,10,10)`

- [ ] **Step 3: Atribuir sprites no Inspector via execute_code**

```csharp
var ui = GameObject.Find("gerenciadoUI")?.GetComponent<CharacterSelectionUI>();
if (ui == null) return "gerenciadoUI nao encontrado";

ui.spriteFundo         = AssetDatabase.LoadAssetAtPath<Sprite>(
    "Assets/assets/UI/charselection/bg_charselection.png");
ui.spritePainel        = AssetDatabase.LoadAssetAtPath<Sprite>(
    "Assets/assets/UI/charselection/panel_stone.png");
ui.spriteMolduraPreview = AssetDatabase.LoadAssetAtPath<Sprite>(
    "Assets/assets/UI/charselection/frame_preview.png");
ui.spriteBarraTopo     = AssetDatabase.LoadAssetAtPath<Sprite>(
    "Assets/assets/UI/charselection/bar_charselect.png");
ui.spriteBotao         = AssetDatabase.LoadAssetAtPath<Sprite>(
    "Assets/assets/UI/charselection/btn_stone.png");

UnityEditor.EditorUtility.SetDirty(ui);
UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
return "sprites atribuidos com sucesso";
```

- [ ] **Step 4: Commit da cena**

```bash
git add "horda/Assets/Scenes/CharacterSelection.unity"
git commit -m "feat: sprites dark fantasy atribuidos no CharacterSelection"
```

---

## Task 8: Testar em Play mode e validar resultado visual

- [ ] **Step 1: Entrar em Play mode**

```
mcp__unity-mcp__manage_editor(action: "play")
```

- [ ] **Step 2: Capturar screenshot**

```csharp
// execute_code
string path = Application.dataPath.Replace("/Assets", "/charsel_final.png");
ScreenCapture.CaptureScreenshot(path);
return path;
```

- [ ] **Step 3: Verificar captura**

```
Read("j:/unity/projetos/horda/orda/horda/charsel_final.png")
```

Esperado: tela com fundo de dungeon, painéis de pedra com borda esculpida, moldura ao redor do personagem, botões com estilo de pedra, teias de aranha visíveis.

- [ ] **Step 4: Sair do Play mode**

```
mcp__unity-mcp__manage_editor(action: "stop")
```

- [ ] **Step 5: Commit final**

```bash
git add -A
git commit -m "feat: UI dark fantasy CharacterSelection completa"
```
