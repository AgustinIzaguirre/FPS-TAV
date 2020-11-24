using System.Runtime.CompilerServices;
using UnityEngine;

public class ClientConfig
{
    private static int id;
    private static int clientPort;
    private static float timeToSend;
    private static int minSnapshots;
    private static float timeout;
    private static Channel clientChannel;
    private static PlayerInfo playerInfo;
    private static GameObject playerPrediction;
    private static GameObject bulletTrailPrefab;

    public static void ConfigureClient(int id, int clientPort, float timeToSend, int minSnapshots, float timeout,
        Channel clientChannel, GameObject bulletTrailPrefab)
    {
        ClientConfig.id = id;
        ClientConfig.clientPort = clientPort;
        ClientConfig.timeToSend = timeToSend;
        ClientConfig.minSnapshots = minSnapshots;
        ClientConfig.timeout = timeout;
        ClientConfig.clientChannel = clientChannel;
        ClientConfig.playerInfo = null;
        ClientConfig.bulletTrailPrefab = bulletTrailPrefab;
    }

    public static void SetPlayerInfo(PlayerInfo playerInfo)
    {
        ClientConfig.playerInfo = playerInfo;
    }
    
    public static void SetPlayerPrediction(GameObject playerPrediction)
    {
        ClientConfig.playerPrediction = playerPrediction;
    }

    public static void SetId(int id)
    {
        ClientConfig.id = id;
    }
    
    public static int GetPort()
    {
        return clientPort;
    }

    public static float GetTimeToSend()
    {
        return timeToSend;
    }

    public static int GetMinSnapshots()
    {
        return minSnapshots;
    }

    public static int GetId()
    {
        return id;
    }
    
    public static float GetTimeout()
    {
        return timeout;
    }
    
    public static Channel GetChannel()
    {
        return clientChannel;
    }
    
    public static PlayerInfo GetPlayerInfo()
    {
        return playerInfo;
    }
    
    public static GameObject GetPlayerPrediction()
    {
        return playerPrediction;
    }
    
    public static GameObject GetBulletTrailPrefab()
    {
        return bulletTrailPrefab;
    }
    
    

}

