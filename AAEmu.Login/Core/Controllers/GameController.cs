﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AAEmu.Commons.Utils;
using AAEmu.Commons.Utils.DB;
using AAEmu.Login.Core.Network.Connections;
using AAEmu.Login.Core.Network.Internal;
using AAEmu.Login.Core.Packets.L2C;
using AAEmu.Login.Core.Packets.L2G;
using AAEmu.Login.Models;
using NLog;

namespace AAEmu.Login.Core.Controllers;

public class GameController : Singleton<GameController>
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();
    private Dictionary<byte, GameServer> _gameServers;
    private Dictionary<byte, byte> _mirrorsId;

    public byte? GetParentId(byte gsId)
    {
        if (_mirrorsId.TryGetValue(gsId, out var id))
            return id;
        return null;
    }

    protected GameController()
    {
        _gameServers = new Dictionary<byte, GameServer>();
        _mirrorsId = new Dictionary<byte, byte>();
    }

    private static async Task SendPacketWithDelay(InternalConnection connection, int delay, InternalPacket message)
    {
        await Task.Delay(delay);
        connection.SendPacket(message);
    }

    private static string ResolveHostName(string host)
    {
        try
        {
            var parsedHost = Dns.GetHostEntry(host);
            foreach (var ipAddress in parsedHost.AddressList)
            {
                // For whatever reason, we can't just access the IsIPv4 property here
                // if (ipAddress.IsIPv4)
                //     return ipAddress.ToString();
                var ipString = ipAddress.ToString();
                if (ipString.Split('.').Length == 4)
                {
                    Logger.Debug($"Resolved {host} to {ipString}");
                    return ipString;
                }
            }
            Logger.Warn($"Unable to resolved {host}");
            return host;
        }
        catch (Exception e)
        {
            // in case of errors, just return it un-parsed
            Logger.Error(e, $"Exception resolving {host}: {e.Message}");
            return host;
        }
    }

    public void Load()
    {
        using (var connection = MySQL.CreateConnection())
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM game_servers WHERE hidden = 0";
                command.Prepare();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var id = reader.GetByte("id");
                        var name = reader.GetString("name");
                        var loadedHost = reader.GetString("host");
                        var host = AppConfiguration.Instance.SkipHostResolve ? loadedHost : ResolveHostName(loadedHost);
                        var port = reader.GetUInt16("port");
                        var gameServer = new GameServer(id, name, host, port);
                        _gameServers.Add(gameServer.Id, gameServer);

                        var extraInfo = host != loadedHost ? "from " + loadedHost :
                            AppConfiguration.Instance.SkipHostResolve ? " (unresolved)" : "";
                        Logger.Info($"Game Server {id}: {name} -> {host}:{port} {extraInfo}");
                    }
                }
            }

            if (_gameServers.Count <= 0)
            {
                Logger.Fatal("No servers have been defined in the game_servers table!");
                return;
            }
        }

        Logger.Info($"Loaded {_gameServers.Count} game server(s)");
    }

    public void Add(byte gsId, List<byte> mirrorsId, InternalConnection connection)
    {
        if (!_gameServers.ContainsKey(gsId))
        {
            Logger.Error($"GameServer connection from {connection.Ip} is requesting an invalid WorldId {gsId}");

            Task.Run(() => SendPacketWithDelay(connection, 5000, new LGRegisterGameServerPacket(GSRegisterResult.Error)));
            // connection.SendPacket(new LGRegisterGameServerPacket(GSRegisterResult.Error));
            return;
        }

        var gameServer = _gameServers[gsId];
        gameServer.Connection = connection;
        gameServer.MirrorsId.AddRange(mirrorsId);
        connection.GameServer = gameServer;
        connection.AddAttribute("gsId", gameServer.Id);
        gameServer.SendPacket(new LGRegisterGameServerPacket(GSRegisterResult.Success));

        foreach (var mirrorId in mirrorsId)
        {
            _gameServers[mirrorId].Connection = connection;
            _mirrorsId.Add(mirrorId, gsId);
        }
        Logger.Info($"Registered GameServer {gameServer.Id} ({gameServer.Name}) from {connection.Ip}");
    }

    public void Remove(byte gsId)
    {
        if (!_gameServers.ContainsKey(gsId))
            return;

        var gameServer = _gameServers[gsId];
        gameServer.Connection = null;

        foreach (var mirrorId in gameServer.MirrorsId)
        {
            if (_gameServers.TryGetValue(mirrorId, out var server))
                server.Connection = null;

            _mirrorsId.Remove(mirrorId);
        }

        gameServer.MirrorsId.Clear();
    }

    public async Task RequestWorldListAsync(LoginConnection connection)
    {
        var gameServers = _gameServers.Values.ToList();
        if (_gameServers.Values.Any(x => x.Active))
        {
            var (requestIds, creationTask) =
                RequestController.Instance.Create(gameServers.Count, 20000); // TODO Request 20s
            for (var i = 0; i < gameServers.Count; i++)
            {
                var value = gameServers[i];
                if (!value.Active)
                {
                    RequestController.Instance.ReleaseId(requestIds[i]);
                    continue;
                }

                var loaded = connection.Characters.ContainsKey(value.Id);
                if (loaded)
                {
                    RequestController.Instance.ReleaseId(requestIds[i]);
                    continue;
                }

                value.SendPacket(
                       new LGRequestInfoPacket(connection.Id, requestIds[i], connection.AccountId));

            }

            await creationTask;
        }
        connection.SendPacket(new ACWorldListPacket(gameServers, connection.GetCharacters()));
    }

    public void SetLoad(byte gsId, byte load)
    {
        lock (_gameServers)
        {
            _gameServers[gsId].Load = (GSLoad)load;
        }
    }

    public void RequestEnterWorld(LoginConnection connection, byte gsId)
    {
        if (!_gameServers.ContainsKey(gsId))
            return;
        var gs = _gameServers[gsId];
        if (!gs.Active)
            return;
        gs.SendPacket(new LGPlayerEnterPacket(connection.AccountId, connection.Id));
    }

    public void EnterWorld(LoginConnection connection, byte gsId, byte result)
    {
        if (result == 0)
        {
            if (_gameServers.TryGetValue(gsId, out var server))
            {
                connection.SendPacket(new ACWorldCookiePacket(connection, server));
            }
            else
            {
                // TODO ...
            }
        }
        else if (result == 1)
        {
            connection.SendPacket(new ACEnterWorldDeniedPacket(0)); // TODO change reason
        }
        else
        {
            // TODO ...
        }
    }
}
