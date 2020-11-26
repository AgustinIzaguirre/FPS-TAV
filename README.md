# FPS-TAV

# Most Relevant Classes Implementation

## Client side

### ClientManager
    Handles client connection and spawns client object on scene

### SimulationClient
    Handles all client relevant behaviour, receives server packets and process them, detects player inputs, send events and inputs to server, spawn and remove players according to server snapshots and predicts player position based on player position on server and inputs left to execute.

### ClientConfig
    Contains client start information, like client id, client port, channel, etc.

## Server side

### ServerManager
    Handles server instantiation and handles server lifecycle methods(Update and FixedUpdate)

### SimulationServer
    Receives all clients information, send world info to all clients and manages all clients data

### SpawnPositionGenerator
    Generates a random position within one of the spawning zones, also selected randomly.

## Used by Client and Server

### GameConfig
    Contains Game basic info like game mode, server ip and server port.

### GameInput
    Contains player input in a masked way, input can be float or integer, float is used to send player orientation, while integer value is used to send player inputs such as moving left, right, jumping, etc.
    Also handles GameInput Serialization.

### PlayerEntity
    Contains information related to player gameobject transform, and also handles serialization and player interpolation

### PlayerInfo
    Contains relevant info of player such as its endpoint, life, damage, animation state, player entity, etc.
    Also handles PlayerInfo serialization

### PlayerMotion
    Controls player movement based on given Game Inputs

### WorldInfo
    Contains relevant info of the world, it contains all players PlayerInfo. Also handles WorldInfo serialization.

### Snapshot
    Is sent from server to client and it contains a sequence number and a worldInfo and also handles Snapshot Serialization