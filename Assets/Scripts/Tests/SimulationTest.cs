using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

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
    private Dictionary<int, SimulationClient> clients;
    private List<JoinEvent> sentJoinEvents;
    private SimulationServer server;
    private int lastClientId;
    private float time;
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
            clients = new Dictionary<int, SimulationClient>();
            sentJoinEvents = new List<JoinEvent>();
            lastClientId = 0;
            server = new SimulationServer(serverEndPoint, timeToSend, serverPrefab);
            Application.targetFrameRate = 60;
            time = 0f;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void OnDestroy() {
        if (gameMode == GameMode.BOTH)
        {
            foreach (var client in clients.Values)
            {
                client.DestroyChannel();
            }

            server.DestroyChannel();
        }
    }

    private void FixedUpdate()
    {
        if (gameMode == GameMode.BOTH)
        {
            if (clients.Count > 0)
            {
                clients[1].ClientFixedUpdate();
            }

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

            foreach (var client in clients.Values)
            {
                client.UpdateClient();
            }
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
            SimulationClient currentClient = clients[currentClientId];
            if (ReceiveClientJoinResponse(currentClient))
            {
                eventsToRemove.Add(i);
            }
        }

        for (int i = 0; i < eventsToRemove.Count; i++)
        {
            sentJoinEvents.RemoveAt(eventsToRemove[i]);
        }
    }

    private bool ReceiveClientJoinResponse(SimulationClient currentClient)
    {
        Channel clientChannel = currentClient.GetChannel();
        var packet = clientChannel.GetPacket();

        while (packet != null)
        {
            int packetType = packet.buffer.GetInt();
            if (packetType == (int) PacketType.JOIN_GAME)
            {
                int clientId = packet.buffer.GetInt();
                // TODO remove on production
                if (clientId == 1)
                {
                    currentClient.id = clientId;
                }
//                else
//                {
//                    currentClient.isPlaying = true;
//                }
                return true;
            }
            packet.Free();
            packet = clientChannel.GetPacket();
        }
        return false;
    }

    private void SendPlayerJoinEvent()
    {
        // Add base player
        int clientId = lastClientId + 1;
        int portNumber = clientId + 9000;
        SimulationClient client = new SimulationClient(portNumber, minSnapshots, timeToSend, timeoutForEvents, clientId,
            serverEndPoint, clientPrefab, simulationPrefab, enemyPrefab, bulletTrailPrefab);
        clients[clientId] = client;
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
        clients[clientId].GetChannel().Send(packet, serverEndPoint);
        packet.Free();
        sentJoinEvents.Add(new JoinEvent(clientId, time));
    }
}