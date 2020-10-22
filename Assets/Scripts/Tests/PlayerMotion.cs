using System.Collections.Generic;
using UnityEngine;

public class PlayerMotion
{
    private static float playerSpeed = 2.5f;
    private static float jumpSpeed = 1.5f;
    private static readonly float MOUSE_SENSITIVITY = 100f;


    private static Vector3 AnalyzeInput(int inputs, Transform transform)
    {
        Vector3 appliedForce = Vector3.zero;
        ;
        if ((inputs & ((int) InputType.JUMP)) > 0)
        {
            appliedForce += transform.up * jumpSpeed;
        } 
        if ((inputs & ((int) InputType.LEFT)) > 0)
        {
            appliedForce += -transform.right;
        }
        if ((inputs & ((int) InputType.RIGHT)) > 0)
        {
            appliedForce += transform.right;
        }
        if ((inputs & ((int) InputType.FORWARD)) > 0)
        {
            appliedForce += transform.forward;
        }
        if ((inputs & ((int) InputType.BACKWARD)) > 0)
        {
            appliedForce += -transform.forward;
        }

        return appliedForce;
    }

    public static void ApplyInputs(int startInput, List<GameInput> inputsToExecute, CharacterController player,
        GravityController gravityController, Transform transform)
    {
        for (int i = startInput; i < inputsToExecute.Count; i++)
        {
            if (inputsToExecute[i].intputValueType == InputValueType.INTEGER_VALUE)
            {
                Vector3 appliedForce = AnalyzeInput(inputsToExecute[i].value, transform);
                if (appliedForce.y > 0)
                {
                    gravityController.Jump(appliedForce.y);
                }
                else
                {
                    appliedForce.y = gravityController.GetVerticalVelocity();
                }

                player.Move(appliedForce * (playerSpeed * Time.fixedDeltaTime));
            }
            else if (inputsToExecute[i].intputValueType == InputValueType.FLOAT_VALUE)
            {
                float rotationValue = inputsToExecute[i].floatValue;
                Vector3 playerOrientation = inputsToExecute[i].playerOrientation;
                transform.eulerAngles = playerOrientation;
                transform.Rotate(Vector3.up * (rotationValue * MOUSE_SENSITIVITY * Time.fixedDeltaTime));
            }
        }
    }

    public static void ApplyInput(GameInput input, CharacterController player, GravityController gravityController,
        Transform transform)
    {
        if (input.intputValueType == InputValueType.INTEGER_VALUE)
        {
            Vector3 appliedForce = AnalyzeInput(input.value, transform);
            if (appliedForce.y > 0)
            {
                gravityController.Jump(appliedForce.y);
            }
            else
            {
                appliedForce.y = gravityController.GetVerticalVelocity();
            }

            player.Move(appliedForce * (playerSpeed * Time.fixedDeltaTime));
        }
        else if (input.intputValueType == InputValueType.FLOAT_VALUE)
        {
            float rotationValue = input.floatValue;
            Vector3 playerOrientation = input.playerOrientation;
            transform.eulerAngles = playerOrientation;
            transform.Rotate(Vector3.up * (rotationValue * MOUSE_SENSITIVITY * Time.fixedDeltaTime));
        }
    }
}
