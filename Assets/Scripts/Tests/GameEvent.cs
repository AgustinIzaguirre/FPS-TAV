public class GameEvent
{
    public string name;
    public int value;
    public float time;
    public int eventNumber;
    public GameEvent(string name, int value, float time, int eventNumber)
    {
        this.name = name;
        this.value = value;
        this.time = time;
        this.eventNumber = eventNumber;
    }
    
    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt((int) PacketType.EVENT);
        buffer.PutInt(eventNumber);
        buffer.PutInt(value);
        buffer.PutFloat(time);
    }

    public static GameEvent Deserialize(BitBuffer buffer)
    {
        int eventNumber = buffer.GetInt();
        int value = buffer.GetInt();
        float time = buffer.GetFloat();
        return new GameEvent("", value, time, eventNumber);
    }
}
