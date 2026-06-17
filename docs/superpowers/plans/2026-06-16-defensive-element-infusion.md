# Infusão de Elemento para Skills Defensivas — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dar às skills defensivas um conjunto próprio de 20 características de elemento (2 por elemento), defensivas, aplicadas via um modelo de 3 gatilhos (OnAtivar / OnAtingido / AuraContinua).

**Architecture:** Conjunto paralelo de dados (`DefensiveCharacteristic` em `ElementDefinition`), uma rotina de aplicação `SkillElementEffect.AplicarDefensivo` que gateia por gatilho, markers no player lidos por `PlayerStats.TakeDamage`, hooks nas ~11 behaviors defensivas, e a UI de infusão mostrando o par defensivo quando a skill é defensiva (`SkillData.EhSkillDeAtaque()==false`).

**Tech Stack:** Unity C# (sem harness de testes unitários). **Verificação neste projeto:** compilar via `mcp__unity-mcp__refresh_unity` (compile=request, scope=scripts) + `mcp__unity-mcp__read_console` (types=["error"], esperado 0); lógica pura via `mcp__unity-mcp__execute_code` (edit-mode, corpo de método C# 6, sem `using`, sem funções locais — usar nomes totalmente qualificados tipo `UnityEngine.Object.DestroyImmediate`); comportamento via Play mode manual.

**Spec:** `docs/superpowers/specs/2026-06-16-defensive-element-infusion-design.md`

> Não usar `--no-verify`. Commitar a cada task. Já estamos na `main`; criar branch `feat/infusao-defensiva` antes da Task 1.

---

## Mapa de arquivos

| Arquivo | Responsabilidade | Ação |
|---|---|---|
| `horda/Assets/scripts/Elements/ElementType.cs` | enums | Modificar (add `DefensiveCharacteristicType`, `DefensiveTrigger`) |
| `horda/Assets/scripts/Elements/ElementDefinition.cs` | dados de elemento | Modificar (add classe `DefensiveCharacteristic` + campo) |
| `horda/Assets/scripts/Elements/DefensiveMarkers.cs` | markers de buff no player | Criar |
| `horda/Assets/scripts/Elements/SkillElementEffect.cs` | aplicação de efeitos | Modificar (add `AplicarDefensivo` + switch) |
| `horda/Assets/scripts/player_stats.cs` | dano do player | Modificar (`TakeDamage` lê markers) |
| `horda/Assets/scripts/Editor/PopularCaracteristicasDefensivas.cs` | popular o ElementRegistry | Criar |
| `horda/Assets/scripts/UI/ElementApplicationUI.cs` | UI de infusão | Modificar (`MostrarEtapa2`/`CriarCardPoder`) |
| 11 behaviors em `horda/Assets/scripts/bases_skills/` | hooks de gatilho | Modificar |
| `horda/Assets/Resources/Localization/GameStrings.asset` | i18n | Modificar (`defchar.name/desc.*`) |

---

## Task 0: Branch

- [ ] **Step 1: Criar branch**

```bash
cd "j:/unity/projetos/horda/orda"
git switch -c feat/infusao-defensiva
```

---

## Task 1: Enums + dados (DefensiveCharacteristic em ElementDefinition)

**Files:**
- Modify: `horda/Assets/scripts/Elements/ElementType.cs`
- Modify: `horda/Assets/scripts/Elements/ElementDefinition.cs`

- [ ] **Step 1: Adicionar enums em ElementType.cs**

Acrescentar ao final de `ElementType.cs` (depois do enum `CharacteristicType`):

```csharp
public enum DefensiveTrigger { OnAtivar, OnAtingido, AuraContinua }

public enum DefensiveCharacteristicType
{
    // Fogo
    AuraIgnea, RetaliacaoChamas,
    // Ar
    EsquivaVentosa, SoproRepulsor,
    // Terra
    PeleDePedra, FundacaoFirme,
    // Agua
    MareRestauradora, FluxoVital,
    // Raio
    DescargaReativa, CorrenteReflexiva,
    // Gelo
    ArmaduraGelida, ToqueCongelante,
    // Planta
    Espinhos, RaizesProtetoras,
    // Trevas
    DrenagemSombria, MantoAmaldicoado,
    // Luz
    BencaoSagrada, LuzOfuscante,
    // Corrompido
    CaosDefensivo, PragaReativa
}
```

- [ ] **Step 2: Adicionar classe + campo em ElementDefinition.cs**

Em `ElementDefinition.cs`, acrescentar a classe `DefensiveCharacteristic` (antes de `ElementDefinition`) e o campo dentro de `ElementDefinition`:

```csharp
[System.Serializable]
public class DefensiveCharacteristic
{
    public string nome;
    [TextArea(2, 3)]
    public string descricao;
    public DefensiveCharacteristicType tipo;
    public DefensiveTrigger gatilho;
    public float valor1;
    public float valor2;
}
```

Dentro de `class ElementDefinition`, logo após `public ElementCharacteristic[] caracteristicas = new ElementCharacteristic[2];`:

