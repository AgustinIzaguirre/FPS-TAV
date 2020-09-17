public class NewPlayerEvent
{
    public CubeEntity newPlayer;
    public float time;
    public int playerId;
    public int destinationId;
    public NewPlayerEvent(int playerId, CubeEntity newPlayer, float time, int destinationId)
    {
        this.playerId = playerId;
        this.newPlayer = newPlayer;
        this.time = time;
        this.destinationId = destinationId;
    }

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(playerId);
        buffer.PutFloat(time);
        newPlayer.Serialize(buffer);
    }

    public static NewPlayerEvent Deserialize(BitBuffer buffer)
    {
        int playerId = buffer.GetInt();
        float time = buffer.GetFloat();
        CubeEntity player = new CubeEntity(null);
        player.Deserialize(buffer);
        return new NewPlayerEvent(playerId, player, time, -1);
    }
}
