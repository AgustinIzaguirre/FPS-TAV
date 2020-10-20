using System;
using UnityEngine;

public class CubeEntity
{
    private static int maxSpaceX = 100;
    private static int minSpaceX = -100;
    private static int maxSpaceZ = 100;
    private static int minSpaceZ = -100;

    public Vector3 position;
    public Vector3 eulerAngles;
    public GameObject cubeGameObject;
    public float verticalVelocity;
    
    public CubeEntity(GameObject cubeGameObject) {
        this.cubeGameObject = cubeGameObject;
    }

    public CubeEntity(GameObject cubeGameObject, Vector3 position, Vector3 eulerAngles)
    {
        this.cubeGameObject = cubeGameObject;
        this.position = position;
        this.eulerAngles = eulerAngles;
    }
    
    public CubeEntity(GameObject cubeGameObject, float verticalVelocity) {
        this.cubeGameObject = cubeGameObject;
        this.verticalVelocity = verticalVelocity;
    }
    
    public void Serialize(BitBuffer buffer) {
        var transform = cubeGameObject.transform;
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

    public static CubeEntity DeserializeInfo(BitBuffer buffer)
    {
        CubeEntity newCubeEntity = new CubeEntity(null);
        newCubeEntity.position = new Vector3();
        newCubeEntity.eulerAngles = new Vector3();
        newCubeEntity.position.x = FloatSerializer.DeserializeFloat(buffer, minSpaceX, maxSpaceX, 0.001f);
        newCubeEntity.position.y = buffer.GetFloat();
        newCubeEntity.position.z = FloatSerializer.DeserializeFloat(buffer, minSpaceZ, maxSpaceZ, 0.001f);
        newCubeEntity.eulerAngles.x = DegreeAngle.DeserializeAngle(buffer);
        newCubeEntity.eulerAngles.y = DegreeAngle.DeserializeAngle(buffer);
        newCubeEntity.eulerAngles.z = DegreeAngle.DeserializeAngle(buffer);
        return newCubeEntity;
    }
    
    public void Apply()
    {
        Transform transform = cubeGameObject.transform;
        transform.position = position;
        transform.eulerAngles = eulerAngles;
    }
    
    public static CubeEntity CreateInterpolated(CubeEntity previous, CubeEntity next, float startTime, float endTime,
        float currentTime, GameObject cube) {
        var cubeEntity = new CubeEntity(cube);
        cubeEntity.position += Interpolator.InterpolateVector3(previous.position, next.position, startTime,
            endTime, currentTime);
        cubeEntity.eulerAngles += Interpolator.InterpolateVector3(previous.eulerAngles, next.eulerAngles, 
            startTime, endTime, currentTime);
        return cubeEntity;
    }

    public bool IsEqual(CubeEntity other, float positionThreshold, float rotationThreshold)
    {
        Vector3 position = cubeGameObject.transform.position;
        Vector3 rotation = cubeGameObject.transform.eulerAngles;
        Vector3 otherPosition = other.cubeGameObject.transform.position;
        Vector3 otherRotation = other.cubeGameObject.transform.eulerAngles;
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