using System;
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
    public PlayerEntity playerEntity;

    public PlayerInfo(int id, IPEndPoint endPoint)
    {
        this.id = id;
        this.endPoint = endPoint;
        life = MAX_LIFE;
        damage = MAX_DAMAGE / 3;
        lastInputApplied = 0;
        isShooting = false;
        isActive = false;
        playerEntity = null;
    }

    public PlayerInfo(int playerId, int playerLastAppliedInput, PlayerEntity currentPlayerEntity, float playerLife,
        float playerDamage, bool isPlayerShooting)
    {
        id = playerId;
        lastInputApplied = playerLastAppliedInput;
        playerEntity = currentPlayerEntity;
        playerGameObject = playerEntity.playerObject;
        life = playerLife;
        damage = playerDamage;
        isShooting = isPlayerShooting;
    }
    
    public PlayerInfo(int playerId, PlayerEntity playerEntity, bool isActive)
    {
        id = playerId;
        lastInputApplied = 0;
        this.playerEntity = playerEntity;
        playerGameObject = playerEntity.playerObject;
        life = MAX_LIFE;
        damage = MAX_DAMAGE / 3;
        isShooting = false;
        this.isActive = isActive;
    }

    public void SetPlayerGameObject(GameObject playerGameObject)
    {
        this.playerGameObject = playerGameObject;
        playerEntity = new PlayerEntity(playerGameObject);
    }

    public void SetPlayerEntity(PlayerEntity playerEntity)
    {
        this.playerEntity = playerEntity;
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

    public void IsShootedBy(PlayerInfo shooter)
    {
        life = Math.Max(0f, life - shooter.damage);
    }
    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(id);
        buffer.PutInt(lastInputApplied);
        playerEntity.Serialize(buffer);
        FloatSerializer.SerializeFloat(buffer, life, (int)MIN_LIFE, (int)MAX_LIFE, 0.2f);
        FloatSerializer.SerializeFloat(buffer, damage, (int)MIN_DAMAGE, (int)MAX_DAMAGE, 0.2f);
        int shootingValue = isShooting ? 1 : 0;
        buffer.PutInt(shootingValue, 0, 1); 
    }
    
    public static PlayerInfo Deserialize(BitBuffer buffer)
    {
        int playerId = buffer.GetInt();
        int playerLastAppliedInput = buffer.GetInt();
        PlayerEntity currentPlayerEntity= PlayerEntity.DeserializeInfo(buffer);
        float playerLife = FloatSerializer.DeserializeFloat(buffer, (int)MIN_LIFE, (int)MAX_LIFE, 0.2f);
        float playerDamage = FloatSerializer.DeserializeFloat(buffer, (int)MIN_DAMAGE, (int)MAX_DAMAGE, 0.2f);
        bool isPlayerShooting = buffer.GetInt(0, 1) == 1 ? true : false;
        return new PlayerInfo(playerId, playerLastAppliedInput, currentPlayerEntity, playerLife, playerDamage, isPlayerShooting);
    }
}