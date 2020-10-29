using System;
using UnityEngine;

public class PlayerEntity
{
    private static int maxSpaceX = 100;
    private static int minSpaceX = -100;
    private static int maxSpaceZ = 100;
    private static int minSpaceZ = -100;

    public Vector3 position;
    public Vector3 eulerAngles;
    public GameObject playerObject;
    public float verticalVelocity;
    
    public PlayerEntity(GameObject playerObject) {
        this.playerObject = playerObject;
    }

    public PlayerEntity(GameObject playerObject, Vector3 position, Vector3 eulerAngles)
    {
        this.playerObject = playerObject;
        this.position = position;
        this.eulerAngles = eulerAngles;
    }
    
    public PlayerEntity(GameObject playerObject, float verticalVelocity) {
        this.playerObject = playerObject;
        this.verticalVelocity = verticalVelocity;
    }
    
    public void Serialize(BitBuffer buffer) {
        var transform = playerObject.transform;
        var position = transform.position;
        var eulerAngles = transform.eulerAngles;
        FloatSerializer.SerializeFloat(buffer, position.x, minSpaceX, maxSpaceX, 0.001f);
//        buffer.PutFloat(position.x);
        buffer.PutFloat(position.y);
        FloatSerializer.SerializeFloat(buffer, position.z, minSpaceZ, maxSpaceZ, 0.001f);
//        buffer.PutFloat(position.z);
        DegreeAngle.SerializeAngle(buffer, eulerAngles.x);
        DegreeAngle.SerializeAngle(buffer, eulerAngles.y);
        DegreeAngle.SerializeAngle(buffer, eulerAngles.z);
        buffer.PutFloat(this.verticalVelocity);
    }

    public void Deserialize(BitBuffer buffer) {
        position = new Vector3();
        eulerAngles = new Vector3();
        position.x = FloatSerializer.DeserializeFloat(buffer, minSpaceX, maxSpaceX, 0.001f);
        position.y = buffer.GetFloat();
        position.z = FloatSerializer.DeserializeFloat(buffer, minSpaceZ, maxSpaceZ, 0.001f);
        eulerAngles.x = DegreeAngle.DeserializeAngle(buffer);
        eulerAngles.y = DegreeAngle.DeserializeAngle(buffer);
        eulerAngles.z = DegreeAngle.DeserializeAngle(buffer);
        verticalVelocity = buffer.GetFloat();
    }

    public static PlayerEntity DeserializeInfo(BitBuffer buffer)
    {
        PlayerEntity newPlayerEntity = new PlayerEntity(null);
        newPlayerEntity.position = new Vector3();
        newPlayerEntity.eulerAngles = new Vector3();
        newPlayerEntity.position.x = FloatSerializer.DeserializeFloat(buffer, minSpaceX, maxSpaceX, 0.001f);
        newPlayerEntity.position.y = buffer.GetFloat();
        newPlayerEntity.position.z = FloatSerializer.DeserializeFloat(buffer, minSpaceZ, maxSpaceZ, 0.001f);
        newPlayerEntity.eulerAngles.x = DegreeAngle.DeserializeAngle(buffer);
        newPlayerEntity.eulerAngles.y = DegreeAngle.DeserializeAngle(buffer);
        newPlayerEntity.eulerAngles.z = DegreeAngle.DeserializeAngle(buffer);
        newPlayerEntity.verticalVelocity = buffer.GetFloat();
        return newPlayerEntity;
    }
    
    public void Apply()
    {
        Transform transform = playerObject.transform;
        transform.position = position;
        transform.eulerAngles = eulerAngles;
    }
    
    public static PlayerEntity CreateInterpolated(PlayerEntity previous, PlayerEntity next, float startTime, float endTime,
        float currentTime, GameObject cube) {
        var newPlayerEntity = new PlayerEntity(cube);
        newPlayerEntity.position += Interpolator.InterpolateVector3(previous.position, next.position, startTime,
            endTime, currentTime);
        newPlayerEntity.eulerAngles += Interpolator.InterpolateVector3(previous.eulerAngles, next.eulerAngles, 
            startTime, endTime, currentTime);
        return newPlayerEntity;
    }

    public bool IsEqual(PlayerEntity other, float positionThreshold, float rotationThreshold)
    {
        Vector3 position = playerObject.transform.position;
        Vector3 rotation = playerObject.transform.eulerAngles;
        Vector3 otherPosition = other.playerObject.transform.position;
        Vector3 otherRotation = other.playerObject.transform.eulerAngles;
        if (Math.Abs(position.x - otherPosition.x) > positionThreshold ||
            Math.Abs(position.y - otherPosition.y) > positionThreshold ||
            Math.Abs(position.z - otherPosition.z) > positionThreshold)
        {
            Debug.Log("Position not equal");
            return false;
        }

//        if ((Math.Abs(rotation.x - otherRotation.x) > rotationThreshold && 
//             Math.Abs((360.0 - rotation.x) - otherRotation.x) > rotationThreshold)||
//            (Math.Abs(rotation.y - otherRotation.y) > rotationThreshold &&
//             Math.Abs((360.0 - rotation.y) - otherRotation.y) > rotationThreshold) ||
//            (Math.Abs(rotation.z - otherRotation.z) > rotationThreshold &&
//             Math.Abs(360.0 - rotation.z - otherRotation.z) > rotationThreshold))
//        {
//            Debug.Log("Rotation not equal");
//            return false;
//        }

        return true;
    }
}