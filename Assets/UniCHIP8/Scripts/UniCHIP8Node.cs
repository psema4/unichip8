using UnityEngine;
using System.Collections;

public class UniCHIP8Node : MonoBehaviour {
	[Header("Extensions Network")]
	[Tooltip("The router managing this GameObjects' UniCHIP8 Extensions network.")]
	public UniCHIP8Router router;

	[Tooltip("Should this GameObject be destroyed when the router receives a reset command?")]
	public bool destroyOnReset = true;

	// Use this for initialization
	void Start () {
		if (router != null)
			router.SendMessage("RegisterNode", gameObject);
	}

	virtual public void Execute(string commandData) {
		// See UniCHIP8Router.cs for commandData format

		//print ("\"" + name + "\" received commandData: " + commandData);

		// arguments processing
		string[] parts = commandData.Split (new string[] { "~" }, System.StringSplitOptions.None);
		string   args1s;
		int      args1i;
		float    args1f;
		Vector3  args3f;

		// helpers
		Vector3 tmpVec3;
		Renderer renderer = gameObject.GetComponent<Renderer>();
		float r;
		float g;
		float b;
		float a;
		Color color;

		// execute
		switch (parts [0]) {
		// transform commands
		case "move":
			args3f = new Vector3 (float.Parse (parts [1]), float.Parse (parts [2]), float.Parse (parts [3]));
			transform.position = args3f;
			break;

		case "moveX":
			args1f = float.Parse(parts[1]);
			tmpVec3 = new Vector3(args1f, transform.position.y, transform.position.z);
			transform.position = tmpVec3;
			break;
			
		case "moveY":
			args1f = float.Parse(parts[1]);
			tmpVec3 = new Vector3(transform.position.x, args1f, transform.position.z);
			transform.position = tmpVec3;
			break;

		case "moveZ":
			args1f = float.Parse(parts[1]);
			tmpVec3 = new Vector3(transform.position.x, transform.position.y, args1f);
			transform.position = tmpVec3;
			break;

		case "rotate":
			args3f = new Vector3 (float.Parse (parts [1]), float.Parse (parts [2]), float.Parse (parts [3]));
			transform.Rotate (args3f);
			break;

		case "rotateX":
			args1f = float.Parse (parts[1]);
			tmpVec3 = new Vector3(args1f, transform.localEulerAngles.y, transform.localEulerAngles.z);
			transform.Rotate (tmpVec3);
			break;

		case "rotateY":
			args1f = float.Parse (parts[1]);
			tmpVec3 = new Vector3(transform.localEulerAngles.x, args1f, transform.localEulerAngles.z);
			transform.Rotate (tmpVec3);
			break;

		case "rotateZ":
			args1f = float.Parse (parts[1]);
			tmpVec3 = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, args1f);
			transform.Rotate (tmpVec3);
			break;

		case "scale":
			args3f = new Vector3 (float.Parse (parts [1]), float.Parse (parts [2]), float.Parse (parts [3]));
			transform.localScale = args3f;
			break;

		case "scaleX":
			args1f = float.Parse (parts[1]);
			tmpVec3 = new Vector3(args1f, transform.localScale.y, transform.localScale.z);
			transform.localScale = tmpVec3;
			break;
			
		case "scaleY":
			args1f = float.Parse (parts[1]);
			tmpVec3 = new Vector3(transform.localScale.x, args1f, transform.localScale.z);
			transform.localScale = tmpVec3;
			break;
			
		case "scaleZ":
			args1f = float.Parse (parts[1]);
			tmpVec3 = new Vector3(transform.localScale.x, transform.localScale.y, args1f);
			transform.localScale = tmpVec3;
			break;

		// other commands
		case "call":
			args1s = parts[1];
			gameObject.SendMessage(args1s);
			break;

		case "broadcast":
			args1s = parts[1];
			gameObject.BroadcastMessage(args1s);
			break;

		case "reparent":
			args1s = parts[1];
			GameObject parentObject = GameObject.Find(args1s);
			
			if (parentObject != null)
				gameObject.transform.SetParent(parentObject.transform);

			break;

		case "addMaterial":
			renderer.material = new Material(Shader.Find ("Standard"));
			break;

		case "setMaterialColor":
			r = float.Parse(parts[1]) / 255;
			g = float.Parse(parts[2]) / 255;
			b = float.Parse(parts[3]) / 255;
			a = float.Parse(parts[4]);

			if (a < 1)
				a = 1;
			
			if (a > 100)
				a = 100;
			
			a /= 100;

			print ("Setting color: " + r + ", " + g + ", " + b + ", " + a);
			color = new Color(r, g, b, a);
			print ("Set color: " + color.ToString());
			renderer.material.color = color;
			break;

		case "lookAt":
			args1s = parts[1];
			GameObject targetObject = GameObject.Find(args1s);

			if (targetObject != null) {
				transform.LookAt(targetObject.transform);
			}
			break;

		case "setLightColor":
			r = float.Parse(parts[1]) / 255;
			g = float.Parse(parts[2]) / 255;
			b = float.Parse(parts[3]) / 255;
			a = float.Parse(parts[4]);

			if (a < 1)
				a = 1;

			if (a > 100)
				a = 100;

			a /= 100;

			color = new Color(r, g, b, a);
			gameObject.GetComponent<Light>().color = color;
			break;

		case "setLightIntensity": // 0-100 range
			args1i = int.Parse (parts[1]);
			args1f = (float) args1i / 100;
			gameObject.GetComponent<Light>().intensity = args1f;
			break;



		case "unregister":
			router.SendMessage("UnregisterNode", gameObject);
			break;

		case "destroy":
			router.SendMessage("DestroyNode", gameObject);
			break;

		default:
			print ("unrecognized command: " + commandData);
			break;
		}
	}

	virtual public void Receive(string data) {
		print ("\"" + name + "\" received data: " + data);
	}
}
