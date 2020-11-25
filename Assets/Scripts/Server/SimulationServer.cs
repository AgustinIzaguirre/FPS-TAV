using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Tests;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.PlayerLoop;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class SimulationServer
{
    private GameObject serverPrefab;
    private Dictionary<int, PlayerInfo> players;
    private Dictionary<int, List<GameInput>> inputsToApply;
    private Channel channel;
    private float timeToSend;
    private float elapsedTime;
    private int sequence;
    private float serverTime;
    private int lastClientId;
    private float eventTimeOut;
    private List<NewPlayerEvent> newPlayerEventSent;
    private List<StartInfoEvent> startInfoSent;
    private Dictionary<int, int> lastShotApplied;

    public SimulationServer(IPEndPoint endPoint, float timeToSend, GameObject serverPrefab)
    {
        channel = new Channel(endPoint.Port);
        players = new Dictionary<int, PlayerInfo>();
        inputsToApply = new Dictionary<int, List<GameInput>>();
        newPlayerEventSent = new List<NewPlayerEvent>();
        startInfoSent = new List<StartInfoEvent>();
        lastShotApplied = new Dictionary<int, int>();
        this.serverPrefab = serverPrefab;
        this.timeToSend = timeToSend;
        sequence = 0;
        elapsedTime = 0f;
        serverTime = 0f;
        lastClientId = 0;
        eventTimeOut = 1f;
        SpawnPositionGenerator.InitializeZones();
    }
    
     public void UpdateServer()
    {
        elapsedTime += Time.deltaTime;
        serverTime += Time.deltaTime;
        ReceivePackets();
        ResendEvents();
        if (elapsedTime >= timeToSend)
        {
            UpdateClients();
            elapsedTime -= timeToSend;
        }
    }

     private void ResendEvents()
     {
         while (newPlayerEventSent.Count > 0 && (serverTime - newPlayerEventSent[0].time) >= eventTimeOut)
         {
             NewPlayerEvent currentEvent = newPlayerEventSent[0];
             int destinationId = currentEvent.destinationId;
             int playerId = currentEvent.playerId;
             SendNewPlayerEvent(playerId, currentEvent.newPlayer, destinationId);
             newPlayerEventSent.RemoveAt(0);
         }
         
         while (startInfoSent.Count > 0 && (serverTime - startInfoSent[0].time) >= eventTimeOut)
         {
             StartInfoEvent currentEvent = startInfoSent[0];
             int destinationId = currentEvent.clientId;
             SendStartInfo(destinationId);
             startInfoSent.RemoveAt(0);
         }
     }

     public void UpdateClients()
     {
         WorldInfo currentWorldInfo = GenerateCurrentWorldInfo();
         
         foreach (var clientId in players.Keys)
         {
             //serialize
             var packet = Packet.Obtain();
             sequence++;
             Snapshot currentSnapshot = new Snapshot(sequence, currentWorldInfo);
             currentSnapshot.Serialize(packet.buffer);
             packet.buffer.Flush();
             channel.Send(packet, players[clientId].endPoint);
             packet.Free();
         }   
     }

     private WorldInfo GenerateCurrentWorldInfo()
     {
         WorldInfo currentWorldInfo = new WorldInfo();
         foreach (var clientId in players.Keys)
         {
             if (players[clientId].isActive)
             {
                 float clientVelocity = players[clientId].GetPlayerGameObject().GetComponent<GravityController>()
                     .GetVerticalVelocity();
                 players[clientId]
                     .SetPlayerEntity(new PlayerEntity(players[clientId].GetPlayerGameObject(), clientVelocity));
             }
             currentWorldInfo.AddPlayer(players[clientId].ClonePlayer());
             players[clientId].SetAnimationState(AnimationStates.IDDLE);
         }

         return currentWorldInfo;
     }

     public void ServerFixedUpdate()
     {
         foreach (var clientId in players.Keys)
         {
             PlayerInfo currentPlayer = players[clientId];
             if (currentPlayer.isActive && currentPlayer.isAlive)
             {
                 GameObject playerObject = currentPlayer.GetPlayerGameObject();
                 Transform clientTransform = playerObject.transform;
                 PlayerMotion.ApplyInputs(0, inputsToApply[clientId],
                     playerObject.GetComponent<CharacterController>(),
                     playerObject.GetComponent<GravityController>(), clientTransform);
                 int lastInputApplied = 0;
                 if (inputsToApply[clientId].Count > 0)
                 {
                     lastInputApplied = inputsToApply[clientId][inputsToApply[clientId].Count - 1].GetInputIntValue();
                 }

                 if (players[clientId].GetAnimationState() == AnimationStates.IDDLE)
                 {
                     players[clientId].SetAnimationState(GetAnimationState(lastInputApplied));
                 }

                 inputsToApply[clientId].Clear();
             }
         }
     }

     private AnimationStates GetAnimationState(int lastInputApplied)
     {
         AnimationStates currentAnimationState = AnimationStates.IDDLE;
         if (lastInputApplied == (int)InputType.LEFT || lastInputApplied == (int)InputType.RIGHT ||
             lastInputApplied == (int)InputType.FORWARD || lastInputApplied == (int)InputType.BACKWARD)
         {
             currentAnimationState = AnimationStates.MOVE;
         }
         return currentAnimationState;
     }

     private void ReceivePackets()
    {
        var packet = channel.GetPacket();

        while (packet != null)
        {
            PacketDispatcher(packet);
            packet.Free();
            packet = channel.GetPacket();
        }
    }

     private void PacketDispatcher(Packet packet)
     {
         int packetType = packet.buffer.GetInt();
         if (packetType == (int) PacketType.INPUT)
         {
             HandleInputMessage(packet);
         }
         else if (packetType == (int) PacketType.EVENT)
         {
             HandleEventMessage(packet);
         }
         else if (packetType == (int) PacketType.SHOOT_EVENT)
         {
             HandleShootEventMessage(packet);
         }
         else if (packetType == (int) PacketType.JOIN_GAME)
         {
             HandleJoinGameMessage(packet);
         }
         else if (packetType == (int) PacketType.NEW_PLAYER)
         {
             HandleNewPlayerMessage(packet);
         }
         else if (packetType == (int) PacketType.START_INFO)
         {
             HandleStartInfoMessage(packet);
         }
     }

     private void HandleStartInfoMessage(Packet packet)
     {
         int clientId = packet.buffer.GetInt();
         Debug.Log("Recieve Start info ACK from client: " + clientId);
         players[clientId].ActivatePlayer();
         Debug.Log("Activate player " + clientId);
         int removeIndex = -1;
         for (int i = 0; i < startInfoSent.Count; i++)
         {
             if (startInfoSent[i].clientId == clientId)
             {
                 removeIndex = i;
             }
         }

         if (removeIndex >= 0)
         {
             startInfoSent.RemoveAt(removeIndex);
         }
     }

     private void HandleNewPlayerMessage(Packet packet)
     {
         int clientId = packet.buffer.GetInt();
         int playerId = packet.buffer.GetInt();
         int removeIndex = -1;
         for (int i = 0; i < newPlayerEventSent.Count; i++)
         {
             NewPlayerEvent currentEvent = newPlayerEventSent[i];
             if (currentEvent.playerId == playerId && currentEvent.destinationId == clientId)
             {
                 removeIndex = i;
             }
         }

         if (removeIndex >= 0)
         {
             newPlayerEventSent.RemoveAt(removeIndex);
         }

         if (clientId == playerId && !players[clientId].isActive)
         {
             SendStartInfo(clientId);
         }
     }

     private void HandleJoinGameMessage(Packet packet)
     {
         int clientId = packet.buffer.GetInt();
         var clientEndPoint = packet.fromEndPoint;
         if (!IsEndpointInUse(clientEndPoint))
         {
             clientId = lastClientId + 1;
             Debug.Log("player " + clientId + " joining game");
             lastClientId += 1;
             if (!players.ContainsKey(clientId))
             {
                 players[clientId] = new PlayerInfo(clientId, clientEndPoint);
                 inputsToApply[clientId] = new List<GameInput>();
                 SendAck(lastClientId, PacketType.JOIN_GAME, players[clientId].endPoint);
                 GenerateNewPlayer(clientId);
                 Debug.Log("Deactivate client = " + clientId);
                 players[clientId].DeactivatePlayer();
             }
         }
     }

     private void HandleShootEventMessage(Packet packet)
     {
         int clientId = packet.buffer.GetInt();
         if (players[clientId].isActive)
         {
             PlayerInfo currentClient = players[clientId];
             currentClient.SetAnimationState(AnimationStates.SHOOT);
             ShootEvent currentShootEvent = ShootEvent.Deserialize(packet.buffer);
             if (!lastShotApplied.ContainsKey(clientId) ||
                 lastShotApplied[clientId] < currentShootEvent.shootEventNumber)
             {
                 lastShotApplied[clientId] = currentShootEvent.shootEventNumber;
                 players[currentShootEvent.targetId].IsShootedBy(players[currentShootEvent.shooterId]);
                 if (players[currentShootEvent.targetId].life <= 0.001f)
                 {
                     players[currentShootEvent.targetId].MarkAsDead();
                     GameObject.Destroy(players[currentShootEvent.targetId].playerGameObject);
                 }
             }
             SendAck(currentShootEvent.shootEventNumber, PacketType.SHOOT_EVENT, currentClient.endPoint);
         }
     }

     private void HandleEventMessage(Packet packet)
     {
         int clientId = packet.buffer.GetInt();
         if (players[clientId].isActive)
         {
             PlayerInfo currentClient = players[clientId];
             GameEvent currentEvent = GameEvent.Deserialize(packet.buffer);
             SendAck(currentEvent.eventNumber, PacketType.EVENT, currentClient.endPoint);
         }
     }

     private void HandleInputMessage(Packet packet)
     {
        int clientId = packet.buffer.GetInt();
        if (players[clientId] != null && players[clientId].isActive)
        {
            PlayerInfo currentPlayer = players[clientId];
            int startInput = packet.buffer.GetInt();
            List<GameInput> inputsToExecute = GameInput.Deserialize(packet.buffer);
            int ackNumber = packet.buffer.GetInt();
            SendAck(ackNumber, PacketType.ACK, currentPlayer.endPoint);
            int firstInput = Math.Max(currentPlayer.lastInputApplied + 1 - startInput, 0);
            if (inputsToApply[clientId] == null)
            {
                inputsToApply[clientId] = new List<GameInput>();
            }
            for (int i = firstInput; i < inputsToExecute.Count; i++)
            {
                inputsToApply[clientId].Add(inputsToExecute[i]);
            }
            currentPlayer.lastInputApplied = ackNumber;
        }
     }

     private bool IsEndpointInUse(IPEndPoint clientEndPoint)
     {
         foreach (var clientId in players.Keys)
         {
             if (players[clientId].endPoint.Equals(clientEndPoint))
             {
                 if (players[clientId].isAlive)
                 {
                     return true;
                 }
             }
         }
         return false;
     }

     private void SendStartInfo(int destinationId)
     {
         PlayerInfo currentPlayer = players[destinationId];
         IPEndPoint clientEndpoint = currentPlayer.endPoint;
         var packet = Packet.Obtain();
         packet.buffer.PutInt((int) PacketType.START_INFO);
         StartInfoEvent newPlayerEvent = new StartInfoEvent(destinationId, serverTime);
         WorldInfo currentWorldInfo = GenerateCurrentWorldInfo();
         currentWorldInfo.Serialize(packet.buffer);
         packet.buffer.Flush();
         channel.Send(packet, clientEndpoint);
         startInfoSent.Add(newPlayerEvent);
         packet.Free(); //TODO check if remove
     }

     private void SendNewPlayerEventToAllPlayers(int playerId, Vector3 position, Vector3 rotation)
     { 
         PlayerEntity newPlayer = new PlayerEntity(players[playerId].GetPlayerGameObject(), position, rotation);
         foreach (var id in players.Keys)
         { 
             SendNewPlayerEvent(playerId, newPlayer, id); 
         }
     }

     private void SendNewPlayerEvent(int playerId, PlayerEntity newPlayer, int destinationId)
     {
         PlayerInfo currentPlayer = players[destinationId];
         IPEndPoint clientEndpoint = currentPlayer.endPoint;
         var packet = Packet.Obtain();
         packet.buffer.PutInt((int) PacketType.NEW_PLAYER);
         NewPlayerEvent newPlayerEvent = new NewPlayerEvent(playerId, newPlayer, serverTime, destinationId);
         newPlayerEvent.Serialize(packet.buffer);
         packet.buffer.Flush();
         channel.Send(packet, clientEndpoint);
         newPlayerEventSent.Add(newPlayerEvent);
         packet.Free();  // TODO check if remove
     }

     private void GenerateNewPlayer(int clientId)
     {
         
//         float xPosition = Random.Range(-4f, 4f);
//         float yPosition = 1f;
//         float zPosition = Random.Range(-4f, 4f);
//         Vector3 position = new Vector3(xPosition, yPosition, zPosition);
         Vector3 position = SpawnPositionGenerator.GetSpawningPosition();
         Quaternion rotation = Quaternion.Euler(Vector3.zero);
         GameObject newPlayer = GameObject.Instantiate(serverPrefab, position, rotation);
         players[clientId].SetPlayerGameObject(newPlayer);
         SendNewPlayerEventToAllPlayers(clientId, position, rotation.eulerAngles);
     }

     private void SendAck(int ackNumber, PacketType packetType, IPEndPoint endPoint)
    {
        var packet = Packet.Obtain();
        packet.buffer.PutInt((int) packetType);
        packet.buffer.PutInt(ackNumber);
        packet.buffer.Flush();
        channel.Send(packet, endPoint);
        packet.Free();
    }

     public Channel GetChannel()
    {
        return channel;
    }
    
    public void DestroyChannel() {
        channel.Disconnect();
    }
}
