namespace TicketService.Domain.Enumerations
{
    public enum TicketStatus
    {
        /// <summary>
        ///     1e Beoordeling
        /// </summary>
        FirstReview = 1,

        /// <summary>
        ///     2e Beoordeling
        /// </summary>
        SecondReview = 2,

        /// <summary>
        ///     In behandeling bij derden
        /// </summary>
        TreatmentThirdParty = 3,

        /// <summary>
        ///     Overleg gewenst
        /// </summary>
        ConsultationDesired = 4,

        /// <summary>
        ///     In behandeling support
        /// </summary>
        PendingSupport = 5,

        /// <summary>
        ///     Gepland
        /// </summary>
        Planned = 6,

        /// <summary>
        ///     FO/TO
        /// </summary>
        Foto = 7,

        /// <summary>
        ///     Ontwikkeling
        /// </summary>
        Development = 8,

        /// <summary>
        ///     Programmeringstest
        /// </summary>
        DevelopmentTest = 9,

        /// <summary>
        ///     Analistentest
        /// </summary>
        AnalystTest = 10,

        /// <summary>
        ///     Systeemtest
        /// </summary>
        SystemTest = 11,

        /// <summary>
        ///     Acceptatietest
        /// </summary>
        AcceptanceTest = 12,

        /// <summary>
        ///     Afgewezen
        /// </summary>
        Declined = 13,

        /// <summary>
        ///     Geparkeerd
        /// </summary>
        Parked = 14,

        /// <summary>
        ///     Gereed
        /// </summary>
        Ready = 15,

        /// <summary>
        ///     Vervallen
        /// </summary>
        Expired = 16,

        /// <summary>
        ///     Aangeboden ter test
        /// </summary>
        OfferedForTest = 17,

        /// <summary>
        ///     Beschikbaar stellen
        /// </summary>
        Available = 18,

        /// <summary>
        ///     Analyse
        /// </summary>
        Analyse = 19
    }
}