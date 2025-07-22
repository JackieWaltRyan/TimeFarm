using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TimeFarm;

internal sealed record TimeFarmConfig {
    [JsonInclude]
    public bool Enable { get; set; }

    [JsonInclude]
    public uint Time { get; set; } = 1000;

    [JsonInclude]
    public List<uint> Whitelist { get; set; } = [];

    [JsonInclude]
    public List<uint> Blacklist { get; set; } = [];

    [JsonConstructor]
    public TimeFarmConfig() { }
}

internal sealed record GetOwnedGamesResponse {
    [JsonPropertyName("response")]
    public ResponseData? Response { get; set; }

    internal sealed record ResponseData {
        [JsonPropertyName("games")]
        public List<Game>? Games { get; set; }

        internal sealed record Game {
            [JsonPropertyName("appid")]
            public uint AppId { get; set; }

            [JsonPropertyName("playtime_forever")]
            public uint PlayTimeForever { get; set; }
        }
    }
}
