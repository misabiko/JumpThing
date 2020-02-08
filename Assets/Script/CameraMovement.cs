using System;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour {
	public CinemachineFreeLook cam;
	public PlayerInput playerInput;
	public CharacterController characterController;
	
	public float camXSensitivity;
	public float camYSensitivity;

	public float upThreshold = 0.1f;

	Vector2 lookInput;
	bool lastGoingUp = false;

	public TextMeshProUGUI textVelX;
	public TextMeshProUGUI textVelY;
	public TextMeshProUGUI textVelZ;

	void Start() {
		playerInput.actions["Look"].started += OnLook;
		playerInput.actions["Look"].performed += OnLook;
		playerInput.actions["Look"].canceled += OnLook;
	}

	void OnLook(InputAction.CallbackContext context) => lookInput = context.ReadValue<Vector2>();

	void Update() {
		cam.m_XAxis.Value += camXSensitivity * lookInput.x;
		cam.m_YAxis.Value -= camYSensitivity * lookInput.y;
	}

	void LateUpdate() {
		textVelX.text = "VelX: " + characterController.velocity.x;
		textVelY.text = "VelY: " + characterController.velocity.y;
		textVelZ.text = "VelZ: " + characterController.velocity.z;
		
		bool goingUp = characterController.velocity.y > upThreshold;

		if (goingUp == lastGoingUp) return;
		lastGoingUp = !lastGoingUp;
		
		float damping = goingUp ? 0.1f : 1f;

		for (int i = 0; i < 3; i++) {
			CinemachineTransposer transposer = cam.GetRig(i).GetCinemachineComponent<CinemachineTransposer>();
			transposer.m_XDamping = damping;
			transposer.m_YDamping = damping;
			transposer.m_ZDamping = damping;
		}
	}
}