# FPS-TAV

# Most Relevant Classes Implementation

## Client side

### ClientManager
    Handles client connection and spawns client object on scene.

### SimulationClient
    Handles all client relevant behaviour:
    * Receives server packets and process them.
    * Detects player inputs.
    * Send events and inputs to server.
    * Spawn and remove players according to server snapshots.
    * Predicts player position based on player position on server and inputs left to execute.

### ClientConfig
    Contains client start information, like client id, client port, channel, etc.

## Server side

### ServerManager
    Handles server instantiation and handles server lifecycle methods(Update and FixedUpdate).

### SimulationServer
    * Receives all clients information.
    * Send world info to all clients.
    * Manages all clients data.

### SpawnPositionGenerator
    Generates a random position within one of the spawning zones, also selected randomly.

## Used by Client and Server

### GameConfig
    Contains Game basic info like game mode, server ip and server port.

### GameInput
    Contains player input in a masked way, input can be float or integer.
     
    float is used to send player orientation.
    
    integer value is used to send player inputs such as moving left, right, jumping, etc.
    
    Also handles GameInput Serialization.

### PlayerEntity
    * Contains information related to player gameobject transform.
    * Handles serialization and player interpolation.

### PlayerInfo
    * Contains relevant player info such as its endpoint, life, damage, animation state, player entity, etc.
    * Handles PlayerInfo serialization.

### PlayerMotion
    Controls player movement based on given Game Inputs.

### WorldInfo
    * Contains relevant info of the world, it contains all players PlayerInfo.
    * Handles WorldInfo serialization.

### Snapshot
    * Is sent from server to client carrying WorldInfo
    * Contains a sequence number and a worldInfo
    * Handles Snapshot Serialization