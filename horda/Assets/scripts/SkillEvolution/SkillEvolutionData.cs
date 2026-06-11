using UnityEngine;

public enum SkillEvolutionType
{
    Nenhuma = 0,

    // Campo de Espinhos
    EspinhosVenenosos,
    CampoAmpliado,

    // Chuva de Estrelas
    ChuvaIntensa,
    ImpactoSismico,

    // Garras do Abismo
    GarrasVenenosas,
    Execucao,

    // Fúria de Lâminas
    LaminasDuplas,
    LaminasExplosivas,

    // Sombras em Cruz
    CruzDupla,
    SombrasPerfurantes,

    // Corte Fantasma
    CorteTriple,
    CorteLetal,

    // Lança de Luz
    LancaPerfurante,
    LancaExplosiva,

    // Chicote de Energia
    ChicoteEletrico,
    DuplaRotacao,

    // Mísseis Teleguiados
    SalvaMisseis,
    MisseisExplosivos,

    // Pulso Rítmico
    PulsoIntenso,
    PulsoCadeia,

    // Espada Fantasma
    EspadaDuplaFantasma,
    EspadaFlamejante,

    // Corrente Sombria
    CorrenteReforcada,
    CorrenteParalisante,

    // Espada Rotatória
    EspadaOrbitalDupla,
    EspadaSonica,

    // Projétil Base
    SpiritBallDupla,
    SpiritBallExplosiva,

    // Projétil Espiral
    EspiralDupla,
    EspiralFuracao,

    // Spirit Homing
    HomingMultiplo,
    HomingEterno,

    // Evoluções de Alcance (melee)
    GarrasAlcance,
    ChicoteAlcance,
    PulsoAlcance,
    EspadaAlcance,
    CorrenteAlcance,

    // Evoluções de Quantidade (projétil)
    LancasMultiplas,

    // Segunda Chance
    SegundaChanceCura,
    SegundaChanceInvencivel,

    // Fuga das Sombras
    FugaInvulneravel,
    FugaCura,

    // Barreira de Energia
    BarreiraFortificada,
    BarreiraExplosiva,

    // Teia de Proteção
    TeiaVenenosa,
    TeiaPermanente,

    // Instinto de Sobrevivência
    InstintoFurioso,
    InstintoEspirito,

    // Espelho Mágico
    EspelhoAmplificado,
    EspelhoExplosivo,

    // Escudo de Karma
    KarmaReforcado,
    KarmaRetribuicao,

    // Escudo Espinhoso
    EspinhosVenenosos2,
    EspinhosExplosivos,

    // Aureola
    AureolaFortificada,
    AureolaExpansiva,

    // Barreira Reflexiva
    BarreiraTotal,
    BarreiraCongelante,

    // Cristais de Gelo
    CristaisDuplos,      // 5 cristais em vez de 3
    CristaisExplosivos,  // cada tiro explode em área

    // Raio Certeiro
    RaioEterno,        // +3 ricochetes extras
    RaioOvercarga,     // dano em área de 2u em cada bounce

    // Tempestade Elétrica
    TempestadeIntensa,   // +50% de raio e +2 raios simultâneos por disparo
    TempestadeContinua,  // +3s de duração

    // Chuva de Meteoros
    MeteorosDuploImpacto,  // cada meteoro aplica dano duas vezes
    MeteorosMaior,         // +50% raio de impacto e +30% dano

    // Campo de Gelo
    GeloAbsoluto,    // inimigos congelados recebem 50% mais dano
    GeloEterno,      // +4s de duração total

    // Vórtice
    VorticeDestruidor,  // inimigos atraídos recebem 15 de dano/s
    VorticeExpansivo,   // +40% de raio e +2s de duração

    // Necrópole
    NecropoleExercito,   // fantasmas duram +3s e têm +50% de velocidade
    NecropoleContaminacao, // inimigos mortos dentro da zona envenenam vizinhos

    // Ritual do Ancião
    RitualAmpliado,   // +40% de raio do pentágono
    RitualExplosivo,  // explosão final tem 2x de raio e dano

    // Bênção do Ancião
    BencaoIntensa,    // cura por pulso aumenta para 15% do HP máximo
    BencaoRapida,     // intervalo entre pulsos reduzido para 0.6s

    // Casulo de Cristal
    CasuloReforjado,   // ao quebrar, lança +8 estilhaços extras
    CasuloLetal,       // dano dos estilhaços aumenta 75% e raio +50%

    // Correntes do Inferno
    InfernoPropagado,  // inimigos acorrentados propagam fogo para vizinhos próximos (5u)
    InfernoIntensidade, // dano por segundo dobrado

    // Drenagem de Vida
    DrenagemTotal,     // percentual de cura aumenta para 25%
    DrenagemMassiva,   // raio da drenagem aumenta 50%

    // Escudo Sônico
    SonicoAmplificado, // dano base por pulso dobrado
    SonicoPercussao,   // knockback 50% maior e atordoa inimigos por 0.5s

    // Forma Bestial - removida do jogo, valores mantidos para não deslocar os enums seguintes
    BestialFrenesi,
    BestialRugidoMortal,

    // Pulso Magnético
    MagneticoSupercarregado, // força de repulsão +80% e dano de repulsão +50%
    MagneticoCentro,         // tempo de atração +1.5s antes da repulsão

    // Punição Divina
    PunicaoJulgamento,  // +2 raios secundários e dano secundário +50%
    PunicaoDivina2,     // dano principal +60% e raio de explosão +50%

    // Domo Retardante
    DomoFortificado,   // duração +3s e velocidade dentro do domo reduz a 0.3
    DomoInversor,      // ao sair do domo, projéteis são invertidos em direção aos inimigos

    // Despertar do Ancião
    DespertarFurioso,  // intervalo entre golpes reduz para 0.35s
    DespertarGigante,  // raio de impacto +60% e dano +50%

    // Maré Implacável
    MareEletrica,    // dano de afogamento +50%
    MarePersistente, // +3s de duração e +1.5 de raio
}

[CreateAssetMenu(fileName = "New Evolution", menuName = "Survivor/Skill Evolution")]
public class SkillEvolutionData : ScriptableObject
{
    [Header("Identificação")]
    public string nomeEvolucao;
    [TextArea(2, 4)]
    public string descricao;
    public Sprite  icone;
    public Color   corDestaque = new Color(1f, 0.8f, 0.2f);

    [Header("Skill Alvo")]
    public SpecificSkillType skillAlvo;
    public SkillEvolutionType tipoEvolucao;

    [Header("Raridade")]
    public SkillRarity raridade = SkillRarity.Rare;
}
