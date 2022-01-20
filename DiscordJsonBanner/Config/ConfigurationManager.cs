using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscordJsonBanner.Config
{
    [DebuggerDisplay("Configuration Data")]
    public sealed class ConfigurationManager
    {
        /// <summary>
        /// Reads JSON and converting it to <see cref="ConfigurationManager"/> object
        /// </summary>
        /// <param name="reader">The JSON reader</param>
        /// <returns><see cref="ConfigurationManager"/> object</returns>
        public static ConfigurationManager[] FromJSON(JsonTextReader reader) => WorkAfterLoad(JToken.ReadFrom(reader, new JsonLoadSettings
        {
            CommentHandling = CommentHandling.Ignore,
        }).ToObject<ConfigurationManager[]>());

        /// <summary>
        /// Converts JSON to <see cref="ConfigurationManager"/> object
        /// </summary>
        /// <param name="JSON">JSON input</param>
        /// <returns><see cref="ConfigurationManager"/> object</returns>
        public static ConfigurationManager[] FromJSON(string JSON) => WorkAfterLoad(JsonConvert.DeserializeObject<ConfigurationManager[]>(JSON, new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Converters = new List<JsonConverter>
            {
                new BlankImage.ToIPreconditionConverter()
            }
        }));

        private static ConfigurationManager[] WorkAfterLoad(ConfigurationManager[] configs)
        {
            foreach (var config in configs)
            {
                config.BlankImagePath = config.BlankImagePath.OrderByDescending(bip => bip.Priority).ToArray();
            }
            return configs;
        }

        /// <summary>
        /// Guild ids
        /// </summary>
        [JsonProperty("guilds")]
        public ulong[] GuildIds { get; set; }

        /// <summary>
        /// Blank image, without any information. Used to render the image
        /// </summary>
        [JsonProperty("blank_image", DefaultValueHandling = DefaultValueHandling.Populate)]
        public BlankImage[] BlankImagePath { get; set; }

        /// <summary>
        /// Image render configuration
        /// </summary>
        [JsonProperty("render_set", DefaultValueHandling = DefaultValueHandling.Populate)]
        public ImageRenderConfiguration ImageRender { get; set; }

        /// <summary>
        /// Online Provider information
        /// </summary>
        [JsonProperty("provider", DefaultValueHandling = DefaultValueHandling.Populate)]
        public OnlineProviderConfiguration OnlineProvider { get; set; }

        // TODO: Добавить в конфиге и в этом классе интервал обновления, чтоб каждому проекту отдельно управлять интервалом

        /// <summary>
        /// The next time to update will be at
        /// </summary>
        public DateTime NextRun { get; set; } = DateTime.UnixEpoch;
    }
}
