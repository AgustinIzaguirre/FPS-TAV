using System.Collections.Generic;
using UnityEngine;

public class PlayerMotion
{
    private static float playerSpeed = 2f;
    
    private static Vector3 AnalyzeInput(int inputs)
    {
        Vector3 appliedForce = Vector3.zero;
        ;
        if ((inputs & 1) > 0)
        {
            appliedForce += Vector3.up * 5;
        } 
        if ((inputs & (1 << 1)) > 0)
        {
            appliedForce += Vector3.left * 5;
        }
        if ((inputs & (1 << 2)) > 0)
        {
            appliedForce += Vector3.right * 5;
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
