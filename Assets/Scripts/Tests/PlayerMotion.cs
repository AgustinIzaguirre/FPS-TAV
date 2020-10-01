using System.Collections.Generic;
using UnityEngine;


public class PlayerMotion
{
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

    public static void ApplyInputs(int startInput, List<int> inputsToExecute, Rigidbody player)
    {
        for (int i = startInput; i < inputsToExecute.Count; i++)
        {
            Vector3 appliedForce = AnalyzeInput(inputsToExecute[i]);
            player.AddForceAtPosition(appliedForce, Vector3.zero, ForceMode.Impulse);
        }
    }
    
    public static void ApplyInput(int input, Rigidbody player)
    {
        Vector3 appliedForce = AnalyzeInput(input);
        player.AddForceAtPosition(appliedForce, Vector3.zero, ForceMode.Impulse);
                
    }
}