```csharp
    public DefensiveCharacteristic[] caracteristicasDefensivas = new DefensiveCharacteristic[2];
```

- [ ] **Step 3: Compilar e verificar**

`mcp__unity-mcp__refresh_unity` (compile=request, mode=force, scope=scripts, wait_for_ready=true) → `mcp__unity-mcp__read_console` (types=["error"]). Esperado: 0 erros.

- [ ] **Step 4: Commit**

```bash
git add horda/Assets/scripts/Elements/ElementType.cs horda/Assets/scripts/Elements/ElementDefinition.cs
git commit -m "feat(infusao-def): enums e dado DefensiveCharacteristic"
```

---

## Task 2: Markers de buff + hooks no TakeDamage

**Files:**
- Create: `horda/Assets/scripts/Elements/DefensiveMarkers.cs`
- Modify: `horda/Assets/scripts/player_stats.cs` (método `TakeDamage`, ~linha 1010-1085)

- [ ] **Step 1: Criar DefensiveMarkers.cs**

```csharp
using UnityEngine;

// Markers de buff defensivo aplicados no player por características de infusão.
// Auto-expiram; re-aplicar renova a duração (não empilha duplicado).

public class PeleDePedraMarker : MonoBehaviour
{
    public float reducao = 0.30f;   // fração de dano reduzida (0..1)
    float restante;
    public void Renovar(float dur, float red) { restante = dur; reducao = red; }
    void Update() { restante -= Time.deltaTime; if (restante <= 0f) Destroy(this); }
}

public class FundacaoFirmeMarker : MonoBehaviour
{
    float restante;
    public void Renovar(float dur) { restante = dur; }
    void Update() { restante -= Time.deltaTime; if (restante <= 0f) Destroy(this); }
}

public class EsquivaMarker : MonoBehaviour
{
    public float chance = 0.25f;    // chance de evadir totalmente (0..1)
    float restante;
    public void Renovar(float dur, float ch) { restante = dur; chance = ch; }
    void Update() { restante -= Time.deltaTime; if (restante <= 0f) Destroy(this); }
}
```

- [ ] **Step 2: Hook de Esquiva no topo de TakeDamage**

Em `player_stats.cs`, logo após `if (invulneravel) return;` (linha ~1012):

```csharp
        // Esquiva Ventosa (infusão defensiva) — chance de evadir totalmente
        var esquivaMk = GetComponent<EsquivaMarker>();
        if (esquivaMk != null && Random.value < esquivaMk.chance)
        {
            if (uiManager != null) uiManager.ShowElementChanged("ESQUIVA!");
            return;
        }
```

- [ ] **Step 3: Hook de Pele de Pedra após o cálculo de defesa**

Em `player_stats.cs`, logo após `float remaining = Mathf.Max(0f, damage - defense * 0.5f);` (linha ~1023):

```csharp
        // Pele de Pedra (infusão defensiva) — redução fixa enquanto ativa
        var peleMk = GetComponent<PeleDePedraMarker>();
        if (peleMk != null) remaining *= (1f - peleMk.reducao);
```

- [ ] **Step 4: Compilar e verificar**

`refresh_unity` + `read_console`. Esperado 0 erros.

- [ ] **Step 5: Verificar lógica em edit-mode**

`mcp__unity-mcp__execute_code` (corpo de método):

```csharp
var go = new GameObject("__TPlayer");
var ps = go.AddComponent<PlayerStats>();
ps.maxHealth = 100f; ps.health = 100f; ps.defense = 0f; ps.invulneravel = false;
var pele = go.AddComponent<PeleDePedraMarker>(); pele.Renovar(10f, 0.5f);
ps.TakeDamage(40f);
float hp = ps.health;
UnityEngine.Object.DestroyImmediate(go);
return "health apos 40 dano com 50% reducao (esperado ~80): " + hp;
```

Esperado: `~80` (40 → 20 de dano).

- [ ] **Step 6: Commit**

```bash
git add horda/Assets/scripts/Elements/DefensiveMarkers.cs horda/Assets/scripts/player_stats.cs
git commit -m "feat(infusao-def): markers de buff + hooks no TakeDamage"
```

---

## Task 3: `SkillElementEffect.AplicarDefensivo`

**Files:**
- Modify: `horda/Assets/scripts/Elements/SkillElementEffect.cs`

> Reusa helpers privados já existentes na classe: `AplicarSlow(ic,dur,fator)`, `AplicarCC(ic,dur,tipo)`, `AplicarDoT(ic,danoTick,numTicks,intervalo)`, `AplicarKnockbackCoroutine(alvo,forca)`, `AplicarCadeia(alvo,raio,dano,maxAlvos)`, `AplicarMaldicao(ic,dur,reducao)`, `AplicarCegamento(ic,dur,reducao)`, `FindPlayer()`.

- [ ] **Step 1: Adicionar AplicarDefensivo + switch**

Acrescentar dentro de `public static class SkillElementEffect` (antes do `}` final da classe, depois de `GetMultiplicadorSagrado`):

