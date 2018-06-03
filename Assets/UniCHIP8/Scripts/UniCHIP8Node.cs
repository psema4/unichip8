using UnityEngine;
using System.Collections;

public class UniCHIP8Node : MonoBehaviour {
	[Header("Message Routing")]
	public UniCHIP8Router router;

	// Use this for initialization
	void Start () {
		if (router != null) {
			router.SendMessage("RegisterNode", gameObject);
		}
	}

	virtual public void Execute(string commandData) {
		// See UniCHIP8Router.cs for commandData format

		print (name + " received commandData: " + commandData);

		string[] parts = commandData.Split (new string[] { "~" }, System.StringSplitOptions.None);
		string args1;
		Vector3 args3;

		switch (parts [0]) {
		// transform commands
		case "move":
			args3 = new Vector3 (float.Parse (parts [1]), float.Parse (parts [2]), float.Parse (parts [3]));
			transform.position = args3;
			break;

		case "rotate":
			args3 = new Vector3 (float.Parse (parts [1]), float.Parse (parts [2]), float.Parse (parts [3]));
			transform.Rotate (args3);
			break;

		case "scale":
			args3 = new Vector3 (float.Parse (parts [1]), float.Parse (parts [2]), float.Parse (parts [3]));
			transform.localScale = args3;
			break;

		// other commands
		case "call":
			args1 = parts[1];
			gameObject.SendMessage(args1);
			break;

		default:
			print ("unrecognized command: " + commandData);
			break;
		}
	}

	virtual public void Receive(string data) {
		print (name + " received data: " + data);
	}
}
