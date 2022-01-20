using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscordJsonBanner.Config
{
    public class BlankImage
    {
        public interface IPrecondition
        {
            bool CheckPrecondition();
        }
        public struct TimePrecondition : IPrecondition
        {
            [JsonProperty("from_h")]
            public byte FromHour { get; set; }
            [JsonProperty("to_h")]
            public byte ToHour { get; set; }

            public bool CheckPrecondition()
            {
                return FromHour > ToHour
                    ? DateTime.UtcNow.Hour >= FromHour || DateTime.UtcNow.Hour <= ToHour
                    : DateTime.UtcNow.Hour >= FromHour && DateTime.UtcNow.Hour <= ToHour;
            }
        }

        public class ToIPreconditionConverter : JsonConverter<IPrecondition>
        {
            public static readonly IReadOnlyDictionary<string, Type> IPreconditionTypes = new Dictionary<string, Type>
            {
                { "time", typeof(TimePrecondition) }
            };
            
            public override void WriteJson(JsonWriter writer, IPrecondition value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override IPrecondition ReadJson(JsonReader reader, Type objectType, IPrecondition existingValue, bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.StartObject) return null;
                var jObject = JObject.Load(reader);
                if (!(jObject["type"] is JValue strType)) return null;
                jObject.Remove("type");
                try
                {
                    var (_, type) = IPreconditionTypes.First(t => t.Key.ToLowerInvariant().Equals(strType.Value));
                    return (IPrecondition)jObject.ToObject(type);
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }
        }
        
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("preconditions")]
        public IPrecondition[] Preconditions { get; set; }

        [JsonProperty("priority"), DefaultValue(0)]
        public short Priority { get; set; }
    }
}