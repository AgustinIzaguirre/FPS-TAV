using System.Collections.Generic;
using System.Net;
using Tests;
using UnityEngine;

public class SimulationClient
{
    private GameObject cube;
    private Channel channel;
    private List<int> sentInputs;
    private List<Snapshot> interpolationBuffer;
    private int lastInputSent;
    private int lastInputRemoved;
    private readonly int minSnapshots;
    private bool render;
    private float clientTime;
    private readonly float timeToSend;
    
    public SimulationClient(int portNumber, GameObject cube, int minSnapshots, float timeToSend)
    {
        channel = new Channel(portNumber);
        interpolationBuffer = new List<Snapshot>();
        sentInputs = new List<int>();
        render = false;
        this.cube = cube;
        this.minSnapshots = minSnapshots;
        this.timeToSend = timeToSend;
        lastInputRemoved = 0;
        lastInputSent = 1;
        clientTime = 0f;

    }

    public void UpdateClient(Channel serverChannel)
    {
        if (render)
        {
            clientTime += Time.deltaTime;
        }

        SendInputToServer(serverChannel);
        var packet = channel.GetPacket();

        if (packet == null)
        {
            return;
        }

        int packetType = packet.buffer.GetInt();
        if (packetType == (int) PacketType.SNAPSHOT)
        {
            CubeEntity cubeEntity = new CubeEntity(cube);
            Snapshot currentSnapshot = new Snapshot(cubeEntity);
            currentSnapshot.Deserialize(packet.buffer);
            AddToInterpolationBuffer(currentSnapshot);
            if (render)
            {
                Interpolate();
            }
        }
        else if (packetType == (int) PacketType.ACK)
        {
            int ackNumber = packet.buffer.GetInt();
            int quantity = ackNumber - lastInputRemoved;
            lastInputRemoved = quantity > 0 ? lastInputRemoved + quantity : lastInputRemoved;
            while (quantity > 0)
            {
                sentInputs.RemoveAt(0);
                quantity -= 1;
            }
        }
    }

    private GameInput GetUserInput()
    {
        bool jump = false, moveLeft = false, moveRight = false;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jump = true;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            moveLeft = true;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            moveRight = true;
        }

        return new GameInput(jump, moveLeft, moveRight);
    }

    private void SendInputToServer(Channel serverChannel)
    {
        GameInput currentInput = GetUserInput();
         
        if (currentInput.value > 0)
        {
            var packet = Packet.Obtain();
            sentInputs.Add(currentInput.value);
            packet.buffer.PutInt(lastInputRemoved + 1);
            GameInput.Serialize(sentInputs, lastInputSent, packet.buffer);
            packet.buffer.Flush();
            string serverIP = "127.0.0.1";
            int port = 9001;
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
            serverChannel.Send(packet, remoteEp);
            packet.Free();
            lastInputSent += 1;
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
        }
        
        CubeEntity interpolatedCube = CubeEntity.CreateInterpolated(current.cubeEntity, next.cubeEntity, 
                                                                    startTime, endTime, clientTime);
        interpolatedCube.Apply();
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
    
    public void DestroyChannel() {
        channel.Disconnect();
    }

}