```csharp
    // ── Infusão DEFENSIVA ──────────────────────────────────────────────────────
    // Chamado pela skill defensiva no gatilho. A característica ativa decide se roda
    // (se o gatilho dela não bate, ignora). 'atacante' pode ser null (ex.: OnAtivar).
    public static void AplicarDefensivo(SkillData skill, PlayerStats player,
        DefensiveTrigger gatilho, GameObject atacante, MonoBehaviour caller)
    {
        if (skill == null || player == null || caller == null) return;
        if (skill.appliedElement == ElementType.None || skill.appliedCharacteristicIndex < 0) return;

        var def = ElementRegistry.Instance?.Get(skill.appliedElement);
        if (def == null || def.caracteristicasDefensivas == null) return;
        if (skill.appliedCharacteristicIndex >= def.caracteristicasDefensivas.Length) return;

        var car = def.caracteristicasDefensivas[skill.appliedCharacteristicIndex];
        if (car == null || car.gatilho != gatilho) return;

        AplicarCaracteristicaDefensiva(car, player, atacante, caller);
    }

    static void AplicarCaracteristicaDefensiva(DefensiveCharacteristic car,
        PlayerStats player, GameObject atacante, MonoBehaviour caller)
    {
        InimigoController icAtacante = atacante != null
            ? (atacante.GetComponent<InimigoController>() ?? atacante.GetComponentInParent<InimigoController>())
            : null;
        Vector2 pos = player.transform.position;

        switch (car.tipo)
        {
            case DefensiveCharacteristicType.AuraIgnea: // queima inimigos próximos (tick)
                foreach (var ic in InimigosNoRaio(pos, car.valor1 > 0 ? car.valor1 : 2.5f))
                    ic.ReceberDano(car.valor2 > 0 ? car.valor2 : 4f, false);
                break;

            case DefensiveCharacteristicType.RetaliacaoChamas: // DoT no atacante
                if (icAtacante != null)
                    caller.StartCoroutine(AplicarDoT(icAtacante, car.valor1 > 0 ? car.valor1 : 5f,
                        car.valor2 > 0 ? car.valor2 : 3f, 1f));
                break;

            case DefensiveCharacteristicType.EsquivaVentosa: // buff de esquiva
                RenovarEsquiva(player, car.valor1 > 0 ? car.valor1 : 0.25f, car.valor2 > 0 ? car.valor2 : 6f);
                break;

            case DefensiveCharacteristicType.SoproRepulsor: // empurra próximos
                foreach (var ic in InimigosNoRaio(pos, car.valor1 > 0 ? car.valor1 : 3f))
                    caller.StartCoroutine(AplicarKnockbackCoroutine(ic.gameObject, car.valor2 > 0 ? car.valor2 : 12f));
                break;

            case DefensiveCharacteristicType.PeleDePedra: // redução de dano
                RenovarPeleDePedra(player, car.valor1 > 0 ? car.valor1 : 0.30f, car.valor2 > 0 ? car.valor2 : 6f);
                break;

            case DefensiveCharacteristicType.FundacaoFirme: // imune CC/knockback
                RenovarFundacaoFirme(player, car.valor1 > 0 ? car.valor1 : 6f);
                break;

            case DefensiveCharacteristicType.MareRestauradora: // cura % ao ativar
                player.Heal(player.maxHealth * (car.valor1 > 0 ? car.valor1 : 0.20f));
                break;

            case DefensiveCharacteristicType.FluxoVital: // regen tick
                player.Heal(car.valor1 > 0 ? car.valor1 : 3f);
                break;

            case DefensiveCharacteristicType.DescargaReativa: // atordoa atacante
                if (icAtacante != null && Random.value <= (car.valor2 > 0 ? car.valor2 : 0.5f))
                    caller.StartCoroutine(AplicarCC(icAtacante, car.valor1 > 0 ? car.valor1 : 1f, "stun"));
                break;

            case DefensiveCharacteristicType.CorrenteReflexiva: // reflete em cadeia
                if (atacante != null)
                    AplicarCadeia(atacante, car.valor1 > 0 ? car.valor1 : 4f,
                        car.valor2 > 0 ? car.valor2 : 10f, 3);
                break;

            case DefensiveCharacteristicType.ArmaduraGelida: // escudo extra
                player.shieldPoints += car.valor1 > 0 ? car.valor1 : 40f;
                break;

            case DefensiveCharacteristicType.ToqueCongelante: // congela atacante
                if (icAtacante != null)
                    caller.StartCoroutine(AplicarCC(icAtacante, car.valor1 > 0 ? car.valor1 : 2f, "gelo"));
                break;

            case DefensiveCharacteristicType.Espinhos: // reflete dano de contato
                if (icAtacante != null)
                    icAtacante.ReceberDano(car.valor1 > 0 ? car.valor1 : 12f, false);
                break;

            case DefensiveCharacteristicType.RaizesProtetoras: // enraíza próximos
                foreach (var ic in InimigosNoRaio(pos, car.valor1 > 0 ? car.valor1 : 3f))
                    caller.StartCoroutine(AplicarCC(ic, car.valor2 > 0 ? car.valor2 : 2.5f, "raiz"));
                break;

            case DefensiveCharacteristicType.DrenagemSombria: // cura ao bloquear
                AplicarCura(car.valor1 > 0 ? car.valor1 : 8f);
                break;

            case DefensiveCharacteristicType.MantoAmaldicoado: // reduz defesa próximos
                foreach (var ic in InimigosNoRaio(pos, car.valor1 > 0 ? car.valor1 : 3.5f))
                    caller.StartCoroutine(AplicarMaldicao(ic, 1.5f, car.valor2 > 0 ? car.valor2 : 0.3f));
                break;

            case DefensiveCharacteristicType.BencaoSagrada: // regen + escudo
                player.Heal(car.valor1 > 0 ? car.valor1 : 15f);
                player.shieldPoints += car.valor2 > 0 ? car.valor2 : 25f;
                break;

            case DefensiveCharacteristicType.LuzOfuscante: // cega próximos
                foreach (var ic in InimigosNoRaio(pos, car.valor1 > 0 ? car.valor1 : 3f))
                    caller.StartCoroutine(AplicarCegamento(ic, 1.5f, car.valor2 > 0 ? car.valor2 : 0.4f));
                break;

            case DefensiveCharacteristicType.CaosDefensivo: // efeito defensivo aleatório
                int r = Random.Range(0, 3);
                if (r == 0) player.Heal(player.maxHealth * 0.15f);
                else if (r == 1) player.shieldPoints += 35f;
                else RenovarPeleDePedra(player, 0.4f, 4f);
                break;

            case DefensiveCharacteristicType.PragaReativa: // espalha infecção ao redor
                AplicarCadeia(player.gameObject, car.valor1 > 0 ? car.valor1 : 3.5f,
                    car.valor2 > 0 ? car.valor2 : 8f, 3);
                break;
        }
    }

    static System.Collections.Generic.List<InimigoController> InimigosNoRaio(Vector2 centro, float raio)
    {
        var lista = new System.Collections.Generic.List<InimigoController>();
        foreach (var col in Physics2D.OverlapCircleAll(centro, raio))
        {
            var ic = col.GetComponent<InimigoController>() ?? col.GetComponentInParent<InimigoController>();
            if (ic != null && !ic.estaMorrendo && !lista.Contains(ic)) lista.Add(ic);
        }
        return lista;
    }

    static void RenovarPeleDePedra(PlayerStats p, float red, float dur)
    {
        var m = p.GetComponent<PeleDePedraMarker>() ?? p.gameObject.AddComponent<PeleDePedraMarker>();
        m.Renovar(dur, red);
    }
    static void RenovarFundacaoFirme(PlayerStats p, float dur)
    {
        var m = p.GetComponent<FundacaoFirmeMarker>() ?? p.gameObject.AddComponent<FundacaoFirmeMarker>();
        m.Renovar(dur);
    }
    static void RenovarEsquiva(PlayerStats p, float ch, float dur)
    {
        var m = p.GetComponent<EsquivaMarker>() ?? p.gameObject.AddComponent<EsquivaMarker>();
        m.Renovar(dur, ch);
    }
```

