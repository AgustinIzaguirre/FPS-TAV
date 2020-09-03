public class DegreeAngle
{
    private float angle;
    private static int minAngle = 0;
    private static int maxAngle = 360;
    private static float angleStep = 0.1f;
    private static int maxValue = (int) ((maxAngle - minAngle) / angleStep);
    private static int minValue = 0;

    public DegreeAngle(float angle)
    {
        this.angle = angle;
    }

    public static void SerializeAngle(BitBuffer buffer, float angle)
    {
        int angle_representation = GetAngleRepresentation(angle);
        buffer.PutInt(angle_representation, minValue, maxValue);
    }

    private static int GetAngleRepresentation(float angle)
    {
        float current_angle = angle - minAngle;
        int angleRepresentation = (int) (current_angle / angleStep);
        return angleRepresentation;
    }

    public static float DeserializeAngle(BitBuffer buffer)
    {
        int currentRepresentation = buffer.GetInt(minValue, maxValue);
        float currentAngle = minAngle + currentRepresentation * angleStep;
        return currentAngle;
    }
}
