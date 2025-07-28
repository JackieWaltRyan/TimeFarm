using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ArchiSteamFarm.Helpers.Json;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Web.Responses;

namespace TimeFarm;

internal sealed class TimeFarm : IGitHubPluginUpdates, IBotModules, IBotCardsFarmerInfo {
    public string Name => nameof(TimeFarm);
    public string RepositoryName => "JackieWaltRyan/TimeFarm";
    public Version Version => typeof(TimeFarm).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

    public Dictionary<string, TimeFarmConfig> TimeFarmConfig = new();
    public Dictionary<string, Dictionary<string, Timer>> TimeFarmTimers = new();

    public Task OnLoaded() => Task.CompletedTask;

    public async Task OnBotInitModules(Bot bot, IReadOnlyDictionary<string, JsonElement>? additionalConfigProperties = null) {
        if (additionalConfigProperties != null) {
            if (TimeFarmTimers.TryGetValue(bot.BotName, out Dictionary<string, Timer>? dict)) {
                foreach (KeyValuePair<string, Timer> timers in dict) {
                    await timers.Value.DisposeAsync().ConfigureAwait(false);

                    bot.ArchiLogger.LogGenericInfo($"{timers.Key} Dispose.");
                }
            }

            TimeFarmTimers[bot.BotName] = new Dictionary<string, Timer> {
                { "PlayedGamesWhileIdle", new Timer(async e => await PlayedGamesWhileIdle(bot).ConfigureAwait(false), null, Timeout.Infinite, Timeout.Infinite) }
            };

            TimeFarmConfig[bot.BotName] = new TimeFarmConfig();

            foreach (KeyValuePair<string, JsonElement> configProperty in additionalConfigProperties) {
                switch (configProperty.Key) {
                    case "TimeFarmConfig": {
                        TimeFarmConfig? config = configProperty.Value.ToJsonObject<TimeFarmConfig>();

                        if (config != null) {
                            TimeFarmConfig[bot.BotName] = config;
                        }

                        break;
                    }
                }
            }

            if (TimeFarmConfig[bot.BotName].Enable) {
                bot.ArchiLogger.LogGenericInfo($"TimeFarmConfig: {TimeFarmConfig[bot.BotName].ToJsonText()}");
            }
        }
    }

    public Task OnBotFarmingStopped(Bot bot) => Task.CompletedTask;

    public async Task OnBotFarmingStarted(Bot bot) {
        if (TimeFarmTimers.TryGetValue(bot.BotName, out Dictionary<string, Timer>? dict)) {
            foreach (KeyValuePair<string, Timer> timers in dict) {
                await timers.Value.DisposeAsync().ConfigureAwait(false);

                bot.ArchiLogger.LogGenericInfo($"{timers.Key} Dispose.");
            }
        }
    }

    public Task OnBotFarmingFinished(Bot bot, bool farmedSomething) {
        if (!TimeFarmConfig.TryGetValue(bot.BotName, out TimeFarmConfig? value) || !value.Enable) {
            return Task.CompletedTask;
        }

        if (TimeFarmTimers.TryGetValue(bot.BotName, out Dictionary<string, Timer>? dict)) {
            dict["PlayedGamesWhileIdle"].Change(1, -1);
        }

        return Task.CompletedTask;
    }

    public async Task PlayedGamesWhileIdle(Bot bot) {
        const uint timeout = 1;

        if (bot.IsConnectedAndLoggedOn) {
            try {
                bot.ArchiLogger.LogGenericInfo($"IsPlayingPossible: {bot.IsPlayingPossible}");
            } catch {
                bot.ArchiLogger.LogGenericInfo("IsPlayingPossible: null");
            }

            try {
                bot.ArchiLogger.LogGenericInfo($"NowFarming: {bot.CardsFarmer.NowFarming}");
            } catch {
                bot.ArchiLogger.LogGenericInfo("NowFarming: null");
            }

            // if (!bot.CardsFarmer.NowFarming && bot.IsPlayingPossible) {
            ObjectResponse<GetOwnedGamesResponse>? rawResponse = await bot.ArchiWebHandler.UrlGetToJsonObjectWithSession<GetOwnedGamesResponse>(new Uri($"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?access_token={bot.AccessToken}&steamid={bot.SteamID}&include_played_free_games=true&include_free_sub=true&skip_unvetted_apps=false")).ConfigureAwait(false);

            List<GetOwnedGamesResponse.ResponseData.Game>? games = rawResponse?.Content?.Response?.Games;

            if (games != null) {
                bot.ArchiLogger.LogGenericInfo($"Total games found: {games.Count}");

                //         if (games.Count > 0) {
                //             List<uint> gamesIDs = [];
                //
                //             TimeFarmConfig tfc = TimeFarmConfig[bot.BotName];
                //
                //             if (tfc.Whitelist.Count > 0) {
                //                 bot.ArchiLogger.LogGenericInfo($"Whitelist: {tfc.Whitelist.ToJsonText()}");
                //             }
                //
                //             if (tfc.Blacklist.Count > 0) {
                //                 bot.ArchiLogger.LogGenericInfo($"Blacklist: {tfc.Blacklist.ToJsonText()}");
                //             }
                //
                //             foreach (GetOwnedGamesResponse.ResponseData.Game game in games) {
                //                 if (tfc.Whitelist.Contains(game.AppId)) {
                //                     gamesIDs.Add(game.AppId);
                //                 }
                //             }
                //
                //             foreach (GetOwnedGamesResponse.ResponseData.Game game in games) {
                //                 if (!gamesIDs.Contains(game.AppId) && (game.PlayTimeForever < tfc.Time * 60) && !tfc.Blacklist.Contains(game.AppId)) {
                //                     gamesIDs.Add(game.AppId);
                //                 }
                //             }
                //
                //             if (gamesIDs.Count > 0) {
                //                 bot.ArchiLogger.LogGenericInfo($"Found games less {tfc.Time} hours: {gamesIDs.Count}");
                //
                //                 List<uint> filterIDs = gamesIDs.Count <= 32 ? gamesIDs : gamesIDs.GetRange(0, 32);
                //
                //                 timeout = 15;
                //
                //                 bot.ArchiLogger.LogGenericInfo($"Status: Playing {filterIDs.Count} selected games: {filterIDs.ToJsonText()} | Next check: {DateTime.Now.AddMinutes(timeout):T}");
                //             } else {
                //                 bot.ArchiLogger.LogGenericInfo($"Status: NotFoundGamesLess{tfc.Time}Hours");
                //
                //                 return;
                //             }
                //         } else {
                //             bot.ArchiLogger.LogGenericInfo("Status: GameListIsEmpty");
                //
                //             return;
                //         }
                //     } else {
                //         bot.ArchiLogger.LogGenericInfo($"Status: Error | Next run: {DateTime.Now.AddMinutes(timeout):T}");
                //     }
            } else {
                bot.ArchiLogger.LogGenericInfo("Status: BotNowFarming or PlayingNotPossible");

                return;
            }
        } else {
            bot.ArchiLogger.LogGenericInfo($"Status: BotNotConnected | Next run: {DateTime.Now.AddMinutes(timeout):T}");
        }

        TimeFarmTimers[bot.BotName]["PlayedGamesWhileIdle"].Change(TimeSpan.FromMinutes(timeout), TimeSpan.FromMilliseconds(-1));
    }
}
