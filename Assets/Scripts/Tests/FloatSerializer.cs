public class FloatSerializer
{

    public static void SerializeFloat(BitBuffer buffer, float value, int minValue, int maxValue, float step)
    {
        int representation = (int)((value - minValue) / step);
        int minRepresentation = 0;
        int maxRepresentation = (int) ((maxValue - minValue) / step);
        buffer.PutInt(representation, minRepresentation, maxRepresentation);
    }

    public static float DeserializeFloat(BitBuffer bitBuffer, int minValue, int maxValue, float step)
    {
        int minRepresentation = 0;
        int maxRepresentation = (int) ((maxValue - minValue) / step);
        int representation = bitBuffer.GetInt(minRepresentation, maxRepresentation);
        return minValue + representation * step;
    }

}
