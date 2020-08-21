using System.Collections.Generic;

public class GameInput
{
    public int value;
    public GameInput(bool jump, bool moveLeft, bool moveRight) 
    {
        int currentInput = 0;
        if (jump)
        {
            currentInput = (currentInput | 1);
        }
        if (moveLeft)
        {
            currentInput = (currentInput | (1 << 1));
        }
        if (moveRight)
        {
            currentInput = (currentInput | (1 << 2));
        }
        value = currentInput;
    }
    
    public static void Serialize(List<int> inputsToSend, int lastInputSent, BitBuffer buffer)
    {
        buffer.PutInt(inputsToSend.Count);
        for (int i = 0; i < inputsToSend.Count; i++)
        {
            buffer.PutInt(inputsToSend[i]);  
        }
        buffer.PutInt(lastInputSent);
    }
    
    public static List<int> Deserialize(BitBuffer buffer)
    {
        List<int> inputsToExecute = new List<int>();
        int count = buffer.GetInt();
        while (count > 0)
        {
            int currentInput = buffer.GetInt();
            inputsToExecute.Add(currentInput);
            count -= 1;
        }
        return inputsToExecute;
    }
}