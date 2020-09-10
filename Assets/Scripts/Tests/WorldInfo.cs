using System.Collections.Generic;

public class WorldInfo
{
    private Dictionary<int, CubeEntity> players;

    public WorldInfo()
    {
        players = new Dictionary<int, CubeEntity>();
    }

    public void addPlayer(int playerId, CubeEntity player)
    {
        players[playerId] = player;
    }

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(players.Count);
        foreach (var playerId in players.Keys)
        {
            buffer.PutInt(playerId);
            players[playerId].Serialize(buffer);
        }
    }

    public static Dictionary<int, CubeEntity> Deserialize(BitBuffer buffer)
    {
        int quantity = buffer.GetInt();
        Dictionary<int, CubeEntity> currentPlayers = new Dictionary<int, CubeEntity>();
        for (int i = 0; i < quantity; i++)
        {
            int playerId = buffer.GetInt();
//            CubeEntity currentPlayer = new CubeEntity();
        }

        return currentPlayers;
    }
}
