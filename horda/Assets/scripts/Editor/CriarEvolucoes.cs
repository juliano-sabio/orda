#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class CriarEvolucoes
{
    const string PASTA = "Assets/Evolucoes";

    [MenuItem("Tools/Evolucoes/Criar Todas as Evolucoes")]
    public static void CriarTodas()
    {
        if (!AssetDatabase.IsValidFolder(PASTA))
        { AssetDatabase.CreateFolder("Assets", "Evolucoes"); AssetDatabase.Refresh(); }

        var lista = new List<SkillEvolutionData>();

        // Campo de Espinhos
        lista.Add(Criar("EspinhosVenenosos",  "Espinhos Venenosos",  "Cada tick de dano aplica veneno por 2s.",              SpecificSkillType.CampoEspinhos, SkillEvolutionType.EspinhosVenenosos, SkillRarity.Rare,      new Color(0.4f,1f,0.4f)));
        lista.Add(Criar("CampoAmpliado",      "Campo Ampliado",      "+50% de raio e +30% de dano.",                         SpecificSkillType.CampoEspinhos, SkillEvolutionType.CampoAmpliado,     SkillRarity.Epic,      new Color(0.2f,1f,0.3f)));

        // Chuva de Estrelas
        lista.Add(Criar("ChuvaIntensa",       "Chuva Intensa",       "+2 estrelas por ativação.",                            SpecificSkillType.ChuvaEstrelas, SkillEvolutionType.ChuvaIntensa,      SkillRarity.Rare,      new Color(1f,0.9f,0.3f)));
        lista.Add(Criar("ImpactoSismico",     "Impacto Sísmico",     "Cada impacto gera onda de choque em área maior.",      SpecificSkillType.ChuvaEstrelas, SkillEvolutionType.ImpactoSismico,    SkillRarity.Epic,      new Color(1f,0.6f,0.1f)));

        // Garras do Abismo
        lista.Add(Criar("GarrasVenenosas",    "Garras Venenosas",    "Aplica veneno ao prender o inimigo.",                  SpecificSkillType.GarrasAbismo,  SkillEvolutionType.GarrasVenenosas,   SkillRarity.Rare,      new Color(0.5f,0.1f,0.8f)));
        lista.Add(Criar("Execucao",           "Execução",            "Inimigos abaixo de 25% HP são eliminados na hora.",    SpecificSkillType.GarrasAbismo,  SkillEvolutionType.Execucao,          SkillRarity.Legendary, new Color(1f,0.2f,0.2f)));

        // Fúria de Lâminas
        lista.Add(Criar("LaminasDuplas",      "Lâminas Duplas",      "Dispara o dobro de lâminas por ativação.",             SpecificSkillType.FuriaLaminas,  SkillEvolutionType.LaminasDuplas,     SkillRarity.Rare,      new Color(0.85f,0.92f,1f)));
        lista.Add(Criar("LaminasExplosivas",  "Lâminas Explosivas",  "Cada lâmina explode em área ao acertar.",              SpecificSkillType.FuriaLaminas,  SkillEvolutionType.LaminasExplosivas, SkillRarity.Epic,      new Color(1f,0.5f,0.1f)));

        // Sombras em Cruz
        lista.Add(Criar("CruzDupla",          "Cruz Dupla",          "Dispara em 8 direções (+ e ×).",                       SpecificSkillType.SombrasCruz,   SkillEvolutionType.CruzDupla,         SkillRarity.Rare,      new Color(0.55f,0.25f,1f)));
        lista.Add(Criar("SombrasPerfurantes", "Sombras Perfurantes", "Alcance aumentado em 100%.",                           SpecificSkillType.SombrasCruz,   SkillEvolutionType.SombrasPerfurantes,SkillRarity.Epic,      new Color(0.4f,0.1f,0.9f)));

        // Corte Fantasma
        lista.Add(Criar("CorteTriple",        "Corte Triplo",        "+1 corte por ativação.",                               SpecificSkillType.CorteFantasma, SkillEvolutionType.CorteTriple,       SkillRarity.Rare,      new Color(0.7f,0.9f,1f)));
        lista.Add(Criar("CorteLetal",         "Corte Letal",         "2 hits no mesmo inimigo causa atordoamento de 1s.",    SpecificSkillType.CorteFantasma, SkillEvolutionType.CorteLetal,        SkillRarity.Epic,      new Color(0.5f,0.8f,1f)));

        // Lança de Luz
        lista.Add(Criar("LancaPerfurante",    "Lança Perfurante",    "Atravessa todos os inimigos no caminho.",              SpecificSkillType.LancaLuz,      SkillEvolutionType.LancaPerfurante,   SkillRarity.Rare,      new Color(1f,0.95f,0.4f)));
        lista.Add(Criar("LancaExplosiva",     "Lança Explosiva",     "Explode em área ao atingir o primeiro inimigo.",       SpecificSkillType.LancaLuz,      SkillEvolutionType.LancaExplosiva,    SkillRarity.Epic,      new Color(1f,0.7f,0.1f)));

        // Chicote de Energia
        lista.Add(Criar("ChicoteEletrico",    "Chicote Elétrico",    "Inimigos atingidos ficam lentos por 1s.",              SpecificSkillType.ChicoteEnergia,SkillEvolutionType.ChicoteEletrico,   SkillRarity.Rare,      new Color(0.2f,0.8f,1f)));
        lista.Add(Criar("DuplaRotacao",       "Dupla Rotação",       "Gira 2x consecutivamente na ativação.",                SpecificSkillType.ChicoteEnergia,SkillEvolutionType.DuplaRotacao,      SkillRarity.Epic,      new Color(0.1f,0.7f,1f)));

        // Mísseis Teleguiados
        lista.Add(Criar("SalvaMisseis",       "Salva de Mísseis",    "+2 mísseis por ativação.",                             SpecificSkillType.MisseisTeleguiados,SkillEvolutionType.SalvaMisseis,  SkillRarity.Rare,      new Color(1f,0.5f,0.1f)));
        lista.Add(Criar("MisseisExplosivos",  "Mísseis Explosivos",  "Cada míssil explode em área ao impactar.",             SpecificSkillType.MisseisTeleguiados,SkillEvolutionType.MisseisExplosivos,SkillRarity.Epic,   new Color(1f,0.3f,0.1f)));

        // Pulso Rítmico
        lista.Add(Criar("PulsoIntenso",       "Pulso Intenso",       "+100% de dano por pulso.",                             SpecificSkillType.PulsoRitmico,  SkillEvolutionType.PulsoIntenso,      SkillRarity.Rare,      new Color(0.3f,0.9f,0.5f)));
        lista.Add(Criar("PulsoCadeia",        "Pulso em Cadeia",     "Inimigos atingidos propagam 50% do dano para vizinhos.",SpecificSkillType.PulsoRitmico, SkillEvolutionType.PulsoCadeia,       SkillRarity.Epic,      new Color(0.2f,1f,0.4f)));

        // Espada Fantasma
        lista.Add(Criar("EspadaDuplaFantasma","Espada Dupla",        "Corta na frente E atrás simultaneamente.",             SpecificSkillType.EspadaFantasma,SkillEvolutionType.EspadaDuplaFantasma,SkillRarity.Rare,     new Color(0.85f,0.85f,1f)));
        lista.Add(Criar("EspadaFlamejante",   "Espada Flamejante",   "Cortes deixam chamas que causam dano por 3s.",         SpecificSkillType.EspadaFantasma,SkillEvolutionType.EspadaFlamejante,  SkillRarity.Epic,      new Color(1f,0.4f,0.1f)));

        // Corrente Sombria
        lista.Add(Criar("CorrenteReforcada",  "Corrente Reforçada",  "+1 alvo e dano dobrado.",                              SpecificSkillType.CorrenteSombria,SkillEvolutionType.CorrenteReforcada, SkillRarity.Rare,     new Color(0.5f,0.15f,0.9f)));
        lista.Add(Criar("CorrenteParalisante","Corrente Paralisante","Paralisa inimigos durante toda a duração.",            SpecificSkillType.CorrenteSombria,SkillEvolutionType.CorrenteParalisante,SkillRarity.Epic,     new Color(0.4f,0.1f,0.8f)));

        // Evoluções de Alcance (melee)
        lista.Add(Criar("GarrasAlcance",    "Garras Expandidas",   "+60% de alcance de detecção.",             SpecificSkillType.GarrasAbismo,   SkillEvolutionType.GarrasAlcance,   SkillRarity.Rare, new Color(0.5f,0.1f,0.8f)));
        lista.Add(Criar("ChicoteAlcance",   "Chicote Longo",       "+50% de raio do chicote.",                 SpecificSkillType.ChicoteEnergia, SkillEvolutionType.ChicoteAlcance,  SkillRarity.Rare, new Color(0.2f,0.8f,1f)));
        lista.Add(Criar("PulsoAlcance",     "Pulso Ampliado",      "+75% de raio do pulso.",                   SpecificSkillType.PulsoRitmico,   SkillEvolutionType.PulsoAlcance,    SkillRarity.Rare, new Color(0.3f,0.9f,0.5f)));
        lista.Add(Criar("EspadaAlcance",    "Lâmina Alongada",     "+50% de alcance dos cortes.",              SpecificSkillType.EspadaFantasma, SkillEvolutionType.EspadaAlcance,   SkillRarity.Rare, new Color(0.85f,0.85f,1f)));
        lista.Add(Criar("CorrenteAlcance",  "Corrente Longa",      "+60% de alcance de detecção.",             SpecificSkillType.CorrenteSombria,SkillEvolutionType.CorrenteAlcance, SkillRarity.Rare, new Color(0.5f,0.15f,0.9f)));

        // Evoluções de Quantidade (projétil)
        lista.Add(Criar("LancasMultiplas",       "Rajada de Lanças",       "Dispara 3 lanças simultaneamente.",                       SpecificSkillType.LancaLuz,              SkillEvolutionType.LancasMultiplas,       SkillRarity.Epic,      new Color(1f,0.95f,0.4f)));

        // Segunda Chance
        lista.Add(Criar("SegundaChanceCura",     "Cura Ampliada",          "Revive com 60% de HP em vez de 30%.",                     SpecificSkillType.SegundaChance,         SkillEvolutionType.SegundaChanceCura,     SkillRarity.Rare,      new Color(1f,0.85f,0.1f)));
        lista.Add(Criar("SegundaChanceInvencivel","Fênix Invencível",       "Ao reviver, fica invulnerável por 3s.",                   SpecificSkillType.SegundaChance,         SkillEvolutionType.SegundaChanceInvencivel,SkillRarity.Epic,    new Color(1f,0.6f,0.1f)));

        // Fuga das Sombras
        lista.Add(Criar("FugaInvulneravel",      "Sombra Intocável",       "Ao teleportar, fica invulnerável por 2s.",                SpecificSkillType.FugaSombras,           SkillEvolutionType.FugaInvulneravel,      SkillRarity.Rare,      new Color(0.6f,0.2f,1f)));
        lista.Add(Criar("FugaCura",              "Fuga Restauradora",      "Ao teleportar, recupera 20% do HP máximo.",               SpecificSkillType.FugaSombras,           SkillEvolutionType.FugaCura,              SkillRarity.Epic,      new Color(0.5f,0.1f,0.9f)));

        // Barreira de Energia
        lista.Add(Criar("BarreiraFortificada",   "Barreira Fortificada",   "+80% de vida do escudo.",                                 SpecificSkillType.BarreiraEnergia,       SkillEvolutionType.BarreiraFortificada,   SkillRarity.Rare,      new Color(0.2f,0.6f,1f)));
        lista.Add(Criar("BarreiraExplosiva",     "Barreira Explosiva",     "Ao quebrar, explode causando dano em área.",              SpecificSkillType.BarreiraEnergia,       SkillEvolutionType.BarreiraExplosiva,     SkillRarity.Epic,      new Color(0.4f,0.8f,1f)));

        // Teia de Proteção
        lista.Add(Criar("TeiaVenenosa",          "Teia Venenosa",          "Inimigos empurrados ficam envenenados por 2s.",           SpecificSkillType.TeiaProtecao,          SkillEvolutionType.TeiaVenenosa,          SkillRarity.Rare,      new Color(0.3f,1f,0.5f)));
        lista.Add(Criar("TeiaPermanente",        "Teia Permanente",        "+100% de duração da teia ativa.",                         SpecificSkillType.TeiaProtecao,          SkillEvolutionType.TeiaPermanente,        SkillRarity.Epic,      new Color(0.2f,0.9f,0.4f)));

        // Instinto de Sobrevivência
        lista.Add(Criar("InstintoFurioso",       "Instinto Furioso",       "+25% de dano enquanto o instinto estiver ativo.",         SpecificSkillType.InstintoSobrevivencia, SkillEvolutionType.InstintoFurioso,       SkillRarity.Rare,      new Color(1f,0.55f,0.1f)));
        lista.Add(Criar("InstintoEspirito",      "Espírito Resiliente",    "Ao ativar, cura 20% do HP máximo imediatamente.",         SpecificSkillType.InstintoSobrevivencia, SkillEvolutionType.InstintoEspirito,      SkillRarity.Epic,      new Color(1f,0.4f,0.05f)));

        // Espelho Mágico
        lista.Add(Criar("EspelhoAmplificado",    "Espelho Amplificado",    "Reflete 150% do dano recebido.",                          SpecificSkillType.EspelhoMagico,         SkillEvolutionType.EspelhoAmplificado,    SkillRarity.Rare,      new Color(0.6f,0.9f,1f)));
        lista.Add(Criar("EspelhoExplosivo",      "Espelho Explosivo",      "Ao refletir, gera explosão em área no atacante.",         SpecificSkillType.EspelhoMagico,         SkillEvolutionType.EspelhoExplosivo,      SkillRarity.Epic,      new Color(0.4f,0.8f,1f)));

        // Escudo de Karma
        lista.Add(Criar("KarmaReforcado",        "Karma Reforçado",        "Absorve 5 hits em vez de 3.",                             SpecificSkillType.EscudoKarma,           SkillEvolutionType.KarmaReforcado,        SkillRarity.Rare,      new Color(1f,0.85f,0.2f)));
        lista.Add(Criar("KarmaRetribuicao",      "Karma Retribuição",      "Ao absorver um hit, causa o dobro do dano no atacante.",  SpecificSkillType.EscudoKarma,           SkillEvolutionType.KarmaRetribuicao,      SkillRarity.Epic,      new Color(1f,0.7f,0.1f)));

        // Escudo Espinhoso
        lista.Add(Criar("EspinhosVenenosos2",  "Espinhos Venenosos",   "Inimigos atingidos ficam envenenados por 2s.",            SpecificSkillType.EscudoEspinhoso,   SkillEvolutionType.EspinhosVenenosos2, SkillRarity.Rare,      new Color(0.3f,1f,0.4f)));
        lista.Add(Criar("EspinhosExplosivos",  "Espinhos Explosivos",  "Ao esgotar os hits, explode em área causando dano.",      SpecificSkillType.EscudoEspinhoso,   SkillEvolutionType.EspinhosExplosivos, SkillRarity.Epic,      new Color(0.2f,1f,0.3f)));

        // Aureola
        lista.Add(Criar("AureolaFortificada",  "Aureola Fortificada",  "+80% de regen e +20% de redução de dano.",                SpecificSkillType.Aureola,           SkillEvolutionType.AureolaFortificada, SkillRarity.Rare,      new Color(1f,0.9f,0.2f)));
        lista.Add(Criar("AureolaExpansiva",    "Aureola Expansiva",    "+60% de raio da aura protetora.",                         SpecificSkillType.Aureola,           SkillEvolutionType.AureolaExpansiva,   SkillRarity.Epic,      new Color(1f,0.85f,0.1f)));

        // Barreira Reflexiva
        lista.Add(Criar("BarreiraTotal",       "Barreira Total",       "Reflexão de 100% do dano e anula o dano recebido.",       SpecificSkillType.BarreiraReflexiva, SkillEvolutionType.BarreiraTotal,      SkillRarity.Rare,      new Color(0.4f,0.85f,1f)));
        lista.Add(Criar("BarreiraCongelante",  "Barreira Congelante",  "Inimigos que tocam a barreira ficam lentos por 1.5s.",     SpecificSkillType.BarreiraReflexiva, SkillEvolutionType.BarreiraCongelante, SkillRarity.Epic,      new Color(0.3f,0.8f,1f)));

        // Cristais de Gelo
        lista.Add(Criar("CristaisDuplos",     "Cristais Duplos",     "5 cristais orbitam em vez de 3.",                         SpecificSkillType.CristaisGelo, SkillEvolutionType.CristaisDuplos,     SkillRarity.Rare,  new Color(0.45f,0.88f,1f)));
        lista.Add(Criar("CristaisExplosivos", "Cristais Explosivos", "Cada tiro de cristal explode em area de gelo ao acertar.", SpecificSkillType.CristaisGelo, SkillEvolutionType.CristaisExplosivos, SkillRarity.Epic,  new Color(0.3f,0.75f,1f)));

        // ── Novas evoluções de ATAQUE ────────────────────────────────────────────
        lista.Add(Criar("ChuvaCongelante",     "Chuva Congelante",     "Estrelas deixam os inimigos atingidos lentos por 2s.",     SpecificSkillType.ChuvaEstrelas, SkillEvolutionType.ChuvaCongelante,     SkillRarity.Rare, new Color(0.5f, 0.85f, 1f)));
        lista.Add(Criar("EspinhosFlamejantes", "Espinhos Flamejantes", "Cada tick incendeia os inimigos (dano contínuo por 3s).",  SpecificSkillType.CampoEspinhos, SkillEvolutionType.EspinhosFlamejantes, SkillRarity.Epic, new Color(1f,   0.5f,  0.1f)));
        lista.Add(Criar("LaminasSangrentas",   "Lâminas Sangrentas",   "As lâminas causam sangramento (dano contínuo por 2.5s).",  SpecificSkillType.FuriaLaminas,  SkillEvolutionType.LaminasSangrentas,   SkillRarity.Rare, new Color(0.9f, 0.2f,  0.3f)));

        // ── Evoluções LENDÁRIAS de assinatura (uma por skill de ataque) ──────────
        lista.Add(Criar("CampoEspinhosLend",   "Vórtice de Espinhos",  "LENDÁRIA: o campo vira um buraco negro e puxa os inimigos pra dentro.",         SpecificSkillType.CampoEspinhos,     SkillEvolutionType.CampoEspinhosLend,   SkillRarity.Legendary, new Color(0.6f, 1f, 0.4f)));
        lista.Add(Criar("ChuvaEstrelasLend",   "Estrela Cadente",      "LENDÁRIA: cai um meteoro GIGANTE no maior aglomerado e deixa uma cratera em chamas.", SpecificSkillType.ChuvaEstrelas, SkillEvolutionType.ChuvaEstrelasLend,   SkillRarity.Legendary, new Color(1f, 0.7f, 0.15f)));
        lista.Add(Criar("FuriaLaminasLend",    "Lâminas Bumerangue",   "LENDÁRIA: as lâminas voltam ao fim do alcance, cortando os inimigos de novo.",  SpecificSkillType.FuriaLaminas,      SkillEvolutionType.FuriaLaminasLend,    SkillRarity.Legendary, new Color(0.9f, 0.95f, 1f)));
        lista.Add(Criar("LancaLuzLend",        "Lança Teleguiada",     "LENDÁRIA: a lança persegue o alvo e relança sozinha pro próximo inimigo.",      SpecificSkillType.LancaLuz,          SkillEvolutionType.LancaLuzLend,        SkillRarity.Legendary, new Color(1f, 0.95f, 0.35f)));
        lista.Add(Criar("SombrasCruzLend",     "Cruz Rotatória",       "LENDÁRIA: os feixes giram 360° ao redor do player, varrendo tudo como um farol.", SpecificSkillType.SombrasCruz,     SkillEvolutionType.SombrasCruzLend,     SkillRarity.Legendary, new Color(0.55f, 0.2f, 1f)));
        lista.Add(Criar("CorteFantasmaLend",   "Marca da Morte",       "LENDÁRIA: cada corte marca o inimigo; ao juntar 3 marcas, ele detona.",         SpecificSkillType.CorteFantasma,     SkillEvolutionType.CorteFantasmaLend,   SkillRarity.Legendary, new Color(0.6f, 0.25f, 1f)));
        lista.Add(Criar("ChicoteEnergiaLend",  "Chicote Condutor",     "LENDÁRIA: deixa um campo elétrico que arqueia e danifica os inimigos próximos.", SpecificSkillType.ChicoteEnergia,   SkillEvolutionType.ChicoteEnergiaLend,  SkillRarity.Legendary, new Color(0.2f, 0.85f, 1f)));
        lista.Add(Criar("MisseisLend",         "Mísseis de Fragmentação","LENDÁRIA: cada míssil se divide em 4 mini-mísseis teleguiados ao impactar.",   SpecificSkillType.MisseisTeleguiados,SkillEvolutionType.MisseisLend,         SkillRarity.Legendary, new Color(1f, 0.45f, 0.1f)));
        lista.Add(Criar("PulsoRitmicoLend",    "Pulso Gravitacional",  "LENDÁRIA: implode os inimigos pro centro e depois os arremessa com força.",     SpecificSkillType.PulsoRitmico,      SkillEvolutionType.PulsoRitmicoLend,    SkillRarity.Legendary, new Color(0.25f, 1f, 0.5f)));
        lista.Add(Criar("EspadaFantasmaLend",  "Espadas Orbitais",     "LENDÁRIA: invoca 3 espadas que orbitam o player, cortando quem chega perto.",   SpecificSkillType.EspadaFantasma,    SkillEvolutionType.EspadaFantasmaLend,  SkillRarity.Legendary, new Color(0.85f, 0.85f, 1f)));
        lista.Add(Criar("CorrenteSombriaLend", "Corrente da Alma",     "LENDÁRIA: os acorrentados compartilham dano — quanto mais elos, mais dói em todos.", SpecificSkillType.CorrenteSombria, SkillEvolutionType.CorrenteSombriaLend, SkillRarity.Legendary, new Color(0.5f, 0.15f, 0.9f)));

        // Espada Rotatória, Projétil Base, Espiral e Spirit Homing removidos a pedido do usuário

        // ── ULTIMATES ────────────────────────────────────────────────────────────

        // Raio Certeiro
        lista.Add(Criar("RaioEterno",            "Raio Eterno",             "+3 ricochetes extras na cadeia de raios.",                          SpecificSkillType.UltRaioCerteiro,      SkillEvolutionType.RaioEterno,            SkillRarity.Rare,  new Color(0.35f, 0.7f,  1f)));
        lista.Add(Criar("RaioOvercarga",          "Raio Sobrecarga",         "Cada bounce causa dano em área de 2u ao redor do impacto.",         SpecificSkillType.UltRaioCerteiro,      SkillEvolutionType.RaioOvercarga,         SkillRarity.Epic,  new Color(0.1f,  0.5f,  1f)));

        // Tempestade Elétrica
        lista.Add(Criar("TempestadeIntensa",      "Tempestade Intensa",      "+50% de raio e dispara 2 raios simultâneos por intervalo.",         SpecificSkillType.UltTempestadeEletrica, SkillEvolutionType.TempestadeIntensa,    SkillRarity.Rare,  new Color(1f,   0.9f,  0.1f)));
        lista.Add(Criar("TempestadeContinua",     "Tempestade Contínua",     "+3s de duração da tempestade.",                                     SpecificSkillType.UltTempestadeEletrica, SkillEvolutionType.TempestadeContinua,  SkillRarity.Epic,  new Color(1f,   0.7f,  0.05f)));

        // Chuva de Meteoros
        lista.Add(Criar("MeteorosDuploImpacto",   "Meteoros de Duplo Impacto","Cada meteoro aplica dano duas vezes ao aterrissar.",              SpecificSkillType.UltChuvaMeteorosUlt,  SkillEvolutionType.MeteorosDuploImpacto,  SkillRarity.Rare,  new Color(1f,   0.55f, 0.1f)));
        lista.Add(Criar("MeteorosMaior",           "Meteoros Maiores",        "+50% raio de impacto e +30% de dano por meteoro.",                 SpecificSkillType.UltChuvaMeteorosUlt,  SkillEvolutionType.MeteorosMaior,         SkillRarity.Epic,  new Color(1f,   0.3f,  0.0f)));

        // Campo de Gelo
        lista.Add(Criar("GeloAbsoluto",           "Gelo Absoluto",           "Inimigos congelados recebem 50% mais dano de todas as fontes.",     SpecificSkillType.UltCampoDeGelo,       SkillEvolutionType.GeloAbsoluto,          SkillRarity.Rare,  new Color(0.5f,  0.85f, 1f)));
        lista.Add(Criar("GeloEterno",             "Gelo Eterno",             "+4s de duração total do Campo de Gelo.",                            SpecificSkillType.UltCampoDeGelo,       SkillEvolutionType.GeloEterno,            SkillRarity.Epic,  new Color(0.3f,  0.7f,  1f)));

        // Vórtice
        lista.Add(Criar("VorticeDestruidor",      "Vórtice Destruidor",      "Inimigos dentro do vórtice recebem 15 de dano por segundo.",        SpecificSkillType.UltVortice,           SkillEvolutionType.VorticeDestruidor,     SkillRarity.Rare,  new Color(0.3f,  1f,    0.6f)));
        lista.Add(Criar("VorticeExpansivo",       "Vórtice Expansivo",       "+40% de raio de atração e +2s de duração.",                         SpecificSkillType.UltVortice,           SkillEvolutionType.VorticeExpansivo,      SkillRarity.Epic,  new Color(0.1f,  0.85f, 0.5f)));

        // Necrópole
        lista.Add(Criar("NecropoleExercito",      "Exército das Sombras",    "Fantasmas duram +3s e têm +50% de velocidade.",                     SpecificSkillType.UltNecropole,         SkillEvolutionType.NecropoleExercito,     SkillRarity.Rare,  new Color(0.5f,  0.1f,  0.8f)));
        lista.Add(Criar("NecropoleContaminacao",  "Contaminação Sombria",    "Inimigos mortos na zona envenenam os vizinhos em 3u por 2s.",        SpecificSkillType.UltNecropole,         SkillEvolutionType.NecropoleContaminacao, SkillRarity.Epic,  new Color(0.35f, 0.0f,  0.6f)));

        // Ritual do Ancião
        lista.Add(Criar("RitualAmpliado",         "Ritual Ampliado",         "+40% de raio do pentágono do ritual.",                              SpecificSkillType.UltRitualAnciao,      SkillEvolutionType.RitualAmpliado,        SkillRarity.Rare,  new Color(0.7f,  0.3f,  1f)));
        lista.Add(Criar("RitualExplosivo",        "Ritual Explosivo",        "A explosão final tem 2x de raio e dano.",                           SpecificSkillType.UltRitualAnciao,      SkillEvolutionType.RitualExplosivo,       SkillRarity.Epic,  new Color(1f,    0.15f, 0.1f)));

        // Bênção do Ancião
        lista.Add(Criar("BencaoIntensa",          "Bênção Intensa",          "Cura por pulso aumenta para 15% do HP máximo.",                     SpecificSkillType.UltBencaoAnciao,      SkillEvolutionType.BencaoIntensa,         SkillRarity.Rare,  new Color(1f,    0.82f, 0.1f)));
        lista.Add(Criar("BencaoRapida",           "Bênção Rápida",           "Intervalo entre pulsos reduzido para 0.6s.",                         SpecificSkillType.UltBencaoAnciao,      SkillEvolutionType.BencaoRapida,          SkillRarity.Epic,  new Color(1f,    0.65f, 0.05f)));

        // Casulo de Cristal
        lista.Add(Criar("CasuloReforjado",        "Casulo Reforjado",        "Ao quebrar, lança +8 estilhaços extras.",                           SpecificSkillType.UltCasuloCristal,     SkillEvolutionType.CasuloReforjado,       SkillRarity.Rare,  new Color(0.55f, 0.9f,  1f)));
        lista.Add(Criar("CasuloLetal",            "Casulo Letal",            "+75% de dano dos estilhaços e +50% de raio de explosão.",           SpecificSkillType.UltCasuloCristal,     SkillEvolutionType.CasuloLetal,           SkillRarity.Epic,  new Color(0.3f,  0.75f, 1f)));

        // Correntes do Inferno
        lista.Add(Criar("InfernoPropagado",       "Inferno Propagado",       "Inimigos acorrentados propagam fogo para vizinhos em 5u.",           SpecificSkillType.UltCorrentesInferno,  SkillEvolutionType.InfernoPropagado,      SkillRarity.Rare,  new Color(1f,    0.5f,  0.05f)));
        lista.Add(Criar("InfernoIntensidade",     "Intensidade Infernal",    "Dano por segundo das correntes é dobrado.",                          SpecificSkillType.UltCorrentesInferno,  SkillEvolutionType.InfernoIntensidade,    SkillRarity.Epic,  new Color(1f,    0.2f,  0.0f)));

        // Drenagem de Vida
        lista.Add(Criar("DrenagemTotal",          "Drenagem Total",          "Percentual de cura por dano aumenta para 25%.",                      SpecificSkillType.UltDrenagemDeVida,    SkillEvolutionType.DrenagemTotal,         SkillRarity.Rare,  new Color(0.9f,  0.1f,  0.45f)));
        lista.Add(Criar("DrenagemMassiva",        "Drenagem Massiva",        "+50% de raio da aura de drenagem.",                                  SpecificSkillType.UltDrenagemDeVida,    SkillEvolutionType.DrenagemMassiva,       SkillRarity.Epic,  new Color(0.75f, 0.05f, 0.35f)));

        // Escudo Sônico
        lista.Add(Criar("SonicoAmplificado",      "Sônico Amplificado",      "Dano base por pulso é dobrado.",                                    SpecificSkillType.UltEscudoSonico,      SkillEvolutionType.SonicoAmplificado,     SkillRarity.Rare,  new Color(0.6f,  0.9f,  1f)));
        lista.Add(Criar("SonicoPercussao",        "Percussão Brutal",        "Knockback +50% maior e atordoa inimigos por 0.5s.",                 SpecificSkillType.UltEscudoSonico,      SkillEvolutionType.SonicoPercussao,       SkillRarity.Epic,  new Color(0.4f,  0.75f, 1f)));

        // Pulso Magnético
        lista.Add(Criar("MagneticoSupercarregado","Magnético Supercarregado","+80% de força de repulsão e +50% de dano de repulsão.",             SpecificSkillType.UltPulsoMagnetico,    SkillEvolutionType.MagneticoSupercarregado, SkillRarity.Rare, new Color(0.7f, 0.35f, 1f)));
        lista.Add(Criar("MagneticoCentro",        "Centro Gravitacional",    "+1.5s de fase de atração antes da repulsão.",                       SpecificSkillType.UltPulsoMagnetico,    SkillEvolutionType.MagneticoCentro,       SkillRarity.Epic,  new Color(0.55f, 0.2f,  1f)));

        // Punição Divina
        lista.Add(Criar("PunicaoJulgamento",      "Julgamento Supremo",      "+2 raios secundários e dano secundário +50%.",                      SpecificSkillType.UltPunicaoDivina,     SkillEvolutionType.PunicaoJulgamento,     SkillRarity.Rare,  new Color(1f,    0.9f,  0.2f)));
        lista.Add(Criar("PunicaoDivina2",         "Ira Divina",              "Dano principal +60% e raio de explosão +50%.",                      SpecificSkillType.UltPunicaoDivina,     SkillEvolutionType.PunicaoDivina2,        SkillRarity.Epic,  new Color(1f,    0.7f,  0.0f)));

        // Domo Retardante
        lista.Add(Criar("DomoFortificado",        "Domo Fortificado",        "+3s de duração e velocidade dentro do domo reduz a 0.3.",           SpecificSkillType.UltDomoRetardante,    SkillEvolutionType.DomoFortificado,       SkillRarity.Rare,  new Color(0.2f,  0.7f,  1f)));
        lista.Add(Criar("DomoInversor",           "Domo Inversor",           "Ao sair do domo, projéteis são redirecionados para inimigos.",       SpecificSkillType.UltDomoRetardante,    SkillEvolutionType.DomoInversor,          SkillRarity.Epic,  new Color(0.1f,  0.5f,  1f)));

        // Despertar do Ancião
        lista.Add(Criar("DespertarFurioso",       "Despertar Furioso",       "Intervalo entre golpes tentáculo reduz para 0.35s.",                SpecificSkillType.UltDespertarAnciao,   SkillEvolutionType.DespertarFurioso,      SkillRarity.Rare,  new Color(0.35f, 0.0f,  0.55f)));
        lista.Add(Criar("DespertarGigante",       "Despertar Gigante",       "+60% de raio de impacto dos tentáculos e +50% de dano.",            SpecificSkillType.UltDespertarAnciao,   SkillEvolutionType.DespertarGigante,      SkillRarity.Epic,  new Color(0.5f,  0.0f,  0.8f)));

        // Maré Implacável
        lista.Add(Criar("MareEletrica",           "Maré Elétrica",           "+50% de dano de afogamento por tick.",                              SpecificSkillType.UltMareImplacavel,    SkillEvolutionType.MareEletrica,          SkillRarity.Rare,  new Color(0.1f,  0.5f,  1f)));
        lista.Add(Criar("MarePersistente",        "Maré Persistente",        "+3s de duração e +1.5 de raio da maré.",                            SpecificSkillType.UltMareImplacavel,    SkillEvolutionType.MarePersistente,       SkillRarity.Epic,  new Color(0.05f, 0.35f, 0.9f)));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Adiciona ao GerenciadorEventos
        var ge = Object.FindFirstObjectByType<GerenciadorEventos>();
        if (ge != null)
        {
            ge.todasEvolucoes.Clear();
            ge.todasEvolucoes.AddRange(lista);
            EditorUtility.SetDirty(ge);
            Debug.Log($"✅ {lista.Count} evoluções criadas e adicionadas ao GerenciadorEventos!");
        }
        else
            Debug.Log($"✅ {lista.Count} evoluções criadas em {PASTA}. Abra a cena de jogo e execute novamente para registrar no GerenciadorEventos.");
    }

    static SkillEvolutionData Criar(string id, string nome, string desc,
        SpecificSkillType skillAlvo, SkillEvolutionType tipo, SkillRarity raridade, Color cor)
    {
        string path = $"{PASTA}/{id}.asset";
        var existente = AssetDatabase.LoadAssetAtPath<SkillEvolutionData>(path);

        // Sobrescreve se existir para garantir dados corretos
        SkillEvolutionData data;
        if (existente != null)
        {
            data = existente;
        }
        else
        {
            data = ScriptableObject.CreateInstance<SkillEvolutionData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.nomeEvolucao = nome;
        data.descricao    = desc;
        data.skillAlvo    = skillAlvo;
        data.tipoEvolucao = tipo;
        data.raridade     = raridade;
        data.corDestaque  = cor;
        data.icone        = BuscarIconeSkill(skillAlvo);
        EditorUtility.SetDirty(data);
        return data;
    }

    // Busca o ícone da SkillData ou UltimateData correspondente ao tipo
    static Sprite BuscarIconeSkill(SpecificSkillType tipo)
    {
        // Tenta nas SkillData em Assets/Skills/
        foreach (var guid in AssetDatabase.FindAssets("t:SkillData", new[] { "Assets/Skills" }))
        {
            var sd = AssetDatabase.LoadAssetAtPath<SkillData>(AssetDatabase.GUIDToAssetPath(guid));
            if (sd != null && sd.specificType == tipo && sd.icon != null)
                return sd.icon;
        }

        // Tenta nas UltimateData em Assets/prefebs/ultimates/
        foreach (var guid in AssetDatabase.FindAssets("t:UltimateData", new[] { "Assets/prefebs/ultimates" }))
        {
            var ud = AssetDatabase.LoadAssetAtPath<UltimateData>(AssetDatabase.GUIDToAssetPath(guid));
            if (ud != null && ud.ultimateType == tipo && ud.ultimateIcon != null)
                return ud.ultimateIcon;
        }

        // Fallback: tenta pelo nome do tipo (ex: "LancaLuz" → "LancaLuzIcon.png")
        string nomeTipo = tipo.ToString().Replace("Ult", "");
        string[] candidatos = {
            $"Assets/Skills/{nomeTipo}Icon.png",
            $"Assets/prefebs/ultimates/{System.Text.RegularExpressions.Regex.Replace(nomeTipo, "(?<=.)(?=[A-Z])", "_").ToLower()}_icon.png",
        };
        foreach (var p in candidatos)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (s != null) return s;
        }

        return null;
    }
}
#endif
