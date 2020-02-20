using System;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour {
	public CinemachineFreeLook cam;
	public PlayerInput playerInput;
	public float camXSensitivity;
	public float camYSensitivity;

	Vector2 lookInput;
	
	public void OnLook(InputAction.CallbackContext context) => lookInput = context.ReadValue<Vector2>();

	void Update() {
		cam.m_XAxis.Value += camXSensitivity * lookInput.x;
		cam.m_YAxis.Value -= camYSensitivity * lookInput.y;
	}
}