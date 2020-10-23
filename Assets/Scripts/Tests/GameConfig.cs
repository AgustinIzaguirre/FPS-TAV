using System;
using System.Net;

public class GameConfig
{
    private static int serverPort = -1;
    private static String serverAddress = null;
    private static GameMode gameMode = GameMode.BOTH;
    private static IPEndPoint serverEndPoint = null;
    private static Channel serverChannel = null;

    public static void ConfigureGame(int serverPort, String serverAddress, GameMode gameMode)
    {
        GameConfig.serverPort = serverPort;
        GameConfig.serverAddress = serverAddress;
        GameConfig.gameMode = gameMode;
        serverEndPoint = new IPEndPoint(IPAddress.Parse(serverAddress), serverPort);
    }

    public static int GetServerPort()
    {
        return serverPort;
    }
    
    public static String GetServerAddress()
    {
        return serverAddress;
    }

    public static IPEndPoint GetServerEndPoint()
    {
        return serverEndPoint;
    }

    public static Channel GetServerChannel()
    {
        return serverChannel;
    }

    public static void SetServerChannel(Channel serverChannel)
    {
        GameConfig.serverChannel = serverChannel;
    }
    public static GameMode GetGameMode()
    {
        return gameMode;
    }
}
