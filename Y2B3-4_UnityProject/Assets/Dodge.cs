using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyCharacterMovement;

public class Dodge : MonoBehaviour
{
    [SerializeField] private float dashDistance = 2f;
    [SerializeField] private float dashTime = 0.5f;
    [SerializeField] private KeyCode dashKey = KeyCode.Space;
    [SerializeField] private float dashSpeed = 20f;

    private Character character;
    private Rigidbody rb;
    private bool dashing = false;

    void Start()
    {
        character = GetComponent<Character>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!dashing && Input.GetKeyDown(dashKey))
        {
            StartCoroutine(Dash());
        }
    }

    IEnumerator Dash()
    {
        // Get the direction the player is moving
        Vector3 direction = character.GetMovementDirection();

        // Set dashing to true to prevent multiple dashes
        dashing = true;

        // Disable player movement
        character.enabled = false;

        // Calculate the velocity required to move the player the specified distance in the given time
        Vector3 velocity = direction * (dashDistance / dashTime);

        // Set the player's velocity to the calculated velocity
        rb.velocity = velocity;

        // Wait for the dash to complete
        yield return new WaitForSeconds(dashTime);

        // Reset player velocity and enable movement
        rb.velocity = Vector3.zero;
        character.enabled = true;

        // Set dashing to false
        dashing = false;
    }
}