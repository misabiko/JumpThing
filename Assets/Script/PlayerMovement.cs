using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour {
	public Transform camTransform;
	public Animator anim;
	public ParticleSystem smokeTrail;
	public ParticleSystem smokePoof;
	public float maxSpeedJog;
	public float maxSpeedRun;
	public float runThreshold;	//Must match animator's transition conditions
	public float jumpForce;
	public float gravityScale;
	public float turnSpeed;

	CharacterController characterController;
	Vector2 moveInput;
	Vector3 moveDirection;
	Vector3 verticalVel;
	bool wasGrounded;

	public TextMeshProUGUI velText;
	static readonly int MoveInputMagnitude = Animator.StringToHash("MoveInputMagnitude");
	static readonly int Jump = Animator.StringToHash("Jump");
	static readonly int AirBorn = Animator.StringToHash("AirBorn");

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
		if (!characterController.isGrounded) return;
		
		verticalVel.y = jumpForce;
		anim.SetTrigger(Jump);
		anim.SetBool(AirBorn, true);
	}

	void Update() {
		Vector3 camForward = camTransform.forward;
		camForward.y = 0;
		camForward.Normalize();
		
		moveDirection = camForward * moveInput.y + camTransform.right * moveInput.x;
		
		float stickStrength = moveDirection.magnitude;
		anim.SetFloat(MoveInputMagnitude, stickStrength);
		CheckSmokeParticles(stickStrength);
	}

	void CheckSmokeParticles(float stickStrength) {
		if (!characterController.isGrounded) {
			if (smokeTrail.isPlaying)	//TODO PROFILEME might not be necessary
				smokeTrail.Stop();
			return;
		}

		if (smokeTrail.isPlaying && stickStrength <= runThreshold)
			smokeTrail.Stop();
		else if (!smokeTrail.isPlaying && stickStrength > runThreshold)
			smokeTrail.Play();
	}

	void FixedUpdate() {
		verticalVel += Physics.gravity * gravityScale * Time.fixedDeltaTime;

		Vector3 velocity;
		if (moveDirection.sqrMagnitude <= runThreshold * runThreshold)
			velocity = moveDirection * maxSpeedJog;
		else {
			Vector3 direction = moveDirection.normalized;
			velocity = (moveDirection - direction * runThreshold) * maxSpeedRun + direction * runThreshold * maxSpeedJog;
		}
		velText.text = "Input: " + (Mathf.Round(moveDirection.magnitude * 100f) / 100f) + "\nVel: " + (Mathf.Round(velocity.magnitude * 100f) / 100f);
		velocity += verticalVel;
		
		characterController.Move(velocity * Time.fixedDeltaTime);
		
		if (moveDirection != Vector3.zero) {
			Quaternion goalRot = Quaternion.LookRotation(moveDirection);
			Quaternion slerp = Quaternion.Slerp(transform.rotation, goalRot, turnSpeed * moveDirection.magnitude * Time.fixedDeltaTime);
			
			transform.rotation = slerp;
		}

		UpdateGrounding(characterController.isGrounded);
	}

	void UpdateGrounding(bool isGrounded) {
		if (isGrounded)
			verticalVel = Vector3.zero;
		
		anim.SetBool(AirBorn, !isGrounded);

		if (isGrounded != wasGrounded)
			smokePoof.Play();
		
		wasGrounded = isGrounded;
	}

	public void Teleport(Vector3 pos, Quaternion rot) {
		characterController.enabled = false;
		verticalVel = Vector3.zero;
		transform.SetPositionAndRotation(pos, rot);
		characterController.enabled = true;
	}
}