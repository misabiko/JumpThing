using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour {
	public Transform camTransform;
	public float maxSpeed;

	CharacterController characterController;
	Vector2 moveInput;
	Vector3 moveDirection;
	
	void Awake() {
		characterController = GetComponent<CharacterController>();

		PlayerInput playerInput = GetComponent<PlayerInput>();

		playerInput.actions["Move"].started += OnMove;
		playerInput.actions["Move"].performed += OnMove;
		playerInput.actions["Move"].canceled += OnMove;
	}

	void OnMove(InputAction.CallbackContext context) {
		moveInput = context.ReadValue<Vector2>();
		if (moveInput.sqrMagnitude > 1f)
			moveInput.Normalize();
	}

	void OnJump(InputAction.CallbackContext context) {
		Debug.Log("Jump!");
	}

	void Update() {
		if (moveInput == Vector2.zero) return;
		
		moveDirection = camTransform.forward * moveInput.y + camTransform.right * moveInput.x;
		moveDirection.y = 0;
	}

	void FixedUpdate() {
		characterController.Move(moveDirection * maxSpeed * Time.fixedDeltaTime);
	}
}