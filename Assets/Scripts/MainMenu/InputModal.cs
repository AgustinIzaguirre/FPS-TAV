using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Text.RegularExpressions; 
using UnityEngine.SceneManagement;


public class InputModal : MonoBehaviour
{
    public InputField serverAddressField;
    public InputField serverPortField;
    public GameObject addressError;
    public GameObject portError;
    public String gameScene;

    private String serverAddress;
    private int serverPort;

    
    public bool ValidateServerAddress()
    {
        Debug.Log(serverAddressField.text);
        string addressRegex = @"(^[0-9]{1,4}(.)[0-9]{1,4}(.)[0-9]{1,4}(.)[0-9]{1,4}$)";
        Regex re = new Regex(addressRegex);
        if (re.IsMatch(serverAddressField.text))
        {
            serverAddress = serverAddressField.text;
            return true;
        }
        else
        {
            addressError.SetActive(true);
            return false;
        }
            
    }

    public bool ValidateServerPort()
    {
        string portRegex = @"(^[0-9]{1,5}$)";
        Regex re = new Regex(portRegex);
        if (re.IsMatch(serverPortField.text))
        {
//            int serverPort = serverPortField.text;
            return true;
        }
        else
        {
            Debug.Log("Port no matchea"); 
            portError.SetActive(true);
            return false;
        }
    }

    public void ValidateFields()
    {
        bool isAddressValid = ValidateServerAddress();
        bool isPortValid = ValidateServerPort();
        if (isAddressValid && isPortValid)
        {
            addressError.SetActive(false);
            portError.SetActive(false);
            GameConfig.SetServerEndPoint(serverAddress, serverPort);
            LoadGame();
        }
    }
    
    public void LoadGame()
    {
        SceneManager.LoadScene(gameScene);
    }

}
