using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Tests;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.PlayerLoop;
using Debug = UnityEngine.Debug;

public class SimulationServer
{
    private GameObject serverPrefab;
    private Dictionary<int, GameObject> clientsCubes;
    private Dictionary<int, ClientInfo> clients;
    private Dictionary<int, bool> activePlayers;
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

    public SimulationServer(IPEndPoint endPoint, float timeToSend, GameObject serverPrefab)
    {
        channel = new Channel(endPoint.Port);
        clientsCubes = new Dictionary<int, GameObject>();
        clients = new Dictionary<int, ClientInfo>();
        activePlayers = new Dictionary<int, bool>();
        inputsToApply = new Dictionary<int, List<GameInput>>();
        newPlayerEventSent = new List<NewPlayerEvent>();
        startInfoSent = new List<StartInfoEvent>();
        this.serverPrefab = serverPrefab;
        this.timeToSend = timeToSend;
        sequence = 0;
        elapsedTime = 0f;
        serverTime = 0f;
        lastClientId = 0;
        eventTimeOut = 1f;
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
         
         foreach (var clientId in clients.Keys)
         {
             //serialize
             var packet = Packet.Obtain();
             sequence++;
             Snapshot currentSnapshot = new Snapshot(sequence, currentWorldInfo);
             currentSnapshot.Serialize(packet.buffer);
             packet.buffer.Flush();
             channel.Send(packet, clients[clientId].endPoint);
             packet.Free();
         }   
     }

     private WorldInfo GenerateCurrentWorldInfo()
     {
         WorldInfo currentWorldInfo = new WorldInfo();
         foreach (var clientId in clientsCubes.Keys)
         {
             float clientVelocity = clientsCubes[clientId].GetComponent<GravityController>().GetVerticalVelocity();
             CubeEntity clientEntity = new CubeEntity(clientsCubes[clientId], clientVelocity);
             currentWorldInfo.AddPlayer(clientId, clientEntity, clients[clientId].lastInputApplied);
         }

         return currentWorldInfo;
     }

     public void ServerFixedUpdate()
     {
         foreach (var clientId in clients.Keys)
         {
             Transform clientTransform = clientsCubes[clientId].transform;
             PlayerMotion.ApplyInputs(0, inputsToApply[clientId],
                     clientsCubes[clientId].GetComponent<CharacterController>(),
                     clientsCubes[clientId].GetComponent<GravityController>(), clientTransform);
             inputsToApply[clientId].Clear();
         }
     }
     
     private void ReceivePackets()
    {
        var packet = channel.GetPacket();

        while (packet != null)
        {
            int packetType = packet.buffer.GetInt();
            if (packetType == (int) PacketType.INPUT)
            {
                int clientId = packet.buffer.GetInt();
                if (activePlayers[clientId])
                {
                    ClientInfo currentClient = clients[clientId];
                    int startInput = packet.buffer.GetInt();
                    List<GameInput> inputsToExecute = GameInput.Deserialize(packet.buffer);
                    int ackNumber = packet.buffer.GetInt();
                    SendAck(ackNumber, PacketType.ACK, currentClient.endPoint);
                    int firstInput = currentClient.lastInputApplied + 1 - startInput;
                    //TODO separate on function
                    if (inputsToApply[clientId] == null)
                    {
                        inputsToApply[clientId] = new List<GameInput>();
                    }
                    for (int i = firstInput; i < inputsToExecute.Count; i++)
                    {
                        inputsToApply[clientId].Add(inputsToExecute[i]);
                    }
                    currentClient.lastInputApplied = ackNumber;
                }
            }
            else if (packetType == (int) PacketType.EVENT)
            {
                 // handle event   
                 int clientId = packet.buffer.GetInt();
                 if (activePlayers[clientId])
                 {
                     ClientInfo currentClient = clients[clientId];
                     GameEvent currentEvent = GameEvent.Deserialize(packet.buffer);
                     SendAck(currentEvent.eventNumber, PacketType.EVENT, currentClient.endPoint);
                 }
            }
            else if (packetType == (int) PacketType.JOIN_GAME)
            {
                int clientId = packet.buffer.GetInt();
                clientId = lastClientId + 1;
                lastClientId += 1;
                var clientEndPoint = packet.fromEndPoint;
                if (!clients.ContainsKey(clientId))
                {
                    clients[clientId] = new ClientInfo(clientId, clientEndPoint);
                    inputsToApply[clientId] = new List<GameInput>();
                    SendAck(lastClientId, PacketType.JOIN_GAME, clients[clientId].endPoint);
                    GenerateNewPlayer(clientId);
                    activePlayers[clientId] = false;
                }
            }
            else if (packetType == (int) PacketType.NEW_PLAYER)
            {
                int clientId = packet.buffer.GetInt();
                int playerId = packet.buffer.GetInt();
                int removeIndex = 0;
                for (int i = 0; i < newPlayerEventSent.Count; i++)
                {
                    NewPlayerEvent currentEvent = newPlayerEventSent[i];
                    if (currentEvent.playerId == playerId && currentEvent.destinationId == clientId)
                    {
                        removeIndex = i;
                    }
                }
                newPlayerEventSent.RemoveAt(removeIndex);
                if (clientId == playerId && !activePlayers[clientId])
                {
                    SendStartInfo(clientId);
                    // sendNewWorldInfo and wait ack then activate player TODO remove i think
                }
            }
            else if (packetType == (int) PacketType.START_INFO)
            {
                int clientId = packet.buffer.GetInt();
                activePlayers[clientId] = true;
                int removeIndex = -1;
                for (int i = 0; i < startInfoSent.Count; i++)
                {
                    if (startInfoSent[i].clientId == clientId)
                    {
                        removeIndex = i;
                    }
                }
                startInfoSent.RemoveAt(removeIndex);
            }
            packet.Free();
            packet = channel.GetPacket();
        }
    }

     private void SendStartInfo(int destinationId)
     {
         ClientInfo currentPlayer = clients[destinationId];
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
        CubeEntity newPlayer = new CubeEntity(clientsCubes[playerId], position, rotation);
        foreach (var id in clients.Keys)
        {
            SendNewPlayerEvent(playerId, newPlayer, id);
        }
     }

     private void SendNewPlayerEvent(int playerId, CubeEntity newPlayer, int destinationId)
     {
         ClientInfo currentPlayer = clients[destinationId];
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
         float xPosition = Random.Range(-4f, 4f);
         float yPosition = 1f;
         float zPosition = Random.Range(-4f, 4f);
         Vector3 position = new Vector3(xPosition, yPosition, zPosition);
         Quaternion rotation = Quaternion.Euler(Vector3.zero);
         GameObject newCube = GameObject.Instantiate(serverPrefab, position, rotation);
         clientsCubes[clientId] = newCube;
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
