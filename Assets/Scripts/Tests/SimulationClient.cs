using System.Collections.Generic;
using System.Net;
using Tests;
using UnityEngine;
using UnityEngine.AI;

public class SimulationClient
{
    private GameObject cube;
    private GameObject clientPrefab;
    private Channel channel;
    private List<int> sentInputs;
    private List<GameEvent> sentEvents;
    private List<Snapshot> interpolationBuffer;
    private Dictionary<int, GameObject> players;
    private int lastInputSent;
    private int lastInputRemoved;
    private readonly int minSnapshots;
    private bool render;
    private float clientTime;
    private readonly float timeToSend;
    private float timeout;
    private int eventNumber;
    private int id;
    public bool isPlaying;
    private IPEndPoint serverEndPoint;
    
    public SimulationClient(int portNumber, GameObject cube, int minSnapshots, float timeToSend, float timeout, int id,
        IPEndPoint serverEndPoint, GameObject clientPrefab)
    {
        channel = new Channel(portNumber);
        interpolationBuffer = new List<Snapshot>();
        players = new Dictionary<int, GameObject>();
        players[id] = cube;
        sentInputs = new List<int>();
        sentEvents = new List<GameEvent>();
        render = false;
        this.cube = cube;
        this.minSnapshots = minSnapshots;
        this.timeToSend = timeToSend;
        this.timeout = timeout;
        this.id = id;
        this.serverEndPoint = serverEndPoint;
        this.clientPrefab = clientPrefab;
        lastInputSent = 1;
        clientTime = 0f;
        eventNumber = 0;
        isPlaying = true;
        
    }
    
    public SimulationClient(int portNumber, int minSnapshots, float timeToSend, float timeout, int id,
        IPEndPoint serverEndPoint, GameObject clientPrefab)
    {
        channel = new Channel(portNumber);
        interpolationBuffer = new List<Snapshot>();
        sentInputs = new List<int>();
        sentEvents = new List<GameEvent>();
        render = false;
        this.minSnapshots = minSnapshots;
        this.timeToSend = timeToSend;
        this.timeout = timeout;
        this.id = id;
        this.serverEndPoint = serverEndPoint;
        this.clientPrefab = clientPrefab;
        lastInputSent = 1;
        clientTime = 0f;
        eventNumber = 0;
        isPlaying = false;
    }

    public void UpdateClient(Channel serverChannel)
    {
        if (isPlaying)
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
                else if (packetType == (int) PacketType.JOIN_GAME)
                {
                    int playerId = packet.buffer.GetInt();
                    CubeEntity playerCube = new CubeEntity(clientPrefab);
                    playerCube.Deserialize(packet.buffer);
                    SpawnPlayer(playerId, playerCube);
                }

                packet.Free();
                packet = channel.GetPacket();
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
            packet.buffer.PutInt((int) PacketType.INPUT); // TODO compress each packet type
            packet.buffer.PutInt(id);
            packet.buffer.PutInt(lastInputRemoved + 1);
            GameInput.Serialize(sentInputs, lastInputSent, packet.buffer);
            packet.buffer.Flush();
            serverChannel.Send(packet, serverEndPoint);
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
        currentEvent.Serialize(packet.buffer, id);
        packet.buffer.Flush();
        serverChannel.Send(packet, serverEndPoint);
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
                eventNumber++;
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

            WorldInfo currentWorldInfo = current.worldInfo;
            WorldInfo nextWorldInfo = next.worldInfo;
            foreach (var playerId in currentWorldInfo.players.Keys)
            {
                CubeEntity previousCubeEntity = currentWorldInfo.players[playerId];
                if (nextWorldInfo.players.ContainsKey(playerId) && players.ContainsKey(playerId))
                {
                    CubeEntity nextCubeEntity = nextWorldInfo.players[playerId];
                    CubeEntity interpolatedCube = CubeEntity.CreateInterpolated(previousCubeEntity, nextCubeEntity,
                        startTime, endTime, clientTime, players[playerId]);
                    interpolatedCube.Apply();

                }
            }
            
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

    public Channel GetChannel()
    {
        return channel;
    }
    
    public void DestroyChannel() {
        channel.Disconnect();
    }

    public void Spawn(CubeEntity clientCube)
    {
        Vector3 position = clientCube.position;
        Quaternion rotation = Quaternion.Euler(clientCube.eulerAngles);
        cube = GameObject.Instantiate(clientCube.cubeGameObject, position, rotation) as GameObject;
        isPlaying = true;
    }
    
    public void SpawnPlayer(int playerId, CubeEntity playerCube)
    {
        Vector3 position = playerCube.position;
        Quaternion rotation = Quaternion.Euler(playerCube.eulerAngles);
        GameObject player = GameObject.Instantiate(playerCube.cubeGameObject, position, rotation) as GameObject;
        players[playerId] = player;
    }
}
