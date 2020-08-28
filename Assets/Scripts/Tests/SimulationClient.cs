using System.Collections.Generic;
using System.Net;
using Tests;
using UnityEngine;
using UnityEngine.AI;

public class SimulationClient
{
    private GameObject cube;
    private Channel channel;
    private List<int> sentInputs;
    private List<GameEvent> sentEvents;
    private List<Snapshot> interpolationBuffer;
    private int lastInputSent;
    private int lastInputRemoved;
    private readonly int minSnapshots;
    private bool render;
    private float clientTime;
    private readonly float timeToSend;
    private float timeout;
    private int eventNumber;
    
    public SimulationClient(int portNumber, GameObject cube, int minSnapshots, float timeToSend, float timeout)
    {
        channel = new Channel(portNumber);
        interpolationBuffer = new List<Snapshot>();
        sentInputs = new List<int>();
        sentEvents = new List<GameEvent>();
        render = false;
        this.cube = cube;
        this.minSnapshots = minSnapshots;
        this.timeToSend = timeToSend;
        this.timeout = timeout;
        lastInputSent = 1;
        clientTime = 0f;
        eventNumber = 0;
        
    }

    public void UpdateClient(Channel serverChannel)
    {
        if (render)
        {
            clientTime += Time.deltaTime;
        }

        SendInputToServer(serverChannel);
        CheckForGameEvents(serverChannel);
        var packet = channel.GetPacket();

        while (packet != null)
        {
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
            else if (packetType == (int) PacketType.EVENT)
            {
                int ackNumber = packet.buffer.GetInt();
                for (int i = 0; i < sentEvents.Count; i++)
                {
                    if (ackNumber == sentEvents[i].eventNumber)
                    {
                        sentEvents.RemoveAt(i);
                    }
                }
            }

            packet.Free();
            packet = channel.GetPacket();
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
            packet.buffer.PutInt((int) PacketType.INPUT);
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

    private void CheckForGameEvents(Channel serverChannel)
    {
        GameEvent currentEvent = GetGameEvent();
        if (currentEvent != null)
        {
            SendEventToServer(serverChannel, currentEvent);
        }
        ResendAndDeleteEvent(serverChannel);
    }

    private void SendEventToServer(Channel serverChannel, GameEvent currentEvent)
    {
        var packet = Packet.Obtain();
        sentEvents.Add(currentEvent);
        currentEvent.Serialize(packet.buffer);
        packet.buffer.Flush();
        string serverIP = "127.0.0.1";
        int port = 9001;
        var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
        serverChannel.Send(packet, remoteEp);
        packet.Free();
        
    }

    private void ResendAndDeleteEvent(Channel serverChannel)
    {
        if (sentEvents.Count > 0)
        {
            GameEvent firstEvent = sentEvents[0];
            while (firstEvent != null && clientTime - firstEvent.time >= timeout)
            {
                sentEvents.RemoveAt(0);
                GameEvent newEvent = new GameEvent(firstEvent.name, firstEvent.value, clientTime, eventNumber);
                SendEventToServer(serverChannel, newEvent);
                firstEvent = sentEvents.Count > 0 ? sentEvents[0] : null;
            }
        }
    }
    private GameEvent GetGameEvent()
    {
        GameEvent currentEvent = null;
        if (Input.GetKeyDown(KeyCode.E))
        {
            eventNumber++;
            currentEvent = new GameEvent("event", 10, clientTime, eventNumber);
        }
        return currentEvent;
    }

    private void Interpolate()
    {
        Snapshot current = interpolationBuffer[0];
        Snapshot next = interpolationBuffer[1];
        Snapshot last = interpolationBuffer[interpolationBuffer.Count - 1];
        float startTime = current.sequence * timeToSend;
        float endTime = next.sequence * timeToSend;
        float lastTime = last.sequence * timeToSend;
        if (lastTime - startTime > 1f)
        {
            ResetBufferAndClientTime(lastTime);
        }
        else
        {
            if (clientTime > endTime)
            {
                interpolationBuffer.RemoveAt(0);
            }

            CubeEntity interpolatedCube = CubeEntity.CreateInterpolated(current.cubeEntity, next.cubeEntity,
                startTime, endTime, clientTime);
            interpolatedCube.Apply();
        }
    }

    private void ResetBufferAndClientTime(float lastTime)
    {
        clientTime = lastTime;
        interpolationBuffer = new List<Snapshot>();
        render = false;
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
