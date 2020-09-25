using System.Collections.Generic;

public class WorldInfo
{
    public Dictionary<int, CubeEntity> players;
    public Dictionary<int, int> playerAppliedInputs;

    public WorldInfo()
    {
        players = new Dictionary<int, CubeEntity>();
        playerAppliedInputs = new Dictionary<int, int>();
    }

    private WorldInfo(Dictionary<int, CubeEntity> players, Dictionary<int, int> playerAppliedInputs)
    {
        this.players = players;
        this.playerAppliedInputs = playerAppliedInputs;
    }

    public void AddPlayer(int playerId, CubeEntity player, int lastInput)
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
            buffer.PutInt(playerAppliedInputs[playerId]);
            players[playerId].Serialize(buffer);
        }
    }

    public static WorldInfo Deserialize(BitBuffer buffer)
    {
        int quantity = buffer.GetInt();
        Dictionary<int, CubeEntity> currentPlayers = new Dictionary<int, CubeEntity>();
        Dictionary<int, int> appliedInputs = new Dictionary<int, int>();
        for (int i = 0; i < quantity; i++)
        {
            int playerId = buffer.GetInt();
            int lastAppliedInput = buffer.GetInt();
            appliedInputs[playerId] = lastAppliedInput;
            CubeEntity player = CubeEntity.DeserializeInfo(buffer);
            currentPlayers[playerId] = player;
        }
        return new WorldInfo(currentPlayers, appliedInputs);
    }
}
