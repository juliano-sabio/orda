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
        // TODO co-op: adicionar os demais conforme gatear o dano em cada behavior.
    };

    public static bool EhSuportado(SpecificSkillType t) => Suportados.ContainsKey(t);

    // Lista dos tipos suportados (pro botão de debug listar automaticamente).
    public static IEnumerable<SpecificSkillType> Tipos => Suportados.Keys;

    public static void Adicionar(PlayerStats alvo, SkillData skill)
    {
        if (alvo == null || skill == null) return;
        System.Type tipo;
        if (!Suportados.TryGetValue(skill.specificType, out tipo)) return;

        var comp = alvo.gameObject.AddComponent(tipo) as SkillBehavior;
        if (comp == null) return;

        comp.cosmetico = true;
        comp.skillData = skill;

        // Config opcional: muitas behaviors têm ConfigurarDeSkillData(SkillData).
        var m = tipo.GetMethod("ConfigurarDeSkillData");
        if (m != null) m.Invoke(comp, new object[] { skill });

        comp.Initialize(alvo);
    }
}
