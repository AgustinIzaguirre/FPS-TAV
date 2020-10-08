using System.Collections.Generic;
using UnityEngine;

public class PlayerMotion
{
    private static float playerSpeed = 10f;
    
    private static Vector3 AnalyzeInput(int inputs)
    {
        Vector3 appliedForce = Vector3.zero;
        ;
        if ((inputs & ((int) InputType.JUMP)) > 0)
        {
            appliedForce += Vector3.up;
        } 
        if ((inputs & ((int) InputType.LEFT)) > 0)
        {
            appliedForce += Vector3.left;
        }
        if ((inputs & ((int) InputType.RIGHT)) > 0)
        {
            appliedForce += Vector3.right;
        }
        if ((inputs & ((int) InputType.FORWARD)) > 0)
        {
            appliedForce += Vector3.forward;
        }
        if ((inputs & ((int) InputType.BACKWARD)) > 0)
        {
            appliedForce += Vector3.back;
        }

        return appliedForce;
    }

    public static void ApplyInputs(int startInput, List<int> inputsToExecute, CharacterController player)
    {
        for (int i = startInput; i < inputsToExecute.Count; i++)
        {
            Vector3 appliedForce = AnalyzeInput(inputsToExecute[i]);
            player.Move(appliedForce * (playerSpeed * Time.fixedDeltaTime));
        }
    }
    
    public static void ApplyInput(int input, CharacterController player)
    {
        Vector3 appliedForce = AnalyzeInput(input);
        player.Move(appliedForce * (playerSpeed * Time.fixedDeltaTime));
    }
}
