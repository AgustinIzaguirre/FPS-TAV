using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ClientManager : MonoBehaviour
{
    [SerializeField] private GameObject clientPrefab;
    [SerializeField] private GameObject simulationPrefab;
    [SerializeField] private GameObject enemyPrefab;
    
    private Rigidbody cubeServerRigidBody;
    private int packetsPerSecond = 60;
    public int minSnapshots = 3;
    public float timeoutForEvents;
    private float timeToSend;
    private Dictionary<int, SimulationClient> clients;
    private List<JoinEvent> sentJoinEvents;
    private float time;
    private IPEndPoint serverEndPoint;
    private Channel serverChannel;
    private GameMode gameMode;
    

    void Start()
    {
        gameMode = GameConfig.GetGameMode();
        if (gameMode == GameMode.CLIENT)
        {
            timeToSend = (float) 1 / (float) packetsPerSecond;
            timeoutForEvents = 1f;
            serverEndPoint = GameConfig.GetServerEndPoint();
            serverChannel = GameConfig.GetServerChannel();
            clients = new Dictionary<int, SimulationClient>();
            sentJoinEvents = new List<JoinEvent>();
            Application.targetFrameRate = 60;
            time = 0f;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void OnDestroy() {
        if (gameMode == GameMode.CLIENT)
        {
            foreach (var client in clients.Values)
            {
                client.DestroyChannel();
            }
        }
    }

    private void FixedUpdate()
    {
        if (gameMode == GameMode.CLIENT)
        {
            if (clients.Count > 0)
            {
                clients[1].ClientFixedUpdate(serverChannel);
            }
        }
    }

    void Update()
    {
        if (gameMode == GameMode.CLIENT)
        {
            time += Time.deltaTime;
//            CheckIfClientJoined();

            foreach (var client in clients.Values)
            {
                client.UpdateClient(serverChannel);
            }
        }
    }
}
