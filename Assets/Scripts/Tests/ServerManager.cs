using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ServerManager : MonoBehaviour
{
    [SerializeField] private GameObject serverPrefab;
    
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
    private GameMode gameMode;

    void Start()
    {
        gameMode = GameConfig.GetGameMode();
        if (gameMode == GameMode.SERVER)
        {
            timeToSend = (float) 1 / (float) packetsPerSecond;
            timeoutForEvents = 1f;
            serverEndPoint = GameConfig.GetServerEndPoint();
            clients = new Dictionary<int, SimulationClient>();
            sentJoinEvents = new List<JoinEvent>();
            lastClientId = 0;
            server = new SimulationServer(serverEndPoint, timeToSend, serverPrefab);
            Application.targetFrameRate = 60;
            time = 0f;
        }
    }

    private void OnDestroy() {
        if (gameMode == GameMode.SERVER)
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
        if (gameMode == GameMode.SERVER)
        {
            server.ServerFixedUpdate();
        }
    }

    void Update()
    {
        if (gameMode == GameMode.SERVER)
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
        }
    }
}
