using System.Collections.Generic;
using UnityEngine;

public class WorldInfo
{
    public Dictionary<int, PlayerEntity> players;
    public Dictionary<int, int> playerAppliedInputs;

    public WorldInfo()
    {
        players = new Dictionary<int, PlayerEntity>();
        playerAppliedInputs = new Dictionary<int, int>();
    }

    private WorldInfo(Dictionary<int, PlayerEntity> players, Dictionary<int, int> playerAppliedInputs)
    {
        this.players = players;
        this.playerAppliedInputs = playerAppliedInputs;
    }

    public void AddPlayer(int playerId, PlayerEntity player, int lastInput)
    {
        players[playerId] = player;
        playerAppliedInputs[playerId] = lastInput;
    }

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(players.Count);
        foreach (var playerId in players.Keys)
        {
            buffer.PutInt(playerId);
            Debug.Log("Serializing playerId = " + playerId);
            buffer.PutInt(playerAppliedInputs[playerId]);
            players[playerId].Serialize(buffer);
        }
    }

    public static WorldInfo Deserialize(BitBuffer buffer)
    {
        int quantity = buffer.GetInt();
        Dictionary<int, PlayerEntity> currentPlayers = new Dictionary<int, PlayerEntity>();
        Dictionary<int, int> appliedInputs = new Dictionary<int, int>();
        for (int i = 0; i < quantity; i++)
        {
            int playerId = buffer.GetInt();
            int lastAppliedInput = buffer.GetInt();
            appliedInputs[playerId] = lastAppliedInput;
            PlayerEntity player = PlayerEntity.DeserializeInfo(buffer);
            currentPlayers[playerId] = player;
        }
        return new WorldInfo(currentPlayers, appliedInputs);
    }
}
