using UnityEngine;
using System.Collections;

public class Launcher : MonoBehaviour {
	public GameObject rocket;
	Rocket r;

	void Start() {
		r = rocket.GetComponent<Rocket> ();
	}

	public void LaunchRocket() {
		print ("Launching rocket...");
		r.Launch ();
	}

}
