using Tests;
using UnityEngine;

public class Interpolator
{
    public static float InterpolateFloat(float start, float end, float startTime, float endTime, float currentTime)
    {
        return start + ((end - start) / (endTime - startTime)) * (currentTime - startTime);
    }

    public static Vector3 InterpolateVector3(Vector3 start, Vector3 end, float startTime, float endTime, float currentTime)
    {
//        float currentX = InterpolateFloat(start.x, end.x, startTime, endTime, currentTime);
//        float currentY = InterpolateFloat(start.y, end.y, startTime, endTime, currentTime);
//        float currentZ = InterpolateFloat(start.z, end.z, startTime, endTime, currentTime);
//        return new Vector3(currentX, currentY, currentZ);
        float normalizedTime = (currentTime - startTime) / (endTime - startTime);
        return Vector3.Lerp(start, end, normalizedTime);
    }
}
