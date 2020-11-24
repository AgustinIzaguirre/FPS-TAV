using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGameMenu : MonoBehaviour
{
    public String mainMenuScene;
    public String gameScene;
    
    public void TryAgain()
    {
        SceneManager.LoadScene(gameScene);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(mainMenuScene);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
