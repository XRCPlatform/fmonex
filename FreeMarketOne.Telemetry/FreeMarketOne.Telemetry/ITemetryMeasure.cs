using System;

namespace FreeMarketOne.Telemetry
{
    interface ITemetryMeasure
    {
        /// <summary>
        /// Date and time of the measurable event.
        /// </summary>
        DateTime Time { get; set; }

        /// <summary>
        /// Platform version so that we can monitor upgrades and compatibilities.
        /// </summary>
        string PlatformVersion { get; set; }

        /// <summary>
        /// Anonymised user hash solely used for measure but not revealing anything personal such us IPs, identities and etc.
        /// </summary>
        string UserId { get; set; }

        /// <summary>
        /// Action that user has taken, such as start_app, stop_app, search, bid, list_product, buy_product, view_product, expand_product, request_arbitration and etc. 
        /// </summary>
        string Action { get; set; }

        /// <summary>
        /// A thing that is acted on, such as product id, search phrase and etc.
        /// </summary>
        string ActionTarget { get; set; }

        /// <summary>
        /// Result of the action. Such as succesfuly completed transaction. Arbitration. Win/Lose bid.
        /// </summary>
        string Outcome { get; set; }
    }
}
