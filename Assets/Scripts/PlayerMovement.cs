using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float jumpHeight = 1.5f;
    public float gravity = -25f;
    public CharacterController controller;
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;
    public Transform playerBody;

    [Header("Dash")]
    public float dashSpeed = 20f;        
    public float dashDuration = 0.2f;   
    public float dashCooldown = 1.0f;   
    bool isDashing;

    Vector3 velocity;
    bool isGrounded;

    // Dash state
    
    float dashTimeRemaining;
    float dashCooldownRemaining;
    Vector3 dashDirection;
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
    void Update()
    {
        // Check if player is grounded
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);


        Collider[] allHits = Physics.OverlapSphere(groundCheck.position, groundDistance);
        string hitReport = allHits.Length == 0 ? "NOTHING" : "";
        foreach (var c in allHits)
        {
            hitReport += $"[{c.name} on layer {c.gameObject.layer} ({LayerMask.LayerToName(c.gameObject.layer)})] ";
        }

        //ensures the velocity doesnt accumulate to negative infinity
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Get input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Tick down dash cooldown so the player can dash again eventually
        if (dashCooldownRemaining > 0f)
        {
            dashCooldownRemaining -= Time.deltaTime;
        }

        // Start a dash on T-press: lock direction once, then ignore further input until it ends
        if (Input.GetKeyDown(KeyCode.T) && !isDashing && dashCooldownRemaining <= 0f)
        {
            Vector3 inputDir = new Vector3(x, 0, z);
            if (inputDir.sqrMagnitude > 0.01f)
            {
                // Dash in the direction the player is currently inputting (relative to facing)
                dashDirection = transform.TransformDirection(inputDir.normalized);
            }
            else
            {
                // No WASD held : dash forward
                dashDirection = transform.forward;
            }
            isDashing = true;
            dashTimeRemaining = dashDuration;
            dashCooldownRemaining = dashCooldown;
        }

        // Horizontal movement: dash overrides normal WASD movement while active
        if (isDashing)
        {
            controller.Move(dashDirection * dashSpeed * Time.deltaTime);
            dashTimeRemaining -= Time.deltaTime;
            if (dashTimeRemaining <= 0f)
            {
                isDashing = false;
            }
        }
        else
        {
            Vector3 move = transform.TransformDirection(new Vector3(x, 0, z));
            controller.Move(move * speed * Time.deltaTime);
        }

        // Jumping (disabled mid-dash to keep dash purely horizontal & predictable)
        if (Input.GetButtonDown("Jump") && isGrounded && !isDashing)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity (still applies during dash, so you can't dash off cliffs and float)
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
