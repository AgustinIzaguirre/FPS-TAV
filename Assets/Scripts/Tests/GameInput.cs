using System.Collections.Generic;

public class GameInput
{
    public int value;
    private static readonly int minValue = 0;
    private static readonly int maxValue = 1 << 4;
    public GameInput(bool jump, bool moveLeft, bool moveRight, bool moveForward, bool moveBackward)
    {
        int currentInput = 0;
        if (jump)
        {
            currentInput = (currentInput | (int) InputType.JUMP);
        }
        if (moveLeft)
        {
            currentInput = (currentInput | (int) InputType.LEFT);
        }
        if (moveRight)
        {
            currentInput = (currentInput | (int) InputType.RIGHT);
        }
        if (moveForward)
        {
            currentInput = (currentInput | (int) InputType.FORWARD);
        }
        if (moveBackward)
        {
            currentInput = (currentInput | (int) InputType.BACKWARD);
        }
        value = currentInput;
    }
    
    public static void Serialize(List<int> inputsToSend, int lastInputSent, BitBuffer buffer)
    {
        buffer.PutInt(inputsToSend.Count);
        for (int i = 0; i < inputsToSend.Count; i++)
        {
            buffer.PutInt(inputsToSend[i], minValue, maxValue);  
        }
        buffer.PutInt(lastInputSent);
    }
    
    public static List<int> Deserialize(BitBuffer buffer)
    {
        List<int> inputsToExecute = new List<int>();
        int count = buffer.GetInt();
        while (count > 0)
        {
            int currentInput = buffer.GetInt(minValue, maxValue);
            inputsToExecute.Add(currentInput);
            count -= 1;
        }
        return inputsToExecute;
    }
}