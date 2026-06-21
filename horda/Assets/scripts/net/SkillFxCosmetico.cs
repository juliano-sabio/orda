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
        // TODO co-op: Teia/Shield/Fuga/SegundaChance/Instinto precisam de trigger-broadcast
        // (disparam em evento de player) ou de prefab (ShieldAura) — fazer com mecanismo próprio.
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
