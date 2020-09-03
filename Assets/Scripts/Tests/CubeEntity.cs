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
        FloatSerializer.SerializeFloat(buffer, position.x, -40, 40, 0.001f);
//        buffer.PutFloat(position.x);
        buffer.PutFloat(position.y);
        FloatSerializer.SerializeFloat(buffer, position.z, -40, 40, 0.001f);
//        buffer.PutFloat(position.z);
        DegreeAngle.SerializeAngle(buffer, eulerAngles.x);
        DegreeAngle.SerializeAngle(buffer, eulerAngles.y);
        DegreeAngle.SerializeAngle(buffer, eulerAngles.z);
    }

    public void Deserialize(BitBuffer buffer) {
        position = new Vector3();
        eulerAngles = new Vector3();
        position.x = FloatSerializer.DeserializeFloat(buffer, -40, 40, 0.001f);
//        position.x = buffer.GetFloat();
        position.y = buffer.GetFloat();
        position.z = FloatSerializer.DeserializeFloat(buffer, -40, 40, 0.001f);
//        position.z = buffer.GetFloat();
        eulerAngles.x = DegreeAngle.DeserializeAngle(buffer);
        eulerAngles.y = DegreeAngle.DeserializeAngle(buffer);
        eulerAngles.z = DegreeAngle.DeserializeAngle(buffer);
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