- [ ] **Step 2: Compilar e verificar**

`refresh_unity` + `read_console`. Esperado 0 erros.

- [ ] **Step 3: Verificar gating de gatilho em edit-mode**

Pré-condição: registro precisa ter dado defensivo (Task 4). Se rodar antes da Task 4, o teste retorna "sem efeito" — adiar este step para depois da Task 4. Caso já feita:

```csharp
// Confirma que gatilho incompatível é ignorado e compatível aplica cura.
var go = new GameObject("__TP"); var ps = go.AddComponent<PlayerStats>();
ps.maxHealth = 100f; ps.health = 50f;
var skills = UnityEditor.AssetDatabase.FindAssets("t:SkillData");
SkillData def = null;
foreach (var g in skills) { var s = UnityEditor.AssetDatabase.LoadAssetAtPath<SkillData>(UnityEditor.AssetDatabase.GUIDToAssetPath(g)); if (s != null && !s.EhSkillDeAtaque()) { def = s; break; } }
string r = "sem skill defensiva";
if (def != null) {
    var orig = def.appliedElement; var origIdx = def.appliedCharacteristicIndex;
    def.appliedElement = ElementType.Agua; def.appliedCharacteristicIndex = 0; // Maré Restauradora = OnAtivar
    SkillElementEffect.AplicarDefensivo(def, ps, DefensiveTrigger.OnAtingido, null, ps); // gatilho errado
    float hpErrado = ps.health;
    SkillElementEffect.AplicarDefensivo(def, ps, DefensiveTrigger.OnAtivar, null, ps);   // gatilho certo
    float hpCerto = ps.health;
    def.appliedElement = orig; def.appliedCharacteristicIndex = origIdx;
    r = "gatilho errado (esperado 50): " + hpErrado + " | gatilho certo (esperado >50): " + hpCerto;
}
UnityEngine.Object.DestroyImmediate(go);
return r;
```

Esperado: errado=50, certo>50.

- [ ] **Step 4: Commit**

