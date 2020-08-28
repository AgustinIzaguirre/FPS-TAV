using UnityEngine;

public class CubeEntity
{
    public Vector3 position;
    public Vector3 eulerAngles;
    public GameObject cubeGameObject;

    public CubeEntity(GameObject cubeGameObject) {
        this.cubeGameObject = cubeGameObject;
    }

    public void Serialize(BitBuffer buffer) {
        var transform = cubeGameObject.transform;
        var position = transform.position;
        var eulerAngles = transform.eulerAngles;
        buffer.PutFloat(position.x);
        buffer.PutFloat(position.y);
        buffer.PutFloat(position.z);
        buffer.PutFloat(eulerAngles.x);
        buffer.PutFloat(eulerAngles.y);
        buffer.PutFloat(eulerAngles.z);
    }

    public void Deserialize(BitBuffer buffer) {
        position = new Vector3();
        eulerAngles = new Vector3();
        position.x = buffer.GetFloat();
        position.y = buffer.GetFloat();
        position.z = buffer.GetFloat();
        eulerAngles.x = buffer.GetFloat();
        eulerAngles.y = buffer.GetFloat();
        eulerAngles.z = buffer.GetFloat();
        cubeGameObject.transform.position = position;
        cubeGameObject.transform.eulerAngles = eulerAngles;
    }
    
    public void Apply()
    {
        Transform transform = cubeGameObject.transform;
        transform.position = position;
        transform.eulerAngles = eulerAngles;
    }
    
    public static CubeEntity CreateInterpolated(CubeEntity previous, CubeEntity next, float startTime, float endTime,
        float currentTime) {
        var cubeEntity = new CubeEntity(previous.cubeGameObject);
        cubeEntity.position += Interpolator.InterpolateVector3(previous.position, next.position, startTime,
            endTime, currentTime);
        cubeEntity.eulerAngles += Interpolator.InterpolateVector3(previous.eulerAngles, next.eulerAngles, 
            startTime, endTime, currentTime);
        return cubeEntity;
    }
}