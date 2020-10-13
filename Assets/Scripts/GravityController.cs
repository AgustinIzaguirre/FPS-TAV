using System;
using UnityEngine;

public class GravityController : MonoBehaviour
{
    [SerializeField]
    private float gravity = 9.81f;

    private float time = 0f;

    private CharacterController controller;

    private bool isJumping;
    private float verticalVelocity;
    private float startVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        isJumping = false;
        verticalVelocity = 0f;
    }

    private void FixedUpdate()
    {
        if (controller.isGrounded)
        {
            verticalVelocity = 0;
            isJumping = false;
        }
        else
        {
            time += Time.fixedDeltaTime;
            verticalVelocity = startVelocity - gravity * time;
        }
    }

    public void Jump(float verticalVelocity)
    {
        isJumping = true;
        time = 0;
        startVelocity = verticalVelocity;
        this.verticalVelocity = verticalVelocity;
        
    }

    public float GetVerticalVelocity()
    {
        return verticalVelocity;
    }
}
