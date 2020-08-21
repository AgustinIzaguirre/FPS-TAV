using UnityEngine;

public class SimulationTest : MonoBehaviour
{
    [SerializeField] private GameObject cubeServer;
    [SerializeField] private GameObject cubeClient;

    private Rigidbody cubeServerRigidBody;
    private int packetsPerSecond = 60;
    public int minSnapshots = 3;
    private float timeToSend;
    private bool connected = true;
    private SimulationClient client;
    private SimulationServer server;

    void Start()
    {
        timeToSend = (float)1 / (float)packetsPerSecond;
        client = new SimulationClient(9000, cubeClient, minSnapshots, timeToSend);
        server = new SimulationServer(9001, cubeServer, timeToSend);
    }

    private void OnDestroy() {
        client.DestroyChannel();
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.D))
        {
            connected = !connected;
        }

        if (connected)
        {
            server.UpdateServer();
        }

        client.UpdateClient(server.GetChannel());
    }
}