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


    private Channel clientChannel;
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
    private bool isClientSpawned;
    

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
            isClientSpawned = false;
            RequestJoin();
        }
    }

//    private void OnDestroy() {
//        if (gameMode == GameMode.CLIENT)
//        {
//            clientChannel.Disconnect();
//        }
//    }

    void Update()
    {
        if (gameMode == GameMode.CLIENT && !isClientSpawned)
        {
            time += Time.deltaTime;
            CheckIfClientJoined();
            ReceiveClientInfo();
        }
    }

    private void ReceiveClientInfo()
    {
        var packet = clientChannel.GetPacket();
        while (packet != null)
        {
            int packetType = packet.buffer.GetInt();
            if (packetType == (int) PacketType.NEW_PLAYER)
            {
                AnalyzeNewPlayerEvent(packet);
            }
            packet.Free();
            packet = clientChannel.GetPacket();
        }
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
        clientChannel.Send(packet, serverEndPoint);
        packet.Free();    
    }
    
    public void SpawnClient(PlayerEntity player)
    {
        Vector3 position = player.position;
        Quaternion rotation = Quaternion.Euler(player.eulerAngles);
        GameObject playerObject = Object.Instantiate(clientPrefab, position, rotation) as GameObject;
        ClientConfig.SetPlayerInfo(new PlayerInfo(ClientConfig.GetId(), new PlayerEntity(playerObject), false));
        isClientSpawned = true;
//        clientController = players[id].playerGameObject.GetComponent<CharacterController>();
//        gravityController = players[id].playerGameObject.GetComponent<GravityController>();
        ClientConfig.SetPlayerPrediction(Instantiate(simulationPrefab, position, rotation) as GameObject);
//        predictionController = playerPrediction.GetComponent<CharacterController>();
//        simulationGravityController = playerPrediction.GetComponent<GravityController>();
//        playerCamera = players[id].playerGameObject.GetComponentInChildren< Camera >();
//        weapon = new Weapon(0.2f,  playerCamera.GetComponent<AudioSource>(),
//            playerCamera.GetComponent<MuzzleFlash>(), bulletTrailPrefab);
//        damageScreenController = playerCamera.GetComponent<DamageScreenController>();
//        healthController = playerCamera.GetComponent<HealthController>();
        Physics.IgnoreCollision(ClientConfig.GetPlayerPrediction().GetComponent<Collider>(),
            ClientConfig.GetPlayerInfo().playerGameObject.GetComponent<Collider>());
    }
    
    private void RequestJoin()
    {
        // Add base player
        int clientId = 1 + GameConfig.GetPlayerQuantity();
        GameConfig.IncrementPlayerQuantity();
        int portNumber = clientId + 9000 + random.Next(0, 500);
        clientChannel = new Channel(ClientConfig.GetPort());
        ClientConfig.ConfigureClient(clientId, portNumber, timeToSend, minSnapshots, timeoutForEvents, clientChannel,
            bulletTrailPrefab);
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
            if (ReceiveClientJoinResponse())
            {
                receivedResponse = true;
            }
        }

        if (receivedResponse)
        {
            sentJoinEvents.Clear();
        }
    }

    private bool ReceiveClientJoinResponse()
    {
        var packet = ClientConfig.GetChannel().GetPacket();
        bool receivedPacket = false;
        while (packet != null)
        {
            int packetType = packet.buffer.GetInt();
            if (packetType == (int) PacketType.JOIN_GAME)
            {
                int clientId = packet.buffer.GetInt();
                Debug.Log("Client id set by server = " + clientId);
                ClientConfig.SetId(clientId);
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
