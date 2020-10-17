using System.Collections.Generic;
using System.Net;
using Tests;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class SimulationClient
{
    private GameObject clientPrefab;
    public GameObject simulationPrefab;


    private float xRotation = 0f;
    
    private Camera playerCamera;
    private CharacterController clientController;
    private CharacterController predictionController;
    private GravityController gravityController;
    private GravityController simulationGravityController;
    private Channel channel;
    private List<int> sentInputs;
    private List<int> appliedInputs;
    private List<GameEvent> sentEvents;
    private List<Snapshot> interpolationBuffer;
    private List<GameInput> inputsToExecute;
    private Dictionary<int, GameObject> players;
    private GameObject playerPrediction;
    private int lastInputSent;
    private int lastInputRemoved;
    private int lastClientInput;
    private int lastServerInput;
    private readonly int minSnapshots;
    private bool render;
    private float clientTime;
    private readonly float timeToSend;
    private float timeout;
    private int eventNumber;
    public int id;
    public bool isSpawned;
    public bool isPlaying;
    private IPEndPoint serverEndPoint;

    public SimulationClient(int portNumber, int minSnapshots, float timeToSend, float timeout, int id,
        IPEndPoint serverEndPoint, GameObject clientPrefab, GameObject simulationPrefab)
    {
        channel = new Channel(portNumber);
        interpolationBuffer = new List<Snapshot>();
        players = new Dictionary<int, GameObject>();
        sentInputs = new List<int>();
        appliedInputs = new List<int>();
        sentEvents = new List<GameEvent>();
        inputsToExecute = new List<GameInput>();
        render = false;
        clientController = null;
        this.minSnapshots = minSnapshots;
        this.timeToSend = timeToSend;
        this.timeout = timeout;
        this.id = id;
        this.serverEndPoint = serverEndPoint;
        this.clientPrefab = clientPrefab;
        lastInputSent = 1;
        clientTime = 0f;
        eventNumber = 0;
        isSpawned = false;
        isPlaying = false;
        lastClientInput = 0;
        lastServerInput = 0;
        this.simulationPrefab = simulationPrefab;
    }

    public void UpdateClient(Channel serverChannel)
    {
        
        if (render)
        {
            clientTime += Time.deltaTime;
        }

        if (isPlaying)
        {
            GetUserActionInput();
////            Debug.Log("Sending input for clientId = " + id + " at time = " + clientTime);
//            SendInputToServer(serverChannel);
//            CheckForGameEvents(serverChannel);
        }

        var packet = channel.GetPacket();
        while (packet != null)
        {
            int packetType = packet.buffer.GetInt();
            if (isPlaying)
            {
                if (packetType == (int) PacketType.SNAPSHOT)
                {
                    Snapshot currentSnapshot = new Snapshot();
                    currentSnapshot.Deserialize(packet.buffer);
                    int lastInput = currentSnapshot.worldInfo.playerAppliedInputs[id];
                    AddToInterpolationBuffer(currentSnapshot);
                    if (render)
                    {
                        Interpolate();

                        CalculatePrediction(currentSnapshot.worldInfo.players[id]);
                        CubeEntity predictionEntity = new CubeEntity(playerPrediction);
                        CubeEntity playerEntity = new CubeEntity(players[id]);
                        // TODO remove from serverInput to lastInput
                        RemoveFromList(lastServerInput, lastInput, appliedInputs);
                        lastServerInput = lastInput;
                        if (!predictionEntity.IsEqual(playerEntity, 0.2f, 50))
                        {
                            Debug.Log("Not equals");
//                            players[id].transform.position = playerPrediction.transform.position;
//                            players[id].transform.rotation = playerPrediction.transform.rotation;
                        }
                    
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
            }
            if (packetType == (int) PacketType.NEW_PLAYER)
            {
//                TODO check if already has player
                NewPlayerEvent newPlayerEvent = NewPlayerEvent.Deserialize(packet.buffer);
                int playerId = newPlayerEvent.playerId;
                if (!players.ContainsKey(playerId))
                {
                    if (id == 1)
                    {
                        if (id == playerId)
                        {
//                            Debug.Log("Spawning own with id = " + playerId);
                            Spawn(newPlayerEvent.newPlayer);
                        }
                        else
                        {
//                            Debug.Log("Spawning player with id = " + playerId);
                            SpawnPlayer(playerId, newPlayerEvent.newPlayer);
                        }
                    }
                }
                SendAck((int) PacketType.NEW_PLAYER, playerId, serverChannel);
            }
            if (packetType == (int) PacketType.START_INFO)
            {
                if (isSpawned)
                {
                    WorldInfo worldInfo = WorldInfo.Deserialize(packet.buffer);
                    foreach (var playerId in worldInfo.players.Keys)
                    {
                        if (playerId != id && !players.ContainsKey(playerId))
                        {
                            
//                            Debug.Log("Spawning player with id = " + playerId);
                            SpawnPlayer(playerId, worldInfo.players[playerId]);
                        }
                    }
                    isPlaying = true;
                    SendAck((int) PacketType.START_INFO, id, serverChannel);
                }
            }

            packet.Free();
            packet = channel.GetPacket();
        } 
    }

    private void RemoveFromList(int start, int end, List<int> list)
    {
        int size = list.Count;
        for (int i = 0; i < size && i <= end - start; i++)
        {
            list.RemoveAt(0);
        }
    }

    private void CalculatePrediction(CubeEntity serverPlayer)
    {
        playerPrediction.transform.position = serverPlayer.position;
        playerPrediction.transform.eulerAngles = serverPlayer.eulerAngles;
//        Debug.Log("lastClientInput: " + lastClientInput + ", lastServerInput: " + lastServerInput);
        int quantity = lastClientInput - lastServerInput;
        float verticalVelocity = serverPlayer.verticalVelocity;
        Transform predictionTransform = playerPrediction.transform;
        for (int i = 0; i < appliedInputs.Count && i <= quantity; i++)
        {
            simulationGravityController.ApplyGravity(verticalVelocity);
            PlayerMotion.ApplyInput(appliedInputs[i], predictionController, simulationGravityController,
                predictionTransform);
            verticalVelocity = simulationGravityController.GetVerticalVelocity();
        }
    }

    private void SendAck(int packetType, int playerId, Channel serverChannel)
    {
        var packet = Packet.Obtain();
        packet.buffer.PutInt(packetType);
        packet.buffer.PutInt(id);
        packet.buffer.PutInt(playerId);
        packet.buffer.Flush();
        serverChannel.Send(packet, serverEndPoint);
        packet.Free();    
    }

    private GameInput GetUserInput()
    {
        bool jump = false, moveLeft = false, moveRight = false, moveForward = false, moveBackward = false;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            moveLeft = true;
        }
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            moveRight = true;
        }
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            moveForward = true;
        }
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            moveBackward = true;
        }

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        float mouseSensitivity = 100f;
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        players[id].transform.Rotate(Vector3.up * (mouseX * mouseSensitivity * Time.fixedDeltaTime));
        
        return new GameInput(jump, moveLeft, moveRight, moveForward, moveBackward);
    }

    private void GetUserActionInput()
    {
        bool jump = false, moveLeft = false, moveRight = false, moveForward = false, moveBackward = false;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jump = true;
        }

        if (jump)
        {
            inputsToExecute.Add(new GameInput(jump, moveLeft, moveRight, moveForward, moveBackward));
        }
    }

    public void ClientFixedUpdate(Channel serverChannel)
    {
        if (isPlaying)
        {
            SendInputToServer(serverChannel);
            CheckForGameEvents(serverChannel);
        }
    }
    
    private void SendInputToServer(Channel serverChannel)
    {
        var packet = Packet.Obtain();
        
        // TODO Maybe separate in function
        Transform transform = players[id].transform;
        int actionInputsize = inputsToExecute.Count;
        foreach (var input in inputsToExecute)
        {
            sentInputs.Add(input.value);
            appliedInputs.Add(input.value);
            PlayerMotion.ApplyInput(input.value, clientController, gravityController, transform);
            lastClientInput += 1;
        }
        inputsToExecute.Clear();
        GameInput currentInput = GetUserInput();
        sentInputs.Add(currentInput.value);
        appliedInputs.Add(currentInput.value);
        PlayerMotion.ApplyInput(currentInput.value, clientController, gravityController, transform);
        lastClientInput += 1;
        packet.buffer.PutInt((int) PacketType.INPUT); // TODO compress each packet type
        packet.buffer.PutInt(id);
        packet.buffer.PutInt(lastInputRemoved + 1);
        GameInput.Serialize(sentInputs, lastInputSent, packet.buffer);
        packet.buffer.Flush();
        serverChannel.Send(packet, serverEndPoint);
        packet.Free();
        lastInputSent += 1 + actionInputsize;
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
            if (currentWorldInfo.players != null && nextWorldInfo.players != null)
            {
                foreach (var playerId in currentWorldInfo.players.Keys)
                {
                    if (playerId != id)
                    {
                        if (currentWorldInfo.players.ContainsKey(playerId) &&
                            nextWorldInfo.players.ContainsKey(playerId) &&
                            players.ContainsKey(playerId))
                        {
                            CubeEntity previousCubeEntity = currentWorldInfo.players[playerId];

                            CubeEntity nextCubeEntity = nextWorldInfo.players[playerId];
                            CubeEntity interpolatedCube = CubeEntity.CreateInterpolated(previousCubeEntity,
                                nextCubeEntity,
                                startTime, endTime, clientTime, players[playerId]);
                            interpolatedCube.Apply();
                        }
                    }
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
//            Debug.Log("Spawning own2 clientId = " + id);
            Vector3 position = clientCube.position;
            Quaternion rotation = Quaternion.Euler(clientCube.eulerAngles);
            players[id] = Object.Instantiate(clientPrefab, position, rotation) as GameObject;
            isSpawned = true;
            clientController = players[id].GetComponent<CharacterController>();
            gravityController = players[id].GetComponent<GravityController>();
            playerPrediction = GameObject.Instantiate(simulationPrefab, position, rotation) as GameObject;
            predictionController = playerPrediction.GetComponent<CharacterController>();
            simulationGravityController = playerPrediction.GetComponent<GravityController>();
            playerCamera = players[id].GetComponentInChildren< Camera >();
            Physics.IgnoreCollision(playerPrediction.GetComponent<Collider>(), players[id].GetComponent<Collider>());
    }
    
    public void SpawnPlayer(int playerId, CubeEntity playerCube)
    {
        Vector3 position = playerCube.position;
        Quaternion rotation = Quaternion.Euler(playerCube.eulerAngles);
        GameObject player = GameObject.Instantiate(clientPrefab, position, rotation) as GameObject;
        players[playerId] = player;
    }
}