```bash
git add horda/Assets/scripts/Elements/SkillElementEffect.cs
git commit -m "feat(infusao-def): AplicarDefensivo com as 20 caracteristicas"
```

---

## Task 4: Popular ElementRegistry com as 20 defensivas (editor tool)

**Files:**
- Create: `horda/Assets/scripts/Editor/PopularCaracteristicasDefensivas.cs`

- [ ] **Step 1: Criar o editor tool**

```csharp
using UnityEngine;
using UnityEditor;

public static class PopularCaracteristicasDefensivas
{
    [MenuItem("Tools/Elementos/Popular Caracteristicas Defensivas")]
    public static void Popular()
    {
        var reg = Resources.Load<ElementRegistry>("Elements/ElementRegistry");
        if (reg == null) { Debug.LogError("ElementRegistry não encontrado em Resources/Elements/ElementRegistry"); return; }

        Set(reg, ElementType.Fogo,
            D("Aura Ígnea", DefensiveCharacteristicType.AuraIgnea, DefensiveTrigger.AuraContinua, 2.5f, 4f),
            D("Retaliação em Chamas", DefensiveCharacteristicType.RetaliacaoChamas, DefensiveTrigger.OnAtingido, 5f, 3f));
        Set(reg, ElementType.Ar,
            D("Esquiva Ventosa", DefensiveCharacteristicType.EsquivaVentosa, DefensiveTrigger.OnAtivar, 0.25f, 6f),
            D("Sopro Repulsor", DefensiveCharacteristicType.SoproRepulsor, DefensiveTrigger.OnAtivar, 3f, 12f));
        Set(reg, ElementType.Terra,
            D("Pele de Pedra", DefensiveCharacteristicType.PeleDePedra, DefensiveTrigger.OnAtivar, 0.30f, 6f),
            D("Fundação Firme", DefensiveCharacteristicType.FundacaoFirme, DefensiveTrigger.OnAtivar, 6f, 0f));
        Set(reg, ElementType.Agua,
            D("Maré Restauradora", DefensiveCharacteristicType.MareRestauradora, DefensiveTrigger.OnAtivar, 0.20f, 0f),
            D("Fluxo Vital", DefensiveCharacteristicType.FluxoVital, DefensiveTrigger.AuraContinua, 3f, 0f));
        Set(reg, ElementType.Raio,
            D("Descarga Reativa", DefensiveCharacteristicType.DescargaReativa, DefensiveTrigger.OnAtingido, 1f, 0.5f),
            D("Corrente Reflexiva", DefensiveCharacteristicType.CorrenteReflexiva, DefensiveTrigger.OnAtingido, 4f, 10f));
        Set(reg, ElementType.Gelo,
            D("Armadura Gélida", DefensiveCharacteristicType.ArmaduraGelida, DefensiveTrigger.OnAtivar, 40f, 0f),
            D("Toque Congelante", DefensiveCharacteristicType.ToqueCongelante, DefensiveTrigger.OnAtingido, 2f, 0f));
        Set(reg, ElementType.Planta,
            D("Espinhos", DefensiveCharacteristicType.Espinhos, DefensiveTrigger.OnAtingido, 12f, 0f),
            D("Raízes Protetoras", DefensiveCharacteristicType.RaizesProtetoras, DefensiveTrigger.OnAtivar, 3f, 2.5f));
        Set(reg, ElementType.Trevas,
            D("Drenagem Sombria", DefensiveCharacteristicType.DrenagemSombria, DefensiveTrigger.OnAtingido, 8f, 0f),
            D("Manto Amaldiçoado", DefensiveCharacteristicType.MantoAmaldicoado, DefensiveTrigger.AuraContinua, 3.5f, 0.3f));
        Set(reg, ElementType.Luz,
            D("Bênção Sagrada", DefensiveCharacteristicType.BencaoSagrada, DefensiveTrigger.OnAtivar, 15f, 25f),
            D("Luz Ofuscante", DefensiveCharacteristicType.LuzOfuscante, DefensiveTrigger.AuraContinua, 3f, 0.4f));
        Set(reg, ElementType.Corrompido,
            D("Caos Defensivo", DefensiveCharacteristicType.CaosDefensivo, DefensiveTrigger.OnAtivar, 0f, 0f),
            D("Praga Reativa", DefensiveCharacteristicType.PragaReativa, DefensiveTrigger.OnAtingido, 3.5f, 8f));

        EditorUtility.SetDirty(reg);
        AssetDatabase.SaveAssets();
        Debug.Log("Caracteristicas defensivas populadas no ElementRegistry.");
    }

    static DefensiveCharacteristic D(string nome, DefensiveCharacteristicType tipo, DefensiveTrigger g, float v1, float v2)
        => new DefensiveCharacteristic { nome = nome, descricao = nome, tipo = tipo, gatilho = g, valor1 = v1, valor2 = v2 };

    static void Set(ElementRegistry reg, ElementType el, DefensiveCharacteristic a, DefensiveCharacteristic b)
    {
        var def = reg.Get(el);
        if (def == null) { Debug.LogWarning("ElementDefinition ausente: " + el); return; }
        def.caracteristicasDefensivas = new[] { a, b };
    }
}
```

