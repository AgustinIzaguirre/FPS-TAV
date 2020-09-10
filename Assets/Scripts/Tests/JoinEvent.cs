public class JoinEvent
{
    public int clientId;
    public float time;
    public JoinEvent(int clientId, float time)
    {
        this.clientId = clientId;
        this.time = time;
    }    
}
