using System.Collections.Generic;
using System.Net;
using Tests;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class SimulationClient
{
    private GameObject clientPrefab;
    private GameObject simulationPrefab;
    private GameObject enemyPrefab;

    private float xRotation = 0f;
    
    private Camera playerCamera;
    private CharacterController clientController;
    private CharacterController predictionController;
    private GravityController gravityController;
    private GravityController simulationGravityController;
    private Channel channel;
    private List<GameInput> sentInputs;
    private List<GameInput> appliedInputs;
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
        IPEndPoint serverEndPoint, GameObject clientPrefab, GameObject simulationPrefab, GameObject enemyPrefab)
    {
        channel = new Channel(portNumber);
        interpolationBuffer = new List<Snapshot>();
        players = new Dictionary<int, GameObject>();
        sentInputs = new List<GameInput>();
        appliedInputs = new List<GameInput>();
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
        this.enemyPrefab = enemyPrefab;
    }

    public void UpdateClient()
    {
        
        if (render)
        {
            clientTime += Time.deltaTime;
        }

        if (isPlaying)
        {
            GetUserActionInput();
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
                    // TODO seems to be failing when trying to get key from snapshot
//                    if (currentSnapshot.worldInfo.playerAppliedInputs.ContainsKey(id))
//                    {
                        Debug.Log("Receiving snapshot");
                        int lastInput = currentSnapshot.worldInfo.players[id].lastInputApplied;
                        AddToInterpolationBuffer(currentSnapshot);
                        if (render)
                        {
                            Interpolate();

                            CalculatePrediction(currentSnapshot.worldInfo.players[id].playerEntity);
                            PlayerEntity predictionEntity = new PlayerEntity(playerPrediction);
                            PlayerEntity playerEntity = new PlayerEntity(players[id]);
                            RemoveInputsFromList(lastServerInput, lastInput, appliedInputs);
                            lastServerInput = lastInput;
                            if (!predictionEntity.IsEqual(playerEntity, 0.2f, 50))
                            {
                                Debug.Log("Not equals");
                                players[id].transform.position = playerPrediction.transform.position;
//                            players[id].transform.rotation = playerPrediction.transform.rotation;
                            }

                        }
//                    }
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
                    Debug.Log("New player: " + playerId);

                    if (GameConfig.GetGameMode() != GameMode.BOTH || id == 1)
                    {
                        Debug.Log("Enters if id = " + id + ", receivedId = " + playerId);
                        if (id == playerId)
                        {
                            Spawn(newPlayerEvent.newPlayer);
                        }
                        else
                        {
                            SpawnPlayer(playerId, newPlayerEvent.newPlayer);
                        }
                    }
                }
                SendAck((int) PacketType.NEW_PLAYER, playerId);
            }
            if (packetType == (int) PacketType.START_INFO)
            {
                Debug.Log("Receive Start Info");
                if (isSpawned)
                {
                    WorldInfo worldInfo = WorldInfo.Deserialize(packet.buffer);
                    foreach (var playerId in worldInfo.players.Keys)
                    {
                        if (playerId != id && !players.ContainsKey(playerId))
                        {
                            SpawnPlayer(playerId, worldInfo.players[playerId].playerEntity);
                        }
                    }
                    Debug.Log("Client instantiated");
                    isPlaying = true;
                    SendAck((int) PacketType.START_INFO, id);
                }
            }

            packet.Free();
            packet = channel.GetPacket();
        } 
    }

    private void RemoveInputsFromList(int start, int end, List<GameInput> list)
    {
        int size = list.Count;
        for (int i = 0; i < size && i <= end - start; i++)
        {
            list.RemoveAt(0);
        }
    }

    private void CalculatePrediction(PlayerEntity serverPlayer)
    {
        Debug.Log("Calculating prediction");
        playerPrediction.transform.position = serverPlayer.position;
        playerPrediction.transform.eulerAngles = serverPlayer.eulerAngles;
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

    private void SendAck(int packetType, int playerId)
    {
        var packet = Packet.Obtain();
        packet.buffer.PutInt(packetType);
        packet.buffer.PutInt(id);
        packet.buffer.PutInt(playerId);
        packet.buffer.Flush();
        channel.Send(packet, serverEndPoint);
        packet.Free();    
    }

    private GameInput GetUserMovementInput()
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
        
        return new GameInput(jump, moveLeft, moveRight, moveForward, moveBackward);
    }
    
    private GameInput GetRotationInput()
    {
        RotateCamera();
        float mouseX = Input.GetAxis("Mouse X");
        return new GameInput(mouseX, players[id].transform.eulerAngles);
    }

    private void GetUserInputs(Transform transform)
    {
        // Movement inputs
        GameInput movementInput = GetUserMovementInput();
        sentInputs.Add(movementInput);
        appliedInputs.Add(movementInput);
        PlayerMotion.ApplyInput(movementInput, clientController, gravityController, transform);
        lastClientInput += 1;
        
        // Rotation inputs
        GameInput rotationInput = GetRotationInput();
        if (rotationInput.floatValue != 0f)
        {
            sentInputs.Add(rotationInput);
            appliedInputs.Add(rotationInput);
            PlayerMotion.ApplyInput(rotationInput, clientController, gravityController, transform);
            lastClientInput += 1;
        }
    }

    private void RotateCamera()
    {
        float mouseY = Input.GetAxis("Mouse Y");
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void GetUserActionInput()
    {
        bool jump = false, moveLeft = false, moveRight = false, moveForward = false, moveBackward = false;
        if (Input.GetMouseButton(0))
        {
            Shoot();  
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jump = true;
        }

        if (jump)
        {
            inputsToExecute.Add(new GameInput(jump, moveLeft, moveRight, moveForward, moveBackward));
        }
    }

    public void ClientFixedUpdate()
    {
        if (isPlaying)
        {
            SendInputToServer();
            CheckForGameEvents();
        }
    }
    
    private void SendInputToServer()
    {
        var packet = Packet.Obtain();
        
        // TODO Maybe separate in function
        Transform transform = players[id].transform;
        int actionInputsize = inputsToExecute.Count;
        foreach (var input in inputsToExecute)
        {
            sentInputs.Add(input);
            appliedInputs.Add(input);
            PlayerMotion.ApplyInput(input, clientController, gravityController, transform);
            lastClientInput += 1;
        }
        inputsToExecute.Clear();
        
        GetUserInputs(transform);

        packet.buffer.PutInt((int) PacketType.INPUT); // TODO compress each packet type
        packet.buffer.PutInt(id);
        packet.buffer.PutInt(lastInputRemoved + 1);
        GameInput.Serialize(sentInputs, lastInputSent, packet.buffer);
        packet.buffer.Flush();
        channel.Send(packet, serverEndPoint);
        packet.Free();
        // lastClientInput - lastInputSent because it can send one or two inputs depending on rotation
        lastInputSent += (lastClientInput - lastInputSent) + actionInputsize;
    }

    private void CheckForGameEvents()
    {
        GameEvent currentEvent = GetGameEvent();
        if (currentEvent != null)
        {
            SendEventToServer(currentEvent);
        }
        ResendAndDeleteEvent();
    }

    private void SendEventToServer(GameEvent currentEvent)
    {
        var packet = Packet.Obtain();
        sentEvents.Add(currentEvent);
        currentEvent.Serialize(packet.buffer, id);
        packet.buffer.Flush();
        channel.Send(packet, serverEndPoint);
        packet.Free();
        
    }

    private void ResendAndDeleteEvent()
    {
        if (sentEvents.Count > 0)
        {
            GameEvent firstEvent = sentEvents[0];
            while (firstEvent != null && clientTime - firstEvent.time >= timeout)
            {
                sentEvents.RemoveAt(0);
                eventNumber++;
                GameEvent newEvent = new GameEvent(firstEvent.name, firstEvent.value, clientTime, eventNumber);
                SendEventToServer(newEvent);
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
                            PlayerEntity previousPlayerEntity = currentWorldInfo.players[playerId].playerEntity;

                            PlayerEntity nextPlayerEntity = nextWorldInfo.players[playerId].playerEntity;
                            PlayerEntity interpolatedPlayer = PlayerEntity.CreateInterpolated(previousPlayerEntity,
                                nextPlayerEntity,
                                startTime, endTime, clientTime, players[playerId]);
                            interpolatedPlayer.Apply();
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

    public void Spawn(PlayerEntity player)
    {
            Vector3 position = player.position;
            Quaternion rotation = Quaternion.Euler(player.eulerAngles);
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
    
    public void SpawnPlayer(int playerId, PlayerEntity playerObject)
    {
        Vector3 position = playerObject.position;
        Quaternion rotation = Quaternion.Euler(playerObject.eulerAngles);
        GameObject player = GameObject.Instantiate(enemyPrefab, position, rotation) as GameObject;
        EnemyInfo enemyInfo = player.GetComponent<EnemyInfo>();
        enemyInfo.SetId(playerId);
        players[playerId] = player;
    }

    public void Shoot()
    {
        playerCamera.GetComponent<AudioSource>().Play();
        playerCamera.GetComponent<MuzzleFlash>().PlayMuzzleFlash(); //TODO improve particle system position

        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit))
        {
            if (hit.transform.name.Contains("Enemy"))
            {
                Debug.Log(hit.transform.GetComponent<EnemyInfo>().GetId());
            }
        }
    }
}