> Confirmar o caminho do asset: o registro é carregado via `Resources.Load<ElementRegistry>("Elements/ElementRegistry")` (ver `ElementRegistry.cs`). Se o nome diferir, ajustar a string no `Load`.

- [ ] **Step 2: Compilar**

`refresh_unity` + `read_console`. Esperado 0 erros.

- [ ] **Step 3: Rodar o tool**

`mcp__unity-mcp__execute_menu_item` com `menu_path` = `Tools/Elementos/Popular Caracteristicas Defensivas`. Depois `read_console` (esperado log "populadas").

- [ ] **Step 4: Verificar em edit-mode**

```csharp
var reg = Resources.Load<ElementRegistry>("Elements/ElementRegistry");
var def = reg != null ? reg.Get(ElementType.Agua) : null;
if (def == null || def.caracteristicasDefensivas == null) return "FALHOU";
var c = def.caracteristicasDefensivas[0];
return "Agua[0]=" + (c != null ? c.tipo + "/" + c.gatilho : "null") + " | total=" + def.caracteristicasDefensivas.Length;
```

Esperado: `Agua[0]=MareRestauradora/OnAtivar | total=2`.

- [ ] **Step 5: Commit**

```bash
git add horda/Assets/scripts/Editor/PopularCaracteristicasDefensivas.cs horda/Assets/Resources/Elements/ElementRegistry.asset
git commit -m "feat(infusao-def): popular ElementRegistry com 20 caracteristicas defensivas"
```

---

## Task 5: UI Etapa2 mostra o par defensivo

**Files:**
- Modify: `horda/Assets/scripts/UI/ElementApplicationUI.cs` (`MostrarEtapa2`, `CriarCardPoder`)

- [ ] **Step 1: Em MostrarEtapa2, escolher o conjunto pelo tipo da skill**

Substituir o bloco que itera `def.caracteristicas` (linhas ~221-229) por:

```csharp
        bool ehDefensiva = skillSelecionada != null && !skillSelecionada.EhSkillDeAtaque();
        if (ehDefensiva && def?.caracteristicasDefensivas != null)
        {
            for (int i = 0; i < Mathf.Min(def.caracteristicasDefensivas.Length, 2); i++)
            {
                var car = def.caracteristicasDefensivas[i];
                if (car == null) continue;
                CriarCardPoderDefensivo(container, car, i, corElem);
            }
        }
        else if (def?.caracteristicas != null)
        {
            for (int i = 0; i < Mathf.Min(def.caracteristicas.Length, 2); i++)
            {
                var car = def.caracteristicas[i];
                if (car == null) continue;
                CriarCardPoder(container, car, i, corElem);
            }
        }
```

- [ ] **Step 2: Adicionar CriarCardPoderDefensivo**

Cópia de `CriarCardPoder` que usa nome/descrição via Loc keys defensivas. Adicionar após `CriarCardPoder`:

```csharp
    void CriarCardPoderDefensivo(GameObject container, DefensiveCharacteristic car, int idx, Color corElem)
    {
        string nome = Loc.T($"defchar.name.{car.tipo.ToString().ToLower()}");
        string desc = Loc.T($"defchar.desc.{car.tipo.ToString().ToLower()}");
        CriarCardGenerico(container, nome, desc, idx, corElem);
    }
```

E refatorar o miolo visual de `CriarCardPoder` para um `CriarCardGenerico(container, nomeStr, descStr, idx, corElem)` que recebe as strings já resolvidas (mover todo o corpo de `CriarCardPoder` para lá, trocando `Loc.T($"characteristic.name...")`/`desc` pelos parâmetros `nomeStr`/`descStr`; o ícone fica fallback letra A/B). `CriarCardPoder` passa a chamar `CriarCardGenerico(container, Loc.T($"characteristic.name.{car.tipo...}"), Loc.T($"characteristic.desc.{car.tipo...}"), idx, corElem)`.

> `ConfirmarEscolha(indice)` **não muda** — grava `appliedElement` + `appliedCharacteristicIndex`.

- [ ] **Step 3: Compilar**

`refresh_unity` + `read_console`. Esperado 0 erros (textos vêm como chave crua até a Task 9 — ok).

- [ ] **Step 4: Verificar em Play mode (manual)**

Entrar em Play, coletar um elemento, infundir numa skill **defensiva** (ex.: Barreira de Energia) → a Etapa2 deve mostrar 2 cards defensivos (mesmo que com texto = chave crua por enquanto). Infundir numa de **ataque** → mostra o par ofensivo de sempre.

- [ ] **Step 5: Commit**

```bash
git add horda/Assets/scripts/UI/ElementApplicationUI.cs
git commit -m "feat(infusao-def): UI mostra par defensivo para skill defensiva"
```

---

## Task 6: Hooks OnAtivar nas behaviors

**Files (Modify):** os behaviors em `horda/Assets/scripts/bases_skills/`

