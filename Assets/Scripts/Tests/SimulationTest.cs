using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Object = UnityEngine.Object;

public class SimulationTest : MonoBehaviour
{
    [SerializeField] private GameObject serverPrefab;
    [SerializeField] private GameObject clientPrefab;
    [SerializeField] private GameObject simulationPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject bulletTrailPrefab;

    private Rigidbody cubeServerRigidBody;
    private int packetsPerSecond = 60;
    public int minSnapshots = 3;
    public float timeoutForEvents;
    private float timeToSend;
    private bool connected = true;
    private Dictionary<int, Channel> clients;
    private List<JoinEvent> sentJoinEvents;
    private SimulationServer server;
    private int lastClientId;
    private float time;
    private bool isClientSpawned;
    private IPEndPoint serverEndPoint;
    public GameMode gameMode = GameMode.BOTH;

    void Start()
    {
        gameMode = GameConfig.GetGameMode();
        if (gameMode == GameMode.BOTH)
        {
            timeToSend = (float) 1 / (float) packetsPerSecond;
            timeoutForEvents = 1f;
            serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9000);
            clients = new Dictionary<int, Channel>();
            sentJoinEvents = new List<JoinEvent>();
            lastClientId = 0;
            server = new SimulationServer(serverEndPoint, timeToSend, serverPrefab);
            Application.targetFrameRate = 60;
            time = 0f;
            Cursor.lockState = CursorLockMode.Locked;
            isClientSpawned = false;
        }
    }

    private void OnDestroy() {
        if (gameMode == GameMode.BOTH)
        {
//            foreach (var client in clients.Values)
//            {
//                client.DestroyChannel();
//            }

            server.DestroyChannel();
        }
    }

    private void FixedUpdate()
    {
        if (gameMode == GameMode.BOTH)
        {
            server.ServerFixedUpdate();
        }
    }

    void Update()
    {
        if (gameMode == GameMode.BOTH)
        {
            time += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.C))
            {
                connected = !connected;
            }

            if (connected)
            {
                server.UpdateServer();
            }
            
            CheckIfClientJoined();
            foreach(var clientId in clients.Keys)
            {
                if (clientId != 1 || !isClientSpawned)
                {
                    ReceiveClientInfo(clientId);
                }
            }
        }
    }
    
    private void ReceiveClientInfo(int clientId)
    {
        var packet = clients[clientId].GetPacket();
        while (packet != null)
        {
            int packetType = packet.buffer.GetInt();
            if (packetType == (int) PacketType.NEW_PLAYER)
            {
                AnalyzeNewPlayerEvent(packet);
            }
            packet.Free();
            packet = clients[clientId].GetPacket();
        }
    }

    private void CheckIfClientJoined()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            SendPlayerJoinEvent();
        }
        ReceiveJoinEventResponses();
        ResendJoinEvents();
    }

    private void ReceiveJoinEventResponses()
    {
        List<int> eventsToRemove = new List<int>();
        for (int i = 0; i < sentJoinEvents.Count; i++)
        {
            int currentClientId = sentJoinEvents[i].clientId;
            if (ReceiveClientJoinResponse(currentClientId))
            {
                eventsToRemove.Add(i);
            }
        }

        for (int i = 0; i < eventsToRemove.Count; i++)
        {
            sentJoinEvents.RemoveAt(eventsToRemove[i]);
        }
    }

    private bool ReceiveClientJoinResponse(int currentClientId)
    {
        var packet = clients[currentClientId].GetPacket();

        while (packet != null)
        {
            int packetType = packet.buffer.GetInt();
            if (packetType == (int) PacketType.JOIN_GAME)
            {
                int clientId = packet.buffer.GetInt();
                // TODO remove on production
                if (clientId == 1)
                {
                    ClientConfig.SetId(clientId);
                }
                return true;
            }
            packet.Free();
            packet = clients[currentClientId].GetPacket();
        }
        return false;
    }

    private void SendPlayerJoinEvent()
    {
        int clientId = lastClientId + 1;
        int portNumber = clientId + 9000;
        clients[clientId] = new Channel(ClientConfig.GetPort());
        ClientConfig.ConfigureClient(clientId, portNumber, timeToSend, minSnapshots, timeoutForEvents,
            clients[clientId], bulletTrailPrefab);
        Debug.Log("Sending join event");
        lastClientId++;
        SendPlayerJoinEvent(clientId);
    }

    private void ResendJoinEvents()
    {
        while (sentJoinEvents.Count > 0 && (time - sentJoinEvents[0].time) > timeoutForEvents)
        {
            JoinEvent currentEvent = sentJoinEvents[0];
            sentJoinEvents.RemoveAt(0);
            SendPlayerJoinEvent(currentEvent.clientId);
        }
    }

    private void SendPlayerJoinEvent(int clientId)
    {
        Packet packet = Packet.Obtain();
        packet.buffer.PutInt((int) PacketType.JOIN_GAME);
        packet.buffer.PutInt(clientId);
        packet.buffer.Flush();
        clients[clientId].Send(packet, serverEndPoint);
        packet.Free();
        sentJoinEvents.Add(new JoinEvent(clientId, time));
    }
    
    public void AnalyzeNewPlayerEvent(Packet packet)
    {
        NewPlayerEvent newPlayerEvent = NewPlayerEvent.Deserialize(packet.buffer);
        int playerId = newPlayerEvent.playerId;
        
        if (ClientConfig.GetId() == playerId)
        {
            SpawnClient(newPlayerEvent.newPlayer);
            SendAck((int) PacketType.NEW_PLAYER, playerId);
        }
    }
    
    private void SendAck(int packetType, int playerId)
    {
        var packet = Packet.Obtain();
        packet.buffer.PutInt(packetType);
        packet.buffer.PutInt(ClientConfig.GetId());
        packet.buffer.PutInt(playerId);
        packet.buffer.Flush();
        clients[ClientConfig.GetId()].Send(packet, serverEndPoint);
        packet.Free();    
    }
    
    public void SpawnClient(PlayerEntity player)
    {
        Vector3 position = player.position;
        Quaternion rotation = Quaternion.Euler(player.eulerAngles);
        GameObject playerObject = Object.Instantiate(clientPrefab, position, rotation) as GameObject;
        ClientConfig.SetPlayerInfo(new PlayerInfo(ClientConfig.GetId(), new PlayerEntity(playerObject), false));
        isClientSpawned = true;
        ClientConfig.SetPlayerPrediction(Instantiate(simulationPrefab, position, rotation) as GameObject);
        Physics.IgnoreCollision(ClientConfig.GetPlayerPrediction().GetComponent<Collider>(),
            ClientConfig.GetPlayerInfo().playerGameObject.GetComponent<Collider>());
    }
}