using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Playnite.SDK;
using WineBridgePlugin.Settings;
using WineBridgePlugin.Utils;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WineBridgePlugin.Integrations.Lutris
{
    public enum LutrisPlatform
    {
        Gog,
        Epic,
        Amazon,
        EaApp,
        BattleNet,
        ItchIo
    }

    public class LutrisInstalledGame
    {
        public long LutrisId { get; set; }
        public string Name { get; set; }
        public string ServiceName { get; set; }
        public string PlayniteGameId { get; set; }
        public string InstallPath { get; set; }
        public string Service { get; set; }
    }

    public class LutrisGameEntity
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Directory { get; set; }
        public string ConfigPath { get; set; }
        public string Service { get; set; }
        public string ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string AmazonId { get; set; }
        public string BattleNetId { get; set; }
        public string EaAppId { get; set; }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class LutrisConfig
    {
        public LutrisConfigGame Game { get; set; }
    }


    // ReSharper disable once ClassNeverInstantiated.Global
    public class LutrisConfigGame
    {
        public string Exe { get; set; }
        public string Prefix { get; set; }
        public string WorkingDir { get; set; }
    }

    public static class LutrisClient
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        public static List<LutrisInstalledGame> GetInstalledGames(LutrisPlatform? platform = null,
            string playniteGameId = null)
        {
            var lutrisDataPathLinux = WineBridgeSettings.LutrisDataPathLinux;

            var gamesFromDb = FetchInstalledGamesFromDatabase(lutrisDataPathLinux, platform, playniteGameId);
            return gamesFromDb.ConvertAll(gameEntity => GetInstalledGame(gameEntity, lutrisDataPathLinux));
        }

        public static bool IsGameInstalled(LutrisPlatform platform, string playniteGameId)
        {
            try
            {
                return DoInConnection(connection =>
                {
                    var command = connection.CreateCommand();
                    var query =
                        "select g.id from games g left join service_games sg on sg.service = g.service and sg.appid = g.service_id where g.installed = 1";
                    AddServiceAndIdParameters(ref query, command, platform, playniteGameId);

                    command.CommandText = query;
                    using (var reader = command.ExecuteReader())
                    {
                        return reader.Read();
                    }
                });
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to check if the game is installed in Lutris database.");
                return false;
            }
        }

        private static List<LutrisGameEntity> FetchInstalledGamesFromDatabase(string lutrisDataPathLinux,
            LutrisPlatform? platform = null, string playniteGameId = null)
        {
            try
            {
                return DoInConnection(lutrisDataPathLinux, connection =>
                {
                    var gameEntities = new List<LutrisGameEntity>();

                    var command = connection.CreateCommand();
                    var query =
                        "select g.id, g.name, g.directory, g.configpath, g.service, g.service_id, sg.name, json_extract(details, '$.product.id') amazon_id, json_extract(details, '$.product_code') bnet_id, json_extract(details, '$.originOfferId') ea_id from games g left join service_games sg on sg.service = g.service and sg.appid = g.service_id  where g.installed = 1";
                    AddServiceAndIdParameters(ref query, command, platform, playniteGameId);

                    command.CommandText = query;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            gameEntities.Add(new LutrisGameEntity
                            {
                                Id = GetLongValue(reader[0]),
                                Name = GetStringValue(reader[1]),
                                Directory = GetStringValue(reader[2]),
                                ConfigPath = GetStringValue(reader[3]),
                                Service = GetStringValue(reader[4]),
                                ServiceId = GetStringValue(reader[5]),
                                ServiceName = GetStringValue(reader[6]),
                                AmazonId = GetStringValue(reader[7]),
                                BattleNetId = GetStringValue(reader[8]),
                                EaAppId = GetStringValue(reader[9])
                            });
                        }
                    }

                    return gameEntities;
                });
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to get installed games from Lutris database.");
                throw;
            }
        }

        public static string GetInstallId(LutrisPlatform platform, string playniteGameId)
        {
            if (platform == LutrisPlatform.Epic || platform == LutrisPlatform.Gog || platform == LutrisPlatform.ItchIo)
            {
                return playniteGameId;
            }

            try
            {
                return DoInConnection(connection =>
                {
                    var command = connection.CreateCommand();
                    var query = "select sg.appid from service_games sg where 1=1";
                    AddServiceAndIdParameters(ref query, command, platform, playniteGameId);

                    command.CommandText = query;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return GetStringValue(reader[0]);
                        }
                    }

                    return null;
                });
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to get installed games from Lutris database.");
                throw;
            }
        }

        private static LutrisInstalledGame GetInstalledGame(LutrisGameEntity gameEntity, string lutrisDataPathLinux)
        {
            var configPath = gameEntity.ConfigPath;
            if (string.IsNullOrEmpty(configPath))
            {
                return new LutrisInstalledGame
                {
                    LutrisId = gameEntity.Id,
                    Name = gameEntity.Name,
                    ServiceName = gameEntity.ServiceName,
                    PlayniteGameId = GetPlayniteGameId(gameEntity),
                    Service = gameEntity.Service
                };
            }

            string installPath = null;
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(new UnderscoredNamingConvention())
                    .IgnoreUnmatchedProperties()
                    .Build();
                var config = deserializer.Deserialize<LutrisConfig>(File.ReadAllText(
                    Path.Combine(lutrisDataPathLinux, "games", $"{configPath}.yml")));
                var workingDir = config?.Game?.WorkingDir;
                if (!string.IsNullOrEmpty(workingDir) && workingDir != "null")
                {
                    installPath = WineUtils.LinuxPathToWindows(workingDir);
                }
                else if (!string.IsNullOrEmpty(gameEntity.Directory))
                {
                    installPath = WineUtils.LinuxPathToWindows(gameEntity.Directory);
                }
                else if (config?.Game?.Prefix != null)
                {
                    installPath = WineUtils.LinuxPathToWindows(config.Game.Prefix);
                }
                else if (config?.Game?.Exe != null)
                {
                    installPath = Path.GetDirectoryName(WineUtils.LinuxPathToWindows(config.Game.Exe));
                }
                else
                {
                    Logger.Warn($"Could not find install path for game {gameEntity.Name} (id: {gameEntity.Id})");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to deserialize game config (id: {gameEntity.Id}, path: {configPath})");
            }

            return new LutrisInstalledGame
            {
                LutrisId = gameEntity.Id,
                Name = gameEntity.Name,
                ServiceName = gameEntity.ServiceName,
                PlayniteGameId = GetPlayniteGameId(gameEntity),
                InstallPath = installPath,
                Service = gameEntity.Service
            };
        }

        public static long? GetGameId(LutrisPlatform platform, string gameId)
        {
            try
            {
                return DoInConnection<long?>(connection =>
                {
                    var command = connection.CreateCommand();
                    var query =
                        "select g.id from games g left join service_games sg on sg.service = g.service and sg.appid = g.service_id where g.installed = 1";
                    AddServiceAndIdParameters(ref query, command, platform, gameId);

                    command.CommandText = query;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return GetLongValue(reader[0]);
                        }

                        return null;
                    }
                });
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to get game id from Lutris database.");
                throw;
            }
        }

        private static T DoInConnection<T>(Func<SqliteConnection, T> action)
        {
            return DoInConnection(WineBridgeSettings.LutrisDataPathLinux, action);
        }

        private static T DoInConnection<T>(string lutrisDataPathLinux, Func<SqliteConnection, T> action)
        {
            using (var connection = new SqliteConnection(
                       $"Data Source={Path.Combine(lutrisDataPathLinux, "pga.db")};Mode=ReadOnly;Cache=Shared"))
            {
                connection.Open();

                return action(connection);
            }
        }

        private static string GetPlayniteGameId(LutrisGameEntity gameEntity)
        {
            switch (gameEntity.Service)
            {
                case "amazon":
                    return gameEntity.AmazonId;
                case "battlenet":
                    return gameEntity.BattleNetId;
                case "ea_app":
                    return gameEntity.EaAppId;
                default:
                    return gameEntity.ServiceId;
            }
        }

        private static void AddServiceAndIdParameters(ref string query,
            SqliteCommand command, LutrisPlatform? platform, string playniteGameId)
        {
            if (platform == null)
            {
                return;
            }

            query += " and sg.service = $service";
            command.Parameters.AddWithValue("service", LutrisUtils.GetLutrisService(platform.Value));

            if (string.IsNullOrEmpty(playniteGameId))
            {
                return;
            }

            switch (platform)
            {
                case LutrisPlatform.Amazon:
                    query += " and json_extract(sg.details, '$.product.id') = $gameId";
                    break;
                case LutrisPlatform.BattleNet:
                    query += " and json_extract(sg.details, '$.product_code') = $gameId";
                    break;
                case LutrisPlatform.EaApp:
                    query += " and json_extract(sg.details, '$.originOfferId') = $gameId";
                    break;
                default:
                    query += " and sg.appid = $gameId";
                    break;
            }

            command.Parameters.AddWithValue("gameId", playniteGameId);
        }

        private static long GetLongValue(object val)
        {
            return Convert.ToInt64(val);
        }

        private static string GetStringValue(object val)
        {
            return Convert.ToString(val);
        }
    }
}