using UnityEngine;

public class Interpolator
{
    public static float InterpolateFloat(float start, float end, float startTime, float endTime, float currentTime)
    {
        return start + ((end - start) / (endTime - startTime)) * (currentTime - startTime);
    }

    public static Vector3 InterpolateVector3(Vector3 start, Vector3 end, float startTime, float endTime, float currentTime)
    {
        float normalizedTime = (currentTime - startTime) / (endTime - startTime);
        return Vector3.Lerp(start, end, normalizedTime);
    }
}