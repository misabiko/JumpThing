using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayMarchingController : MonoBehaviour {
	public float smoother = 1.0f;
	public Vector3 colorA;
	public Vector3 colorB;
	public float stepDivider = 16f;
	public float stepOffset = 2f;
}