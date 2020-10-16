using System.Collections.Generic;
using UnityEngine;

public class PlayerMotion
{
    private static float playerSpeed = 10f;
    private static float jumpSpeed = 1.5f;
    
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

//    public static void ApplyInputs(int startInput, List<int> inputsToExecute, CharacterController player)
//    {
//        for (int i = startInput; i < inputsToExecute.Count; i++)
//        {
//            Vector3 appliedForce = AnalyzeInput(inputsToExecute[i]);
//            player.Move(appliedForce * (playerSpeed * Time.fixedDeltaTime));
//        }
//    }
    
    public static void ApplyInputs(int startInput, List<int> inputsToExecute, CharacterController player,
        GravityController gravityController, Transform transform)
    {
        for (int i = startInput; i < inputsToExecute.Count; i++)
        {
            Vector3 appliedForce = AnalyzeInput(inputsToExecute[i], transform);
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
    }

//    public static void ApplyInput(int input, CharacterController player)
//    {
//        Vector3 appliedForce = AnalyzeInput(input);
//        player.Move(appliedForce * (playerSpeed * Time.fixedDeltaTime));
//    }
    
    public static void ApplyInput(int input, CharacterController player, GravityController gravityController,
        Transform transform)
    {
        Vector3 appliedForce = AnalyzeInput(input, transform);
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
}
