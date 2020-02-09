using System;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour {
	[SerializeField] CinemachineFreeLook cam;
	[SerializeField] PlayerInput playerInput;
	[SerializeField] float camXSensitivity;
	[SerializeField] float camYSensitivity;

	Vector2 lookInput;

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
}