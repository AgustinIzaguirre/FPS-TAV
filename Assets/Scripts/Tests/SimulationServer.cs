using System.Collections.Generic;
using System.Net;
using Tests;
using UnityEngine;

public class SimulationServer
{
    private GameObject cube;
    private Rigidbody cubeRigidBody;
    private Channel channel;
    private float timeToSend;
    private float elapsedTime;
    private int sequence;
    private int lastInputApplied;

    public SimulationServer(int portNumber, GameObject cube, float timeToSend)
    {
        channel = new Channel(portNumber);
        this.cube = cube;
        cubeRigidBody = cube.GetComponent<Rigidbody>();
        this.timeToSend = timeToSend;
        sequence = 0;
        elapsedTime = 0f;
        lastInputApplied = 0;
    }
    
     public void UpdateServer()
    {
        elapsedTime += Time.deltaTime;
        ReceiveInputs();
        if (elapsedTime >= timeToSend)
        {
            //serialize
            var packet = Packet.Obtain();
            sequence++;
            CubeEntity cubeEntity = new CubeEntity(cube);
            Snapshot currentSnapshot = new Snapshot(sequence, cubeEntity);
            currentSnapshot.Serialize(packet.buffer);
            packet.buffer.Flush();
            string clientIP = "127.0.0.1";
            int port = 9000;
            var remoteEp = new IPEndPoint(IPAddress.Parse(clientIP), port);
            channel.Send(packet, remoteEp);
            packet.Free();
            elapsedTime -= timeToSend;
        }
    }

    private void ReceiveInputs()
    {
        var packet = channel.GetPacket();
        
        if (packet == null) {
            return;
        }
        int startInput = packet.buffer.GetInt();
        List<int> inputsToExecute = GameInput.Deserialize(packet.buffer);
        int ackNumber = packet.buffer.GetInt();
        SendACK(ackNumber);
        ApplyInputs(startInput, inputsToExecute);
        lastInputApplied = ackNumber;
    }

    private void SendACK(int ackNumber)
    {
        var packet = Packet.Obtain();
        packet.buffer.PutInt((int)PacketType.ACK);
        packet.buffer.PutInt(ackNumber);
        packet.buffer.Flush();
        string serverIP = "127.0.0.1";
        int port = 9000;
        var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
        channel.Send(packet, remoteEp);
        packet.Free();
    }

    private void ApplyInputs(int startInput, List<int> inputsToExecute)
    {
        for (int i = 0; i < inputsToExecute.Count; i++)
        {
            if (lastInputApplied < startInput + i)
            {
                Vector3 appliedForce = AnalyzeInput(inputsToExecute[i]);
                MoveCubeWithForce(appliedForce);
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

    private void MoveCubeWithForce(Vector3 appliedForce)
    {
        cubeRigidBody.AddForceAtPosition(appliedForce, Vector3.zero, ForceMode.Impulse);
    }
    

    public Channel GetChannel()
    {
        return channel;
    }
    
    public void DestroyChannel() {
        channel.Disconnect();
    }
}
