using System.Collections.Generic;
using UnityEngine;

// Co-op: adiciona behaviors de skill em modo COSMÉTICO no fantoche do colega (geram o
// visual procedural, mas sem dano). Espelha o mapa specificType→behavior do SkillManager.
// IMPORTANTE: só entram aqui os tipos cujo dano JÁ está gateado por `cosmetico` (senão
// daria dano em dobro). Conforme cada skill é gateada, adiciona o tipo no mapa abaixo.
public static class SkillFxCosmetico
{
    static readonly Dictionary<SpecificSkillType, System.Type> Suportados =
        new Dictionary<SpecificSkillType, System.Type>
    {
        { SpecificSkillType.CristaisGelo,       typeof(CristaisGeloSkillBehavior) },
        { SpecificSkillType.LancaLuz,           typeof(LancaLuzSkillBehavior) },
        { SpecificSkillType.ChicoteEnergia,     typeof(ChicoteEnergiaSkillBehavior) },
        { SpecificSkillType.ChuvaEstrelas,      typeof(ChuvaEstrelasSkillBehavior) },
        { SpecificSkillType.MisseisTeleguiados, typeof(MisseisTeleguiadosSkillBehavior) },
        { SpecificSkillType.EspadaFantasma,     typeof(EspadaFantasmaSkillBehavior) },
        { SpecificSkillType.FuriaLaminas,       typeof(FuriaLaminasSkillBehavior) },
        { SpecificSkillType.GarrasAbismo,       typeof(GarrasAbismoSkillBehavior) },
        { SpecificSkillType.CampoEspinhos,      typeof(CampoEspinhosSkillBehavior) },
        { SpecificSkillType.PulsoRitmico,       typeof(PulsoRitmicoSkillBehavior) },
        { SpecificSkillType.CorrenteSombria,    typeof(CorrenteSombriaSkillBehavior) },
        { SpecificSkillType.SombrasCruz,        typeof(SombrasCruzSkillBehavior) },
        { SpecificSkillType.CorteFantasma,      typeof(CorteFantasmaSkillBehavior) },
        { SpecificSkillType.EscudoEspinhoso,    typeof(EscudoEspinhosoSkillBehavior) },
        { SpecificSkillType.EscudoKarma,        typeof(EscudoKarmaSkillBehavior) },
        { SpecificSkillType.EspelhoMagico,      typeof(EspelhoMagicoSkillBehavior) },
        { SpecificSkillType.BarreiraEnergia,    typeof(BarreiraEnergiaSkillBehavior) },
        { SpecificSkillType.BarreiraReflexiva,  typeof(BarreiraReflexivaSkillBehavior) },
        { SpecificSkillType.Aureola,            typeof(AureolaSkillBehavior) },
        // Defensivas com gatilho por evento: a behavior cosmética é adicionada no equip,
        // mas NÃO auto-dispara (guarda por `cosmetico`). O visual roda via broadcast do dono
        // (PlayerNet.SincronizarDefensiva → IDefensivaCosmetico.ExecutarCosmetico).
        { SpecificSkillType.TeiaProtecao,          typeof(TeiaProtecaoSkillBehavior) },
        { SpecificSkillType.FugaSombras,           typeof(FugaSombrasSkillBehavior) },
        { SpecificSkillType.InstintoSobrevivencia, typeof(InstintoSobrevivenciaSkillBehavior) },
        { SpecificSkillType.SegundaChance,         typeof(SegundaChanceSkillBehavior) },
        // Shield (ShieldAura) é prefab-based — replicado por caminho próprio no PlayerNet.
    };

    public static bool EhSuportado(SpecificSkillType t) => Suportados.ContainsKey(t);

    // Lista dos tipos suportados (pro botão de debug listar automaticamente).
    public static IEnumerable<SpecificSkillType> Tipos => Suportados.Keys;

    // Adiciona a versão cosmética da skill no fantoche. `elemento` = appliedElement do DONO
    // (a infusão). Usa um CLONE do SkillData pra a cor não vir do asset compartilhado (que
    // reflete a infusão do COLEGA). Retorna a behavior criada (pro PlayerNet rastrear e
    // atualizar a cor quando o dono infundir depois).
    public static SkillBehavior Adicionar(PlayerStats alvo, SkillData skill, int elemento)
    {
        if (alvo == null || skill == null) return null;
        System.Type tipo;
        if (!Suportados.TryGetValue(skill.specificType, out tipo)) return null;

        var clone = Object.Instantiate(skill); // cópia runtime, independente do asset
        clone.appliedElement = (ElementType)elemento;

        var comp = alvo.gameObject.AddComponent(tipo) as SkillBehavior;
        if (comp == null) return null;

        comp.cosmetico = true;
        comp.skillData = clone;

        // Config opcional: muitas behaviors têm ConfigurarDeSkillData(SkillData).
        var m = tipo.GetMethod("ConfigurarDeSkillData");
        if (m != null) m.Invoke(comp, new object[] { clone });

        comp.Initialize(alvo);
        return comp;
    }
}
