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
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);


        Collider[] allHits = Physics.OverlapSphere(groundCheck.position, groundDistance);
        string hitReport = allHits.Length == 0 ? "NOTHING" : "";
        foreach (var c in allHits)
        {
            hitReport += $"[{c.name} on layer {c.gameObject.layer} ({LayerMask.LayerToName(c.gameObject.layer)})] ";
        }

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        var x = Input.GetAxis("Horizontal");
        var z = Input.GetAxis("Vertical");

        if (dashCooldownRemaining > 0f)
        {
            dashCooldownRemaining -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.T) && !isDashing && dashCooldownRemaining <= 0f)
        {
            var inputDir = new Vector3(x, 0, z);
            if (inputDir.sqrMagnitude > 0.01f)
            {
                dashDirection = transform.TransformDirection(inputDir.normalized);
            }
            else
            {
                dashDirection = transform.forward;
            }
            isDashing = true;
            dashTimeRemaining = dashDuration;
            dashCooldownRemaining = dashCooldown;
        }

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
            var move = transform.TransformDirection(new Vector3(x, 0, z));
            controller.Move(move * speed * Time.deltaTime);
        }

        if (Input.GetButtonDown("Jump") && isGrounded && !isDashing)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
