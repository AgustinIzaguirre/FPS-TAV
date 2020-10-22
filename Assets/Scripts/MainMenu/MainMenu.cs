using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public String gameScene;
    private int serverPort;
    private String serverAddress;

    void Start()
    {
        serverPort = 9000;
        serverAddress = "127.0.0.1";
    }
    public void HostGame()
    {
        GameConfig.ConfigureGame(serverPort, serverAddress, GameMode.SERVER);
        SceneManager.LoadScene(gameScene);
    }

    public void JoinGame()
    {
        GameConfig.ConfigureGame(serverPort,serverAddress, GameMode.CLIENT);
        SceneManager.LoadScene(gameScene);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}
