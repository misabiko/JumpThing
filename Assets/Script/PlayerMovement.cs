using System;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour {
	public CinemachineFreeLook cam;
	public Transform camTransform;
	public float camXSensitivity;
	public float camYSensitivity;

	public float maxSpeed;

	CharacterController characterController;
	Vector2 moveInput;
	Vector3 moveDirection;
	
	void Awake() {
		characterController = GetComponent<CharacterController>();

		PlayerInput playerInput = GetComponent<PlayerInput>();

		playerInput.actions["Look"].started += OnLook;
		playerInput.actions["Look"].performed += OnLook;
		playerInput.actions["Look"].canceled += OnLook;

		playerInput.actions["Move"].started += OnMove;
		playerInput.actions["Move"].performed += OnMove;
		playerInput.actions["Move"].canceled += OnMove;
	}

	void OnLook(InputAction.CallbackContext context) {
		Vector2 lookInput = context.ReadValue<Vector2>();
		cam.m_XAxis.Value += camXSensitivity * lookInput.x;
		cam.m_YAxis.Value += camYSensitivity * lookInput.y;
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
		if (moveInput != Vector2.zero) {
			moveDirection = camTransform.forward * moveInput.y + camTransform.right * moveInput.x;
			moveDirection.y = 0;
		}
	}

	void FixedUpdate() {
		characterController.Move(moveDirection * maxSpeed * Time.fixedDeltaTime);
	}
}