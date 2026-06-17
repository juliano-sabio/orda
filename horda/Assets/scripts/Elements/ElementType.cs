public enum ElementType
{
    None, Fogo, Ar, Terra, Agua, Raio, Gelo, Planta, Trevas, Luz, Corrompido
}

public enum CharacteristicType
{
    // Fogo
    Queimadura, Explosao,
    // Ar
    Recuo, Rajada,
    // Terra
    Atordoamento, EscudoPedra,
    // Agua
    Lentidao, Cura,
    // Raio
    Cadeia, Paralisia,
    // Gelo
    Congelamento, Fragilidade,
    // Planta
    Veneno, Enraizamento,
    // Trevas
    Maldicao, RouboVida,
    // Luz
    Sagrado, Cegamento,
    // Corrompido
    Caos, Infeccao
}

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
