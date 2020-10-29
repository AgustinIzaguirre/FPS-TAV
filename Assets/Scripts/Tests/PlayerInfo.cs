using System.Net;
using UnityEngine;

public class PlayerInfo
{
    public static float MIN_DAMAGE = 0f;
    public static float MAX_DAMAGE = 100f;
    public static float MIN_LIFE = 0f;
    public static float MAX_LIFE = 100f;

    public int id;
    public GameObject playerGameObject;
    public IPEndPoint endPoint;
    public int lastInputApplied;
    public float life;
    public float damage;
    public bool isShooting;
    public bool isActive;

    public PlayerInfo(int id, IPEndPoint endPoint)
    {
        this.id = id;
        this.endPoint = endPoint;
        this.life = MAX_LIFE;
        this.damage = MAX_DAMAGE / 3;
        this.lastInputApplied = 0;
        this.isShooting = false;
        this.isActive = false;
    }
    public void SetPlayerGameObject(GameObject playerGameObject)
    {
        this.playerGameObject = playerGameObject;
    }

    public void ActivatePlayer()
    {
        isActive = true;
    }

    public void DeactivatePlayer()
    {
        isActive = false;
    }

    public GameObject GetPlayerGameObject()
    {
        return playerGameObject;
    }
}