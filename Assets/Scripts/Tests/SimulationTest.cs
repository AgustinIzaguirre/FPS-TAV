using System.Collections;
using System.Collections.Generic;
using System.Net;
using Tests;
using UnityEngine;

public class SimulationTest : MonoBehaviour
{

    private Channel channel;

    [SerializeField] private GameObject cubeServer;
    [SerializeField] private GameObject cubeClient;

    private Rigidbody cubeServerRigidBody;
    private int packetsPerSecond = 20;
    private float elapsedTime = 0f;
    private List<Snapshot> interpolationBuffer;
    public int minSnapshots = 3;
    private float timeToSend;
    private float clientTime = 0f;
    private bool render;
    private int sequence = 0;
    
    void Start()
    {
        channel = new Channel(9000);
        timeToSend = (float)1 / (float)packetsPerSecond;
        interpolationBuffer = new List<Snapshot>();
        render = false;
        cubeServerRigidBody = cubeServer.GetComponent<Rigidbody>();
    }

    private void OnDestroy() {
        channel.Disconnect();
    }

    void Update() {
        elapsedTime += Time.deltaTime;
        
        if (Input.GetKeyDown(KeyCode.Space)) {
            cubeServerRigidBody.AddForceAtPosition(Vector3.up * 5, Vector3.zero, ForceMode.Impulse);
        }
        
        UpdateServer();
        UpdateClient();
    }

    private void UpdateClient() {
        if (render)
        {
            clientTime += Time.deltaTime;
        }
        
        var packet = channel.GetPacket();
        
        if (packet == null) {
            return;
        }

        CubeEntity cubeEntity = new CubeEntity(cubeClient);
        Snapshot currentSnapshot = new Snapshot(cubeEntity);
        currentSnapshot.Deserialize(packet.buffer);
        AddToInterpolationBuffer(currentSnapshot);
        if (render)
        {
            Interpolate();
        }
    }

    private void Interpolate()
    {
        Snapshot current = interpolationBuffer[0];
        Snapshot next = interpolationBuffer[1];
        float startTime = current.sequence * timeToSend;
        float endTime = next.sequence * timeToSend;
        if (clientTime > endTime)
        {
            interpolationBuffer.RemoveAt(0);

//            while (clientTime > endTime && interpolationBuffer.Count > 2)
//            {
//                if (interpolationBuffer.Count > 2)
//                {
//                    interpolationBuffer.RemoveAt(0);
//                    next = interpolationBuffer[1];
//                    endTime = next.sequence * timeToSend;
//                }
//            }
        }
        CubeEntity interpolatedCube = CubeEntity.CreateInterpolated(current.cubeEntity, next.cubeEntity, 
                                                                    startTime, endTime, clientTime);
        interpolatedCube.Apply();
        Debug.Log("startTime: " + startTime);
        Debug.Log("clientTime: " + clientTime);
        Debug.Log("nextTime: " + endTime);
    }
    

    private void AddToInterpolationBuffer(Snapshot currentSnapshot)
    {
        int size = interpolationBuffer.Count;
        if (size == 0 || currentSnapshot.sequence > interpolationBuffer[size - 1].sequence)
        {
            interpolationBuffer.Add(currentSnapshot);
            if (size + 1 >= minSnapshots)
            {
                render = true;
            }
        }

        if (interpolationBuffer.Count <= 1)
        {
            render = false;
        }
    }

    private void UpdateServer()
    {
        if (elapsedTime >= timeToSend)
        {
            //serialize
            var packet = Packet.Obtain();
            sequence++;
            CubeEntity cubeEntity = new CubeEntity(cubeServer);
            Snapshot currentSnapshot = new Snapshot(sequence, cubeEntity);
            currentSnapshot.Serialize(packet.buffer);
            packet.buffer.Flush();
            string serverIP = "127.0.0.1";
            int port = 9000;
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
            channel.Send(packet, remoteEp);
            packet.Free();
            elapsedTime -= timeToSend;
        }
    }
}
