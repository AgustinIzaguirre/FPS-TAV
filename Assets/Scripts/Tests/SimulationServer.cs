using System.Collections.Generic;
using System.Net;
using Tests;
using UnityEngine;
using UnityEngine.AI;

public class SimulationServer
{
    private GameObject serverPrefab;
    private Dictionary<int, GameObject> clientsCubes;
    private Dictionary<int, ClientInfo> clients;
    private Channel channel;
    private float timeToSend;
    private float elapsedTime;
    private int sequence;
    private int lastInputApplied;
    private float serverTime;

    public SimulationServer(IPEndPoint endPoint, float timeToSend, GameObject serverPrefab)
    {
        channel = new Channel(endPoint.Port);
        clientsCubes = new Dictionary<int, GameObject>();
        clients = new Dictionary<int, ClientInfo>();
        this.serverPrefab = serverPrefab;
        this.timeToSend = timeToSend;
        sequence = 0;
        elapsedTime = 0f;
        lastInputApplied = 0;
        serverTime = 0f;
    }
    
     public void UpdateServer()
    {
        elapsedTime += Time.deltaTime;
        serverTime += Time.deltaTime;
        ReceivePackets();
        if (elapsedTime >= timeToSend)
        {
            UpdateClients();
            elapsedTime -= timeToSend;
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
             CubeEntity cubeEntity = new CubeEntity(clientsCubes[clientId]);
             Snapshot currentSnapshot = new Snapshot(sequence, cubeEntity, currentWorldInfo);
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
             CubeEntity clientEntity = new CubeEntity(clientsCubes[clientId]);
             currentWorldInfo.addPlayer(clientId, clientEntity);
         }

         return currentWorldInfo;
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
                Debug.Log("Receive inputs from " + clientId);
                ClientInfo currentClient = clients[clientId];
                int startInput = packet.buffer.GetInt();
                List<int> inputsToExecute = GameInput.Deserialize(packet.buffer);
                int ackNumber = packet.buffer.GetInt();
                SendAck(ackNumber, PacketType.ACK, currentClient.endPoint);
                ApplyInputs(clientId, startInput, inputsToExecute);
                currentClient.lastInputApplied = ackNumber;
            }
            else if (packetType == (int) PacketType.EVENT)
            {
                 // handle event   
                 int clientId = packet.buffer.GetInt();
                 ClientInfo currentClient = clients[clientId];
                 GameEvent currentEvent = GameEvent.Deserialize(packet.buffer);
                 SendAck(currentEvent.eventNumber, PacketType.EVENT, currentClient.endPoint);
            }
            else if (packetType == (int) PacketType.JOIN_GAME)
            {
                int clientId = packet.buffer.GetInt();
                var clientEndPoint = packet.fromEndPoint;
                clients[clientId] = new ClientInfo(clientId, clientEndPoint);
                SendJoinEventResponse(clientId);
            }
            packet.Free();
            packet = channel.GetPacket();
            
        }
    }

    private void SendJoinEventResponse(int clientId)
    {
        float xPosition = Random.Range(-4f, 4f);
        float yPosition = 1f;
        float zPosition = Random.Range(-4f, 4f);
        Vector3 position = new Vector3(xPosition, yPosition, zPosition);
        Quaternion rotation = Quaternion.Euler(Vector3.zero);
        GameObject newCube = GameObject.Instantiate(serverPrefab, position, rotation);
        clientsCubes[clientId] = newCube;
        CubeEntity newClient = new CubeEntity(newCube, position, rotation.eulerAngles);
        foreach (var id in clients.Keys)
        {
            ClientInfo currentPlayer = clients[id];
            IPEndPoint clientEndpoint = currentPlayer.endPoint;
            var packet = Packet.Obtain();
            packet.buffer.PutInt((int) PacketType.JOIN_GAME);
            packet.buffer.PutInt(clientId);
            newClient.Serialize(packet.buffer);
            packet.buffer.Flush();
            channel.Send(packet, clientEndpoint);
        }
        
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

    private void ApplyInputs(int clientId, int startInput, List<int> inputsToExecute)
    {
        for (int i = 0; i < inputsToExecute.Count; i++)
        {
            if (clients[clientId].lastInputApplied < startInput + i)
            {
                Vector3 appliedForce = AnalyzeInput(inputsToExecute[i]);
                MoveCubeWithForce(clientId, appliedForce);
            }
        }
    }

    private Vector3 AnalyzeInput(int inputs)
    {
        Vector3 appliedForce = Vector3.zero;
        ;
        if ((inputs & 1) > 0)
        {
            appliedForce += Vector3.up * 5;
        } 
        if ((inputs & (1 << 1)) > 0)
        {
            appliedForce += Vector3.left * 5;
        }
        if ((inputs & (1 << 2)) > 0)
        {
            appliedForce += Vector3.right * 5;
        }

        return appliedForce;
    }

    private void MoveCubeWithForce(int clientId, Vector3 appliedForce)
    {
        Rigidbody clientRigidBody = clientsCubes[clientId].GetComponent<Rigidbody>();
        clientRigidBody.AddForceAtPosition(appliedForce, Vector3.zero, ForceMode.Impulse);
    }
    

    public Channel GetChannel()
    {
        return channel;
    }
    
    public void DestroyChannel() {
        channel.Disconnect();
    }
}
