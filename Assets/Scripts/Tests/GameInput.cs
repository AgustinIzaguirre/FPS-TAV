using System;
using System.Collections.Generic;
using UnityEngine;

public class GameInput
{
    public int value;
    public float floatValue;
    public Vector3 playerOrientation;
    public InputValueType intputValueType;
    public static readonly int valueTypeQuantity = 2;
    private static readonly int minValue = 0;
    private static readonly int maxValue = 1 << 5;
    public static readonly int ROTATION_MASK = (15 << 9);
    public static readonly int ROTATION_OFFSET = 9;
    public static readonly int ROTATION_RANGE = 64;
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
        intputValueType = InputValueType.INTEGER_VALUE;
    }

    public GameInput(float horizontalRotation, Vector3 playerOrientation)
    {
        floatValue = horizontalRotation;
        this.playerOrientation = playerOrientation;
        intputValueType = InputValueType.FLOAT_VALUE;
    }

    public GameInput(int value)
    {
        this.value = value;
        intputValueType = InputValueType.INTEGER_VALUE;
    }

    public static void Serialize(List<GameInput> inputsToSend, int lastInputSent, BitBuffer buffer)
    {
        buffer.PutInt(inputsToSend.Count);
        for (int i = 0; i < inputsToSend.Count; i++)
        {
            if (inputsToSend[i].intputValueType == InputValueType.INTEGER_VALUE)
            {
                buffer.PutInt((int)inputsToSend[i].intputValueType, 0, valueTypeQuantity);
                buffer.PutInt(inputsToSend[i].value, minValue, maxValue);
            }
            else
            {
                buffer.PutInt((int)inputsToSend[i].intputValueType, 0, valueTypeQuantity);
                buffer.PutFloat(inputsToSend[i].floatValue);
                buffer.PutFloat(inputsToSend[i].playerOrientation.x);
                buffer.PutFloat(inputsToSend[i].playerOrientation.y);
                buffer.PutFloat(inputsToSend[i].playerOrientation.z);
            }
        }
        buffer.PutInt(lastInputSent);
    }
    
    public static List<GameInput> Deserialize(BitBuffer buffer)
    {
        List<GameInput> inputsToExecute = new List<GameInput>();
        int count = buffer.GetInt();
        while (count > 0)
        {
            int currentInputType = buffer.GetInt(0, valueTypeQuantity);
            GameInput currentInput = null;
            if (currentInputType == (int) InputValueType.INTEGER_VALUE)
            {
                currentInput = new GameInput(buffer.GetInt(minValue, maxValue));
            }
            else
            {
                float rotation = buffer.GetFloat();
                Vector3 orientation = new Vector3(buffer.GetFloat(), buffer.GetFloat(), buffer.GetFloat());
                currentInput = new GameInput(rotation, orientation);
            }
            inputsToExecute.Add(currentInput);
            count -= 1;
        }
        return inputsToExecute;
    }
}