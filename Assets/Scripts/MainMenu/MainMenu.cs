using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public String gameScene;

    public void HostGame()
    {
        Debug.Log("Host game not implemented yet");
    }

    public void JoinGame()
    {
        GameConfig.ConfigureGame(9000,"127.0.0.1", GameMode.CLIENT);
        SceneManager.LoadScene(gameScene);
//        Debug.Log("Join game not implemented yet");
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}
