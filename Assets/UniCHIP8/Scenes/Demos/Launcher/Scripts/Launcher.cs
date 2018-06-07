using UnityEngine;
using System.Collections;

public class Launcher : MonoBehaviour {
	public GameObject rocket;
	private bool launched = false;
	Rigidbody rb;
	Rocket r;

	void Start() {
		r = rocket.GetComponent<Rocket> ();
		rb = rocket.GetComponent<Rigidbody> ();
	}

	void FixedUpdate() {
		if (launched && r.fuel > 0) {
			rb.AddForce(Vector3.up * (r.thrust * Time.deltaTime), ForceMode.Acceleration);
		}
	}

	public void LaunchRocket() {
		print ("Launching rocket...");
		r.Launch ();
		launched = true;
	}

}
