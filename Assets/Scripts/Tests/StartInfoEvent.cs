public class StartInfoEvent
{
    public int clientId;
    public float time;
    public StartInfoEvent(int clientId, float time)
    {
        this.clientId = clientId;
        this.time = time;
    }    
}