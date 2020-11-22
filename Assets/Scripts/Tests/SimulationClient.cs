using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Tests;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class SimulationClient : MonoBehaviour
{
    [SerializeField]
    private GameObject enemyPrefab;
    [SerializeField]
    private Camera playerCamera;
    [SerializeField]
    private CharacterController clientController;
    [SerializeField]
    private GravityController gravityController;

    
    private float xRotation = 0f;

    private GameObject bulletTrailPrefab;
    private CharacterController predictionController;
    private GravityController simulationGravityController;
    private DamageScreenController damageScreenController;
    private HealthController healthController;
    private Channel channel;
    private List<GameInput> sentInputs;
    private List<GameInput> appliedInputs;
    private List<GameEvent> sentEvents;
    private List<ShootEvent> sentShootEvents;
    private List<Snapshot> interpolationBuffer;
    private List<GameInput> inputsToExecute;
    private Dictionary<int, PlayerInfo> players;
    private Dictionary<int, EnemyAnimatorController> enemyAnimators;
    private GameObject playerPrediction;
    private int lastInputSent;
    private int lastInputRemoved;
    private int lastClientInput;
    private int lastServerInput;
    private int minSnapshots;
    private bool render;
    private float clientTime;
    private float timeToSend;
    private float timeout;
    private int eventNumber;
    private int shootEventNumber;
    private int id;
    private bool isPlaying;
    private IPEndPoint serverEndPoint;
    private Weapon weapon;
    private bool isAlive;
    private float delay;

    public SimulationClient(int portNumber, int minSnapshots, float timeToSend, float timeout, int id,
        IPEndPoint serverEndPoint, GameObject clientPrefab, GameObject simulationPrefab, GameObject enemyPrefab,
        GameObject bulletTrailPrefab)
    {
//        channel = new Channel(portNumber);
//        interpolationBuffer = new List<Snapshot>();
//        players = new Dictionary<int, PlayerInfo>();
//        enemyAnimators = new Dictionary<int, EnemyAnimatorController>();
//        sentInputs = new List<GameInput>();
//        appliedInputs = new List<GameInput>();
//        sentEvents = new List<GameEvent>();
//        sentShootEvents = new List<ShootEvent>();
//        inputsToExecute = new List<GameInput>();
//        render = false;
//        clientController = null;
//        this.minSnapshots = minSnapshots;
//        this.timeToSend = timeToSend;
//        this.timeout = timeout;
//        this.id = id;
//        this.serverEndPoint = serverEndPoint;
//        this.clientPrefab = clientPrefab;
//        this.bulletTrailPrefab = bulletTrailPrefab;
//        lastInputSent = 1;
//        clientTime = 0f;
//        eventNumber = 0;
//        shootEventNumber = 0;
//        isSpawned = false;
//        isPlaying = false;
//        lastClientInput = 0;
//        lastServerInput = 0;
//        this.simulationPrefab = simulationPrefab;
//        this.enemyPrefab = enemyPrefab;
//        weapon = null;
//        isAlive = true;
//        delay = 0f;
    }

    public void Start()
    {
        channel = ClientConfig.GetChannel();
        minSnapshots = ClientConfig.GetMinSnapshots();
        timeToSend = ClientConfig.GetTimeToSend();
        timeout = ClientConfig.GetTimeout();
        id = ClientConfig.GetId();
        playerPrediction = ClientConfig.GetPlayerPrediction();
        bulletTrailPrefab = ClientConfig.GetBulletTrailPrefab();
        serverEndPoint = GameConfig.GetServerEndPoint();
        predictionController = playerPrediction.GetComponent<CharacterController>();
        simulationGravityController = playerPrediction.GetComponent<GravityController>();
        interpolationBuffer = new List<Snapshot>();
        players = new Dictionary<int, PlayerInfo>();
        players[id] = ClientConfig.GetPlayerInfo();
        enemyAnimators = new Dictionary<int, EnemyAnimatorController>();
        sentInputs = new List<GameInput>();
        appliedInputs = new List<GameInput>();
        sentEvents = new List<GameEvent>();
        sentShootEvents = new List<ShootEvent>();
        inputsToExecute = new List<GameInput>();
        render = false;
        lastInputSent = 1;
        clientTime = 0f;
        eventNumber = 0;
        shootEventNumber = 0;
        isPlaying = false;
        lastClientInput = 0;
        lastServerInput = 0;
        weapon = new Weapon(0.2f,  playerCamera.GetComponent<AudioSource>(),
            playerCamera.GetComponent<MuzzleFlash>(), bulletTrailPrefab);
        damageScreenController = playerCamera.GetComponent<DamageScreenController>();
        healthController = playerCamera.GetComponent<HealthController>();
        isAlive = true;
        delay = 0f;
        
    }

    public void Update()
    {
        if (!isAlive)
        {
            return;
        }
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
//                        Debug.Log("Receiving snapshot");
                        int lastInput = currentSnapshot.worldInfo.players[id].lastInputApplied;
                        AddToInterpolationBuffer(currentSnapshot);
                        if (render)
                        {
                            Interpolate();
                            if (!isPlaying)
                            {
                                return;
                            }

                            if (currentSnapshot.worldInfo.players[id].GetAnimationState() == AnimationStates.MOVE)
                            {
                                Debug.Log("MOVE");
                            }
                            else if (currentSnapshot.worldInfo.players[id].GetAnimationState() == AnimationStates.SHOOT)
                            {
                                    Debug.Log("SHOOT");
                            }

                            if (currentSnapshot.worldInfo.players[id].playerEntity != null)
                            {
                                CalculatePrediction(currentSnapshot.worldInfo.players[id].playerEntity);
                                PlayerEntity predictionEntity = new PlayerEntity(playerPrediction);
                                PlayerEntity playerEntity = new PlayerEntity(players[id].playerGameObject);
                                RemoveInputsFromList(lastServerInput, lastInput, appliedInputs);
                                lastServerInput = lastInput;
                                if (!predictionEntity.IsEqual(playerEntity, 0.2f, 50))
                                {
//                                    Debug.Log("Not equals");
                                    Vector3 newPosition = playerPrediction.transform.position;
                                    if (Math.Abs(playerPrediction.transform.position.y -
                                                 players[id].playerGameObject.transform.position.y) <= 1.0f)
                                    {
                                        newPosition.y = players[id].playerGameObject.transform.position.y;
                                    }

                                    players[id].playerGameObject.transform.position = newPosition;
                                }
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
                else if (packetType == (int) PacketType.SHOOT_EVENT)
                {
                    int ackNumber = packet.buffer.GetInt();
                    for (int i = 0; i < sentShootEvents.Count; i++)
                    {
                        if (ackNumber == sentShootEvents[i].shootEventNumber)
                        {
                            sentShootEvents.RemoveAt(i);
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
//                    Debug.Log("New player: " + playerId);
                    if (GameConfig.GetGameMode() != GameMode.BOTH || id == 1)
                    {
//                        Debug.Log("Enters if id = " + id + ", receivedId = " + playerId);
                        if (id != playerId)
                        {
                            SpawnPlayer(playerId, newPlayerEvent.newPlayer);
                        }
                    }
                }
                SendAck((int) PacketType.NEW_PLAYER, playerId);
            }
            if (packetType == (int) PacketType.START_INFO)
            {
//                Debug.Log("Receive Start Info");
                WorldInfo worldInfo = WorldInfo.Deserialize(packet.buffer);
                foreach (var playerId in worldInfo.players.Keys)
                {
                    if (playerId != id && !players.ContainsKey(playerId) && worldInfo.players[playerId].isAlive)
                    {
                        SpawnPlayer(playerId, worldInfo.players[playerId].playerEntity);
                    }
                }
//                    Debug.Log("Client instantiated");
                isPlaying = true;
                players[id].ActivatePlayer();
                SendAck((int) PacketType.START_INFO, id);
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
//        Debug.Log("Calculating prediction");
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
        return new GameInput(mouseX, players[id].playerGameObject.transform.eulerAngles);
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
        if (isAlive && Input.GetMouseButton(0) && weapon.Shoot(clientTime))
        {
            int targetId = Shoot();
            if (targetId >= 0)
            {
//                Debug.Log("Sending shoot event hit player = " + targetId);
                shootEventNumber++;
                SendShootEventToServer(new ShootEvent(id, targetId, clientTime, shootEventNumber));
            }
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jump = true;
        }

        if (jump)
        {
            inputsToExecute.Add(new GameInput(jump, moveLeft, moveRight, moveForward, moveBackward));
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            delay = Math.Min(0.5f, delay + 0.1f);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            delay = 0f;
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void FixedUpdate()
    {
        if (isPlaying)
        {
            SendInputToServer();
            ResendShootEvents();
            CheckForGameEvents();
        }
    }
    
    private void SendInputToServer()
    {
        var packet = Packet.Obtain();
        
        // TODO Maybe separate in function
        Transform transform = players[id].playerGameObject.transform;
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
        SendWithDelay(packet, serverEndPoint, (int)(delay * 1000));
        // lastClientInput - lastInputSent because it can send one or two inputs depending on rotation
        lastInputSent += (lastClientInput - lastInputSent) + actionInputsize;
    }

    private void SendWithDelay(Packet packet, IPEndPoint destination, int delayTimeInMs)
    {
        Task.Delay(delayTimeInMs).ContinueWith(t=> channel.Send(packet, destination));
    }

    private void ResendShootEvents()
    {
        if (sentShootEvents.Count > 0)
        {
            ShootEvent firstEvent = sentShootEvents[0];
            while (firstEvent != null && clientTime - firstEvent.time >= timeout)
            {
                sentShootEvents.RemoveAt(0);
                shootEventNumber++;
                ShootEvent newEvent = new ShootEvent(firstEvent.shooterId, firstEvent.targetId, clientTime, shootEventNumber);
                SendShootEventToServer(newEvent);
                firstEvent = sentShootEvents.Count > 0 ? sentShootEvents[0] : null;
            }
        }
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

    private void SendShootEventToServer(ShootEvent currentShootEvent)
    {
        var packet = Packet.Obtain();
        sentShootEvents.Add(currentShootEvent);
        currentShootEvent.Serialize(packet.buffer, id);
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
                    if (playerId != id && isPlaying)
                    {
                        if (currentWorldInfo.players[playerId].life <= 0.001)
                        {
//                            Debug.Log("Player " + playerId + " is dead on client");
                            if (players[playerId].isAlive)
                            {
                                players[playerId].MarkAsDead();
                                enemyAnimators[playerId].Kill();
                            }
                        }
                        else if (currentWorldInfo.players.ContainsKey(playerId) &&
                            nextWorldInfo.players.ContainsKey(playerId) &&
                            players.ContainsKey(playerId) && next.worldInfo.players[playerId].isAlive)
                        {
                            PlayerEntity previousPlayerEntity = currentWorldInfo.players[playerId].playerEntity;

                            PlayerEntity nextPlayerEntity = nextWorldInfo.players[playerId].playerEntity;
                            PlayerEntity interpolatedPlayer = PlayerEntity.CreateInterpolated(previousPlayerEntity,
                                nextPlayerEntity,
                                startTime, endTime, clientTime, players[playerId].playerGameObject);
                            interpolatedPlayer.Apply();
                            enemyAnimators[playerId].ApplyAnimation(nextWorldInfo.players[playerId].animationState);
                        }
                    }
                    else if (currentWorldInfo.players[playerId].life <= 0.001)
                    {
                        KillPlayer();
                        return;
                    }
                    else if ((int)(currentWorldInfo.players[playerId].life + 0.5f) < (int)(players[playerId].life + 0.5f))
                    {
                        players[playerId].life = currentWorldInfo.players[playerId].life;
                        healthController.UpdateLife(players[playerId].life);
                        damageScreenController.Activate();
                    }
                }
            }
        }
    }

    private void KillPlayer()
    {
        isAlive = false; 
        damageScreenController.Activate();
        isPlaying = false;
        render = false;
        players[id].MarkAsDead();
        LoadEndGameScene();
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

//    public void Spawn(PlayerEntity player)
//    {
//            Vector3 position = player.position;
//            Quaternion rotation = Quaternion.Euler(player.eulerAngles);
//            GameObject playerObject = Object.Instantiate(clientPrefab, position, rotation) as GameObject;
//            players[id] = new PlayerInfo(id, new PlayerEntity(playerObject), false);
//            isSpawned = true;
//            clientController = players[id].playerGameObject.GetComponent<CharacterController>();
//            gravityController = players[id].playerGameObject.GetComponent<GravityController>();
//            playerPrediction = GameObject.Instantiate(simulationPrefab, position, rotation) as GameObject;
//            predictionController = playerPrediction.GetComponent<CharacterController>();
//            simulationGravityController = playerPrediction.GetComponent<GravityController>();
//            playerCamera = players[id].playerGameObject.GetComponentInChildren< Camera >();
//            weapon = new Weapon(0.2f,  playerCamera.GetComponent<AudioSource>(),
//                playerCamera.GetComponent<MuzzleFlash>(), bulletTrailPrefab);
//            damageScreenController = playerCamera.GetComponent<DamageScreenController>();
//            healthController = playerCamera.GetComponent<HealthController>();
//            Physics.IgnoreCollision(playerPrediction.GetComponent<Collider>(),
//                players[id].playerGameObject.GetComponent<Collider>());
//    }
    
    public void SpawnPlayer(int playerId, PlayerEntity playerObject)
    {
        Vector3 position = playerObject.position;
        Quaternion rotation = Quaternion.Euler(playerObject.eulerAngles);
        GameObject playerGameobject = GameObject.Instantiate(enemyPrefab, position, rotation) as GameObject;
        EnemyInfo enemyInfo = playerGameobject.GetComponent<EnemyInfo>();
        enemyAnimators[playerId] = new EnemyAnimatorController(playerGameobject.GetComponent<Animator>());
        enemyInfo.SetId(playerId);
        players[playerId] = new PlayerInfo(playerId, new PlayerEntity(playerGameobject), true);
    }

    public int Shoot()
    {
        int targetId = -1;
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit))
        {
            if (hit.transform.name.Contains("Enemy"))
            {
                targetId = hit.transform.GetComponent<EnemyInfo>().GetId();
//                Debug.Log("hit player = " + targetId);
            }

            weapon.SpawnBullet(hit.point);
        }

        return targetId;
    }

    public void LoadEndGameScene()
    {
        DestroyChannel();
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene("EndGame");
    }
}
