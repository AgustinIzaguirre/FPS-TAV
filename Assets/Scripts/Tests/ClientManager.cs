using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ClientManager : MonoBehaviour
{
    [SerializeField] private GameObject clientPrefab;
    [SerializeField] private GameObject simulationPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject bulletTrailPrefab;

    
    private Rigidbody cubeServerRigidBody;
    private int packetsPerSecond = 60;
    public int minSnapshots = 3;
    public float timeoutForEvents;
    private float timeToSend;
    private SimulationClient client;
    private List<JoinEvent> sentJoinEvents;
    private float time;
    private IPEndPoint serverEndPoint;
    private GameMode gameMode;
    private System.Random random;

    

    void Start()
    {
        gameMode = GameConfig.GetGameMode();
        if (gameMode == GameMode.CLIENT)
        {
            timeToSend = (float) 1 / (float) packetsPerSecond;
            timeoutForEvents = 1f;
            serverEndPoint = GameConfig.GetServerEndPoint();
            sentJoinEvents = new List<JoinEvent>();
            Application.targetFrameRate = 60;
            time = 0f;
            Cursor.lockState = CursorLockMode.Locked;
            random = new System.Random();
            RequestJoin();
        }
    }

    private void OnDestroy() {
        if (gameMode == GameMode.CLIENT)
        {
            client.DestroyChannel();
            
        }
    }

    private void FixedUpdate()
    {
        if (gameMode == GameMode.CLIENT)
        {
            client.ClientFixedUpdate();
        }
    }

    void Update()
    {
        if (gameMode == GameMode.CLIENT)
        {
            time += Time.deltaTime;
            CheckIfClientJoined();
            client.UpdateClient();
        }
    }

    private void RequestJoin()
    {
        // Add base player
        int clientId = 1 + GameConfig.GetPlayerQuantity();
        GameConfig.IncrementPlayerQuantity();
        int portNumber = clientId + 9000 + random.Next(0, 500);
        SimulationClient client = new SimulationClient(portNumber, minSnapshots, timeToSend, timeoutForEvents, clientId,
            serverEndPoint, clientPrefab, simulationPrefab, enemyPrefab, bulletTrailPrefab);
        this.client = client;
        Debug.Log("Sending join event");
        SendPlayerJoinEvent(clientId);
    }
    
    private void SendPlayerJoinEvent(int clientId)
    {
        Packet packet = Packet.Obtain();
        packet.buffer.PutInt((int) PacketType.JOIN_GAME);
        packet.buffer.PutInt(clientId);
        packet.buffer.Flush();
        client.GetChannel().Send(packet, serverEndPoint);
        packet.Free();
        sentJoinEvents.Add(new JoinEvent(clientId, time));
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
    
    private void CheckIfClientJoined()
    {
        ReceiveJoinEventResponses();
        ResendJoinEvents();
    }

    private void ReceiveJoinEventResponses()
    {
        bool receivedResponse = false;
        for (int i = 0; i < sentJoinEvents.Count && !receivedResponse; i++)
        {
            if (ReceiveClientJoinResponse(client))
            {
                receivedResponse = true;
            }
        }

        if (receivedResponse)
        {
            sentJoinEvents.Clear();
        }
    }

    private bool ReceiveClientJoinResponse(SimulationClient currentClient)
    {
        Channel clientChannel = currentClient.GetChannel();
        var packet = clientChannel.GetPacket();
        bool receivedPacket = false;
        while (packet != null)
        {
            int packetType = packet.buffer.GetInt();
            if (packetType == (int) PacketType.JOIN_GAME)
            {
                int clientId = packet.buffer.GetInt();
                Debug.Log("Client id set by server = " + clientId);
                if (currentClient.id == 1)
                {
                    currentClient.id = clientId;
                    Debug.Log("CurrentClient id = " + currentClient.id);

                }
                receivedPacket = true;
            }
            packet.Free();
            if (receivedPacket)
            {
                return true;
            }
            packet = clientChannel.GetPacket();
        }
        return false;
    }
}
