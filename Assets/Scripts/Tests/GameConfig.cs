using System;

public class GameConfig
{
    private static int serverPort = -1;
    private static String serverAddress = null;
    private static GameMode gameMode = GameMode.BOTH;

    public static void ConfigureGame(int serverPort, String serverAddress, GameMode gameMode)
    {
        GameConfig.serverPort = serverPort;
        GameConfig.serverAddress = serverAddress;
        GameConfig.gameMode = gameMode;
    }

    public static int GetServerPort()
    {
        return serverPort;
    }
    
    public static String GetServerAddress()
    {
        return serverAddress;
    }

    public static GameMode GetGameMode()
    {
        return gameMode;
    }
}