Em cada ponto de ativação, chamar `SkillElementEffect.AplicarDefensivo(skillData, playerStats, DefensiveTrigger.OnAtivar, null, this);`.

- [ ] **Step 1: Inserir as chamadas OnAtivar**

| Arquivo | Local |
|---|---|
| `AureolaSkillBehavior.cs` | início de `IEnumerator CorotinaAtiva()` (após `ativo = true;`) |
| `BarreiraEnergiaSkillBehavior.cs` | fim de `void AtivarEscudo()` |
| `BarreiraReflexivaSkillBehavior.cs` | início de `IEnumerator CorotinaAtiva()` (após `ativo = true;`) |
| `EspelhoMagicoSkillBehavior.cs` | início de `IEnumerator CorotinaEspelho()` (após `ativoAgora = true;`) |
| `EscudoKarmaSkillBehavior.cs` | fim de `void Ativar()` |
| `TeiaProtecaoSkillBehavior.cs` | início de `IEnumerator CorotinaAtiva()` (após `ativa = true;`) |
| `InstintoSobrevivenciaSkillBehavior.cs` | dentro de `IEnumerator Ativar()` (após `ativo = true;`) — **nome real: `IEnumerator Ativar()`** |
| `SegundaChanceSkillBehavior.cs` | em `TentarReviver()`, após `emRecarga = true;` |
| `FugaSombrasSkillBehavior.cs` | início de `IEnumerator Teleportar()` |

Exemplo (Auréola):

```csharp
        ativo = true;
        regenAcum = 0f;
        SkillElementEffect.AplicarDefensivo(skillData, playerStats, DefensiveTrigger.OnAtivar, null, this);
```

> Todas as behaviors têm `skillData` (campo protegido em `SkillBehavior`) e `playerStats`. `EscudoEspinhoso` e `EscudoRotativo` setam `this.skillData` em `UpdateFromSkillData` (feito em sessão anterior). Conferir que cada behavior tem `skillData` não-nulo no ponto do hook; se algum não setar, adicionar `this.skillData = data;` no respectivo `ConfigurarDeSkillData`/`UpdateFromSkillData`.

- [ ] **Step 2: Compilar**

`refresh_unity` + `read_console`. Esperado 0 erros.

- [ ] **Step 3: Verificar (Play mode manual)**

Infundir **Água/Maré Restauradora** (OnAtivar, cura 20%) numa defensiva com ativação por dano (ex.: Auréola/Barreira Reflexiva), tomar dano para ativar, confirmar cura. Console deve mostrar a aplicação (adicionar `Debug.Log` temporário se necessário).

- [ ] **Step 4: Commit**

```bash
git add horda/Assets/scripts/bases_skills/*.cs
git commit -m "feat(infusao-def): hooks OnAtivar nas defensivas"
```

---

## Task 7: Hooks OnAtingido nas behaviors

**Files (Modify):** behaviors de reflexão/contato.

Passar o **atacante** (GameObject) quando conhecido.

- [ ] **Step 1: Inserir chamadas OnAtingido**

| Arquivo | Local | Atacante |
|---|---|---|
| `BarreiraReflexivaSkillBehavior.cs` | em `AplicarReflexao(dano)`, dentro do `if (atacante != null)` | `atacante.gameObject` |
| `EspelhoMagicoSkillBehavior.cs` | em `TentarRefletir(dano)`, dentro do `if (alvo != null)` | `alvo.gameObject` |
| `EscudoKarmaSkillBehavior.cs` | em `AbsorverHit(dano)`, após `hitsRestantes--;` (atacante = mais próximo, achado no bloco de KarmaRetribuicao — extrair antes) | `alvo?.gameObject` |
| `EscudoEspinhosoSkillBehavior.cs` | em `FixedUpdate`, após `ic.ReceberDano(dano, false);` | `ic.gameObject` |

Exemplo (Barreira Reflexiva, dentro do `if (atacante != null)`):

```csharp
            SkillElementEffect.AplicarDefensivo(skillData, playerStats, DefensiveTrigger.OnAtingido, atacante.gameObject, this);
```

Para Karma, localizar o atacante mais próximo uma vez no topo de `AbsorverHit` (reusar a busca que já existe no bloco `KarmaRetribuicao`) e usar em ambos.

- [ ] **Step 2: Compilar**

`refresh_unity` + `read_console`. Esperado 0 erros.

- [ ] **Step 3: Verificar (Play mode manual)**

Infundir **Planta/Espinhos** (OnAtingido) na Barreira Reflexiva/Escudo Espinhoso e confirmar que o inimigo que ataca/toca leva o dano refletido.

- [ ] **Step 4: Commit**

```bash
git add horda/Assets/scripts/bases_skills/*.cs
git commit -m "feat(infusao-def): hooks OnAtingido nas defensivas de reflexao/contato"
```

---

## Task 8: Hooks AuraContinua nas behaviors

**Files (Modify):** behaviors com tick/aura.

Throttle: chamar no máximo a cada ~0.5s (acumular um timer local no behavior para não chamar todo frame).

- [ ] **Step 1: Inserir tick AuraContinua**

