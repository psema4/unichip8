using UnityEngine;
using System.Collections;

public class Rocket : MonoBehaviour {
	public int fuel = 500;
	private int startingFuel;
	[HideInInspector]
	public float thrust;
	private bool inFlight = false;
	private Rigidbody rb;

	void Start() {
		rb = GetComponent<Rigidbody> ();
		startingFuel = fuel;
	}

	void FixedUpdate () {
		if (inFlight) {
			thrust = fuel / 10f;
			rb.AddForce (Vector3.up * (thrust * Time.deltaTime), ForceMode.Acceleration);

			if (fuel > 0) {
				fuel -= 1;

			} else {
				inFlight = false;
			}

		} else {
			if (transform.position.y > 50) {
				rb.useGravity = true;
				rb.mass = 0.01f;
				
			} else if (transform.position.y < 25) {
				rb.useGravity = false;
				fuel = startingFuel;
			}

		}
	}

	public void Launch() {
		inFlight = true;
	}
}
