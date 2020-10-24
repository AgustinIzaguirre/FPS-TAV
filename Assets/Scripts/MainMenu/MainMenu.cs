using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public String gameScene;
    public GameObject inputModal; 
    private int serverPort;
    private String serverAddress;
    
    void Start()
    {
        serverPort = 9000;
        serverAddress = "127.0.0.1";
    }
    public void HostGame()
    {
        GameConfig.SetGameMode(GameMode.SERVER);
        OpenInputModal();
//        GameConfig.ConfigureGame(serverPort, serverAddress, GameMode.SERVER);
//        SceneManager.LoadScene(gameScene);
    }

    public void JoinGame()
    {
        GameConfig.SetGameMode(GameMode.CLIENT);
//        GameConfig.ConfigureGame(serverPort,serverAddress, GameMode.CLIENT);
//        SceneManager.LoadScene(gameScene);
        inputModal.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
    
    private void OpenInputModal()
    {
        inputModal.SetActive(true);
    }
    
    public void CloseInputModal()
    {
        inputModal.SetActive(false);
    }

    public void LoadGame()
    {
        SceneManager.LoadScene(gameScene);
    }
}
