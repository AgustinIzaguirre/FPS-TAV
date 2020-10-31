public class ShootEvent
{
    public int shooterId;
    public int targetId;
    public float time;
    public int shootEventNumber;
    
    public ShootEvent(int shooterId, int targetId, float time, int shootEventNumber)
    {
        this.shooterId = shooterId;
        this.targetId = targetId;
        this.time = time;
        this.shootEventNumber = shootEventNumber;
    }
    
    public void Serialize(BitBuffer buffer, int clientId)
    {
        buffer.PutInt((int) PacketType.SHOOT_EVENT);
        buffer.PutInt(clientId);
        buffer.PutInt(shootEventNumber);
        buffer.PutInt(shooterId);
        buffer.PutInt(targetId);
        buffer.PutFloat(time);
    }

    public static ShootEvent Deserialize(BitBuffer buffer)
    {
        int shootEventNumber = buffer.GetInt();
        int shooterId = buffer.GetInt();
        int targetId = buffer.GetInt();
        float time = buffer.GetFloat();
        return new ShootEvent(shooterId, targetId, time, shootEventNumber);
    }
}