using UnityEngine;

namespace Script {
	[CreateAssetMenu(menuName = "PlayerData")]
	public class PlayerData : ScriptableObject {
		[Header("General")]
		public int scoreOffset = 50;
		public float deathHeight = -30f;
		
		[Header("Movement")]
		public float accelJog = 300f;
		public float accelRun = 350f;
		public float accelAirBorn = 10f;
		public float maxSpeedJog = 5f;
		public float maxSpeedRun = 20f;
		public float runThreshold = 0.95f;	//Must match animator's transition conditions
		public float jumpForce = 50f;
		public float turnSpeed = 10f;
		public float airBornAngle = 20f;
		public float groundCheckDist = 0.1f;
	
		public float warmUpDelay = 1.5f;
		public float warmUpSpeed = 0.001f;
		public float maxWarmUp = 0.5f;
		
		public float crouchThreshold = 0.2f;
	}
}