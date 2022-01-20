using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace DiscordJsonBanner.Config
{
    [DebuggerDisplay("Configuration Data")]
    public sealed class OnlineProviderConfiguration
    {
        /// <summary>
        /// URL from which site pickup information
        /// </summary>
        [JsonProperty("url", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string Url { get; set; }
        /// <summary>
        /// JSON path selector for the online (can return a int value or an array of int)
        /// </summary>
        [JsonProperty("selector", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string OnlineSelector { get; set; }

        /// <summary>
        /// Interval between updates (In minutes, default is 5 minutes)
        /// </summary>
        [JsonProperty("update_interval", DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(5)]
        public byte UpdateInterval { get; set; }
    }
}