| Arquivo | Local |
|---|---|
| `CampoEspinhosSkillBehavior.cs` | dentro do tick de dano (`if (timer <= 0f)` em `Update`, junto de `DanificarInimigos()`) |
| `EscudoEspinhosoSkillBehavior.cs` | em `Update`, com timer local de 0.5s |
| `AureolaSkillBehavior.cs` | no bloco `if (ativo ...)` do `Update`, com timer local de 0.5s |
| `BarreiraReflexivaSkillBehavior.cs` | em `Update` quando `ativo`, com timer local de 0.5s |

Exemplo (timer local — declarar `float auraTick;` no behavior, e no `Update`):

```csharp
        auraTick -= Time.deltaTime;
        if (auraTick <= 0f)
        {
            auraTick = 0.5f;
            SkillElementEffect.AplicarDefensivo(skillData, playerStats, DefensiveTrigger.AuraContinua, null, this);
        }
```

> Em Campo de Espinhos, que já tem o tick por `intervalo`, chamar junto de `DanificarInimigos()` (sem timer extra).

- [ ] **Step 2: Compilar**

`refresh_unity` + `read_console`. Esperado 0 erros.

- [ ] **Step 3: Verificar (Play mode manual)**

Infundir **Fogo/Aura Ígnea** (AuraContinua) no Campo de Espinhos/Auréola e confirmar que inimigos próximos tomam dano contínuo. Infundir **Água/Fluxo Vital** e confirmar regen contínuo do player enquanto ativo.

- [ ] **Step 4: Commit**

```bash
git add horda/Assets/scripts/bases_skills/*.cs
git commit -m "feat(infusao-def): hooks AuraContinua nas defensivas"
```

---

## Task 9: Localização (defchar.name/desc × 14 idiomas)

**Files:**
- Modify: `horda/Assets/Resources/Localization/GameStrings.asset`

- [ ] **Step 1: Adicionar as 40 chaves**

Para cada um dos 20 `DefensiveCharacteristicType` (lowercase), adicionar `defchar.name.{tipo}` e `defchar.desc.{tipo}` com as 14 traduções (ordem: PT_BR EN ES DE FR IT RU PL TR ZH JA KO NL ID), seguindo o mesmo estilo de escape do arquivo (`\xNN` / `\uXXXX` ou UTF-8 — o tool de Edit normaliza). Usar os nomes da tabela da spec como base PT e traduzir.

Exemplo (auraignea):

```yaml
  - key: defchar.name.auraignea
    translations:
    - Aura Ígnea
    - Igneous Aura
    - Aura Ígnea
    - Feueraura
    - Aura Ignée
    - Aura Ignea
    - "Огненная аура"
    - Ognista Aura
    - Ateş Aurası
    - "炎のオーラ"
    - "炎のオーラ"
    - "화염 오라"
    - Vuuraura
    - Aura Api
  - key: defchar.desc.auraignea
    translations:
    - Queima inimigos próximos enquanto ativa.
    - Burns nearby enemies while active.
    - ... (14)
```

(Inserir o bloco completo das 40 chaves antes do final do array `entries`.)

- [ ] **Step 2: Verificar (Play mode manual)**

Infundir uma defensiva e confirmar que os 2 cards mostram nome/descrição traduzidos (não a chave crua). Trocar idioma e reconferir.

- [ ] **Step 3: Commit**

```bash
git add horda/Assets/Resources/Localization/GameStrings.asset
git commit -m "feat(infusao-def): localizacao das caracteristicas defensivas"
```

---

## Task 10: Integração final + merge

- [ ] **Step 1: Smoke test em Play mode**

Para 3 elementos cobrindo os 3 gatilhos (ex.: Água/Maré=OnAtivar, Planta/Espinhos=OnAtingido, Fogo/AuraIgnea=AuraContinua): infundir em defensivas adequadas e confirmar efeito + UI + texto.

- [ ] **Step 2: Compilar limpo + console sem erros**

`refresh_unity` + `read_console` (types=["error","warning"]). Resolver erros (warnings ok).

- [ ] **Step 3: Finalizar branch**

Usar `superpowers:finishing-a-development-branch` para decidir merge na `main` + push (com aprovação do usuário).

---

## Questões em aberto (decidir durante execução)

1. **Pele de Pedra + defesa**: 30% pode ficar forte com a defesa base; tunar `valor1` no editor tool.
2. **FundacaoFirme**: o `AplicarCC`/knockback do `SkillElementEffect` e do jogo precisam checar `FundacaoFirmeMarker` para realmente imunizar — hoje o marker é aplicado mas **nenhum consumidor o lê**. Decidir se entra agora (adicionar checagem nos pontos de CC) ou fica como no-op v1. (Sinalizado: efeito sem consumidor.)
3. **OnAtingido sem atacante** em defensivas que não localizam atacante: características como Espinhos/Retaliação só funcionam onde há atacante (reflexão/contato). Em defensivas de pura ativação, o par defensivo escolhido pode não ter alvo — aceitável (jogador escolhe), mas considerar restringir/avisar na UI no futuro.
