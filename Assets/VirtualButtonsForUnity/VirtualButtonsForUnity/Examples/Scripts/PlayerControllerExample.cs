using UnityEngine;

public class PlayerControllerExample : MonoBehaviour
{

    [SerializeField] private float playerSpeed = 2.0f;
    [SerializeField] private float jumpHeight = 1.0f;
    [SerializeField] private float gravityValue = -9.81f;

    protected CharacterController controller;
    protected PlayerActionsExample playerInput;
    private Vector3 playerVelocity;
    private bool groundedPlayer;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = new PlayerActionsExample();
    }

    private void Update()
    {
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        var movement = playerInput.Player.Move.ReadValue<Vector2>();
        var move = new Vector3(movement.x, 0, movement.y);
        _ = controller.Move(move * Time.deltaTime * playerSpeed);

        if (move != Vector3.zero)
        {
            gameObject.transform.forward = move;
        }

        // bool jumpPress = playerInput.Player.Jump.IsPressed();
        var jumpPress = playerInput.Player.Jump.triggered;
        if (jumpPress && groundedPlayer)
        {
            playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        _ = controller.Move(playerVelocity * Time.deltaTime);
    }

    private void OnEnable()
    {
        playerInput.Enable();
    }

    private void OnDisable()
    {
        playerInput.Disable();
    }
}
