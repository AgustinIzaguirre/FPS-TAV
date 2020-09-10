using System.Net;

public class ClientInfo
{
    public int id;
    public IPEndPoint endPoint;
    public int lastInputApplied;

    public ClientInfo(int id, IPEndPoint endPoint)
    {
        this.id = id;
        this.endPoint = endPoint;
        lastInputApplied = 0;
    }
}
