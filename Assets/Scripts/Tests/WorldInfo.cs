using System.Collections.Generic;
using UnityEngine;

public class WorldInfo
{
    public Dictionary<int, PlayerInfo> players;
    public WorldInfo()
    {
        players = new Dictionary<int, PlayerInfo>();
    }

    private WorldInfo(Dictionary<int, PlayerInfo> players)
    {
        this.players = players;
    }

    public void AddPlayer(PlayerInfo player)
    {
        players[player.id] = player;
    }

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(players.Count);
        foreach (var playerId in players.Keys)
        {
            players[playerId].Serialize(buffer);
        }
    }

    public static WorldInfo Deserialize(BitBuffer buffer)
    {
        int quantity = buffer.GetInt();
        Dictionary<int, PlayerInfo> currentPlayers = new Dictionary<int, PlayerInfo>();
        for (int i = 0; i < quantity; i++)
        {
            PlayerInfo player = PlayerInfo.Deserialize(buffer);
            currentPlayers[player.id] = player;
        }
        return new WorldInfo(currentPlayers);
    }
}
