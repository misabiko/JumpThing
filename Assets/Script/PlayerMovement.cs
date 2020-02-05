using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour {
	public Transform camTransform;
	public float maxSpeed;
	public float jumpForce;
	public float gravityScale;

	CharacterController characterController;
	Vector2 moveInput;
	Vector3 moveDirection;
	Vector3 verticalVel;
	
	void Awake() {
		characterController = GetComponent<CharacterController>();

		PlayerInput playerInput = GetComponent<PlayerInput>();

		playerInput.actions["Move"].started += OnMove;
		playerInput.actions["Move"].performed += OnMove;
		playerInput.actions["Move"].canceled += OnMove;

		playerInput.actions["Jump"].started += OnJump;
		playerInput.actions["Jump"].performed += OnJump;
		playerInput.actions["Jump"].canceled += OnJump;
	}

	void OnMove(InputAction.CallbackContext context) {
		moveInput = context.ReadValue<Vector2>();
		if (moveInput.sqrMagnitude > 1f)
			moveInput.Normalize();
	}

	void OnJump(InputAction.CallbackContext context) {
		if (characterController.isGrounded)
			verticalVel.y = jumpForce;
	}

	void Update() {
		Vector3 camForward = camTransform.forward;
		camForward.y = 0;
		camForward.Normalize();
		
		moveDirection = camForward * moveInput.y + camTransform.right * moveInput.x;
	}

	void FixedUpdate() {
		verticalVel += Physics.gravity * gravityScale * Time.fixedDeltaTime;
		Vector3 velocity = moveDirection * maxSpeed + verticalVel;

		characterController.Move(velocity * Time.fixedDeltaTime);
		
		
		if (characterController.isGrounded)
			verticalVel = Vector3.zero;
	}
}