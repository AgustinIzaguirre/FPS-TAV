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
    public bool isAlive;
    public PlayerEntity playerEntity;
    public AnimationStates animationState;

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
        isAlive = true;
        animationState = AnimationStates.IDDLE;
    }

    public PlayerInfo(int playerId, int playerLastAppliedInput, PlayerEntity currentPlayerEntity, float playerLife,
        float playerDamage, bool isPlayerShooting, AnimationStates animationState, bool isAlive)
    {
        id = playerId;
        lastInputApplied = playerLastAppliedInput;
        playerEntity = null;
        playerGameObject = null;
        if (isAlive)
        {
            playerEntity = currentPlayerEntity;
            playerGameObject = playerEntity.playerObject;
        }
        life = playerLife;
        damage = playerDamage;
        isShooting = isPlayerShooting;
        this.animationState = animationState;
        this.isAlive = isAlive;
        this.isActive = isAlive;
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
        isAlive = true;
        animationState = AnimationStates.IDDLE;
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
        if (life <= 0.001)
        {
            isAlive = false;
        }
    }
    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(id);
        buffer.PutInt(lastInputApplied);
        int isAliveValue = isAlive ? 1 : 0;
        buffer.PutInt(isAliveValue, 0, 1);
        if (isAlive)
        {
            playerEntity.Serialize(buffer);
        }
        FloatSerializer.SerializeFloat(buffer, life, (int)MIN_LIFE, (int)MAX_LIFE, 0.2f);
        FloatSerializer.SerializeFloat(buffer, damage, (int)MIN_DAMAGE, (int)MAX_DAMAGE, 0.2f);
        int shootingValue = isShooting ? 1 : 0;
        buffer.PutInt(shootingValue, 0, 1); //TODO remove shootingValue
        buffer.PutInt((int)animationState, 0, 3);
    }
    
    public static PlayerInfo Deserialize(BitBuffer buffer)
    {
        int playerId = buffer.GetInt();
        int playerLastAppliedInput = buffer.GetInt();
        PlayerEntity currentPlayerEntity = null;
        bool isPlayerAlive = buffer.GetInt(0, 1) == 1;
        if (isPlayerAlive)
        {
            currentPlayerEntity = PlayerEntity.DeserializeInfo(buffer);
        }

        float playerLife = FloatSerializer.DeserializeFloat(buffer, (int)MIN_LIFE, (int)MAX_LIFE, 0.2f);
        float playerDamage = FloatSerializer.DeserializeFloat(buffer, (int)MIN_DAMAGE, (int)MAX_DAMAGE, 0.2f);
        bool isPlayerShooting = buffer.GetInt(0, 1) == 1;
        AnimationStates currentAnimationState = GetAnimationState(buffer.GetInt(0, 3));
        return new PlayerInfo(playerId, playerLastAppliedInput, currentPlayerEntity, playerLife, playerDamage,
            isPlayerShooting, currentAnimationState, isPlayerAlive);
    }

    private static AnimationStates GetAnimationState(int animationValue)
    {
        AnimationStates currentAnimationState = AnimationStates.IDDLE;
        if (animationValue == 1)
        {
            currentAnimationState = AnimationStates.MOVE;
        }
        if (animationValue == 2)
        {
            currentAnimationState = AnimationStates.SHOOT;
        }
        if (animationValue == 3)
        {
            currentAnimationState = AnimationStates.DEAD;
        }

        return currentAnimationState;
    }

    public void MarkAsDead()
    {
        isAlive = false;
        isActive = false;
    }

    public void SetAnimationState(AnimationStates currentAnimationState)
    {
        animationState = currentAnimationState;
    }
    
    public AnimationStates GetAnimationState()
    {
        return animationState;
    }
}