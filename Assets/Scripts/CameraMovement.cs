using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] 
    private Transform cameraTransform;
    
    private float minX;

    private float maxX;

    private float minZ;

    private float maxZ;

    private System.Random random;


    private Vector3 goalPosition;
    private Vector3 goalRotation;
    public float cameraSpeed = 5f;

    void Start()
    {
        
        random = new System.Random();
        minX = -9.5f;
        maxX = 15.8f;
        minZ = -27.3f;
        maxZ = 33.1f;
        goalPosition = GenerateGoal();
    }

    void Update()
    {

        if ((goalPosition - cameraTransform.position).magnitude < 2f)
        {
            goalPosition = GenerateGoal();
            AlignCameraRotation();
        }
        Vector3 direction = (goalPosition - cameraTransform.position).normalized;
        Vector3 newPosition = cameraTransform.position + direction * cameraSpeed * Time.deltaTime;
        cameraTransform.position = newPosition;

    }

    private void AlignCameraRotation()
    {
        float degrees = Vector3.Angle(cameraTransform.position, goalPosition);
        cameraTransform.Rotate(Vector3.up, degrees);
    }

    private Vector3 GenerateGoal()
    {
        float xGoal =  (float)random.NextDouble() * (maxX - minX) + minX;
        float zGoal =  (float)random.NextDouble() * (maxZ - minZ) + minZ;
        return new Vector3(xGoal, cameraTransform.position.y, zGoal);
    }
}
