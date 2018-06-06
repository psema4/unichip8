using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// A CHIP-8 Implementation for Unity 3D

public class UniCHIP8 : UniCHIP8Node {
	[Header("Machine State")]
	[Tooltip("Enable or disable the UniCHIP8.")]
	public bool powerState;

	[Tooltip("The number of iterations (clock ticks) per FixedUpdate.")]
	public int clockMultiplier = 1;

	[Tooltip("Enable or disable strict CHIP-8 compatibility. This must be disabled to use UniCHIP8 Extensions.")]
	public bool compatibilityMode = true;

	[Tooltip("Log register state on every clock tick.")]
	public bool logging = false;

	private int tickCount;
	private bool waitingForKeypress;
	private int waitingRegister;
	private bool[] Keys = new bool[] {
		false, false, false, false,
		false, false, false, false,
		false, false, false, false,
		false, false, false, false
	};
	
	[Header("Registers")]
	[Tooltip("General Registers")]
	public byte[] V;

	[Tooltip("Address Register")]
	public ushort I;

	[Tooltip("Program Counter")]
	public ushort PC;

	[Tooltip("Delay Timer")]
	public byte DT;

	[Tooltip("Sound Timer")]
	public byte ST;

	[Tooltip("Stack Pointer")]
	public byte SP;

	[Tooltip("Call Stack")]
	public ushort[] Stack;

	[Header("Virtual Hardware")]
	[Tooltip("Enable or disable keyboard input.")]
	public bool hasKeyboard = true;

	[ConditionalHide("hasKeyboard", true)]
	[Tooltip("Enable keyboard input when a GameObject (ie. the player) is near.")]
	public bool proximityKeyboard = false;

	[ConditionalHide("proximityKeyboard", true)]
	[Tooltip("The GameObject to test for proximity.")]
	public GameObject proximityTarget;

	[ConditionalHide("proximityKeyboard", true)]
	[Tooltip("The distance between the UniCHIP8 and the target GameObject that is considered to be near.")]
	public float proximityDistance = 3f;
	[Space(10)]
	
	[Tooltip("Enable or disable the screen.")]
	public bool hasScreen = true;
	private Texture2D screenTexture;

	[ConditionalHide("hasScreen", true)]
	[Tooltip("The screen model in your scene, having a UniCHIP8Screen material.")]
	public GameObject screenObject;

	[ConditionalHide("hasScreen", true)]
	[Tooltip("Select the screen's background color.")]
	public Color backgroundColor = Color.black;

	[ConditionalHide("hasScreen", true)]
	[Tooltip("Select the screen's foreground color.")]
	public Color foregroundColor = Color.green;
	[Space(10)]

	[Tooltip("Enable or disable the speaker.")]
	public bool hasSpeaker = true;
	private AudioSource audioSource;

	[ConditionalHide("hasSpeaker", true)]
	[Tooltip("The sound played on system beep.")]
	public AudioClip speakerSound;
	[Space(10)]
	
	[Tooltip("Enable or disable the data port.")]
	public bool hasDataPort = true;

	[ConditionalHide("hasDataPort", true)]
	[Tooltip("The address in RAM at which data will be used for I/O.")]
	public ushort dataPortAddress = 0xD50;

	[ConditionalHide("hasDataPort", true)]
	[Tooltip("The number of bytes available for I/O data.")]
	public byte dataPortSize = 32;

	[Header("ROM")]
	[Tooltip("The location in your project where ROM files can be found.")]
	public string romFolder = "Assets/UniCHIP8/Roms";

	[Tooltip("The filename (including extension) of the ROM file to boot when the UniCHIP8 is powered on.")]
	public string romFilename = "";
	
	[Header("RAM")]
	[Tooltip("The Program Counter is initialized to begin executing at this address when powered on.")]
	public ushort bootAddress = 0x200;

	[Tooltip("The location of the CHIP-8 font data.")]
	public ushort fontDataStart = 0xD7F;

	[Tooltip("View RAM state at runtime by pausing the Unity Editor and expanding this array.")]
	public byte[] ram;

	[Tooltip("View Video RAM state at runtime by pausing the Unity Editor and expanding this array.")]
	public byte[] vram;

	[Header("Scene Integration")]
	[Tooltip("These prefabs are made available to the UniCHIP8 Extension opcode, \"create\".")]
	public GameObject[] prefabs;

	void Start () {
		GetComponent<UniCHIP8Node>().destroyOnReset = false;

		screenTexture = new Texture2D (64, 32);
		screenObject.GetComponent<Renderer> ().material.mainTexture = screenTexture;
		audioSource = GetComponent<AudioSource> ();

		if (router != null) {
			router.BroadcastMessage("RegisterNode", gameObject);
		}

		Reset();
	}

	void Reset() {
		PC = (ushort) bootAddress;
		SP = 0;
		I  = 0;
		PC = 0x0200;
		DT = 0;
		ST = 0;
		
		V = new byte[16];
		Stack = new ushort[16];
		
		for (int i=0; i<16; i++) {
			V[i] = 0;
			Stack[i] = 0;
		}
		
		ram = new byte[4096];
		ClearRAM ();
		
		vram = new byte[64*32]; // FIXME: DXYN/SetPixel() USES ONLY A SINGLE BIT PER BYTE, LOTS OF WASTED STORAGE (ADD COLOUR/HI-RES?)
		ClearScreen ();
		
		if (romFilename == "")
			LoadROM ();
		
		else
			LoadROMFromFile (romFolder + "/" + romFilename);
		
		waitingForKeypress = false;
	}
	
	void Beep() {
		if (ST < 3)
			ST = 3;

		if (! hasSpeaker || speakerSound == null)
			if (logging)
				print ("BEEP");

		else if (hasSpeaker && speakerSound != null)
			audioSource.PlayOneShot (speakerSound, 1F);
	}
	
	void ClearScreen() {
		for (int i=0; i<2048; i++)
			vram[i] = 0;
		
		RenderScreen ();
	}
	
	void RenderScreen() {
		if (hasScreen) {
			int i = 0;
			int j = 64 * 31; // Textures are bottom-to-top not top-to-bottom; skip to last row
			Color[] screenData = new Color[2048];
			
			for (int y=0; y<32; y++) {
				for (int x = 0; x < 64; x++) {
					byte srcByte = vram [i++];
					screenData [j++] = srcByte > 0 ? foregroundColor : backgroundColor;
				}
				
				j -= 128; // go to start of previous row (64 bytes-per-row)
			}
			screenTexture.SetPixels (screenData);
			screenTexture.Apply ();
		}
	}
	
	bool SetPixel(int x, int y) {
		int j = 0;
		int width = 64;
		int height = 32;
		int originalX = x;
		int originalY = y;
		
		if (x > width)
			x -= width;

		else if (x < 0)
			x += width;
		
		if (y > height)
			y -= height;

		else if (y < 0)
			y += height;
		
		j = x + (y * width) - 1;
		
		if (j < 0 || j >= vram.Length) {
			print ("SetPixel(" + originalX +"," + originalY + "): Address " + j + " is out of range, NOT setting X=" + x + ", Y=" + y);
			return false;
			
		} else {
			vram [j] ^= 1;
			return vram [j] == 0;
		}
	}
	
	void ClearRAM() {
		for (int i=0; i<ram.Length; i++)
			ram[i] = 0;
		
		int[] fontData = new int[] {
			0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
			0x20, 0x60, 0x20, 0x20, 0x70, // 1
			0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
			0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
			0x90, 0x90, 0xF0, 0x10, 0x10, // 4
			0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
			0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
			0xF0, 0x10, 0x20, 0x40, 0x40, // 7
			0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
			0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
			0xF0, 0x90, 0xF0, 0x90, 0x90, // A
			0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
			0xF0, 0x80, 0x80, 0x80, 0xF0, // C
			0xE0, 0x90, 0x90, 0x90, 0xE0, // D
			0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
			0xF0, 0x80, 0xF0, 0x80, 0x80  // F
		};
		
		for (int i=0; i < fontData.Length; i++)
			ram[fontDataStart + i] = (byte) fontData[i];
	}
	
	void LoadROM() {
		WriteASCIIString (0x010, "Test Cube");
		WriteASCIIString (0x01A, "Test 2");
		WriteASCIIString (0x021, "Blue CPU");

		// Draws a BCD number (042) to the top-left of the texture
		byte[] programData = new byte[] {
			0x65, 0x03, // 6XNN -> SET V5 to 3
			0xF5, 0x18, // FX18 -> BEEP			
			0x00, 0xE0, // 00E0 -> CLEAR screen
			
			0xA0, 0x00, // ANNN -> SET I to Address 000
			0x60, 0x2A, // 6XNN -> SET V0 to 42
			0xF0, 0x33, // FX33 -> STORE V0 (42) AS BCD AT I (000)
			
			0xF5, 0x65, // FX65 -> LOAD V5 (3) Registers V0-V2 from memory starting at I (Load the BCD values)
			
			0x63, 0x01, // 6XNN -> SET V3 to 1
			0x64, 0x01, // 6XNN -> SET V4 to 1
			0xF0, 0x29, // FX29 -> SET I to address of char in V0 (Hundreds Digit)
			0xD3, 0x45, // DXYN -> DRAW char at V3,V4
			
			0x73, 0x05, // 7XNN -> ADD 5 to V3
			0xF1, 0x29, // FX29 -> SET I to address of char in V1 (Tens Digit)
			0xD3, 0x45, // DXYN -> DRAW char at V3,V4
			
			0x73, 0x05, // 7XNN -> ADD 5 to V3
			0xF2, 0x29, // FX29 -> SET I to address of char in V2 (Ones Digit)
			0xD3, 0x45, // DXYN -> DRAW char at V3,V4

			// UniCHIP8 Extensions
			0x0E, 0x00, // 0E00 -> extensions test

			// transformation tests
/*
			// create target object from a prefab
			//0x60, 0x00, // 6XNN -> SET V0 to 0
			//0xA0, 0x10, // ANNN -> SET I to address 0x010: starting address for target GameObject name, "Test Cube"
			//0x0E, 0xA0, // 0EA0 -> instantiate "Test Cube" from the prefab in prefabs[0]

			// or, create target object from primitive
			0xA0, 0x10, // ANNN -> SET I to address 0x010
			0x0E, 0xB0, // 0EB0 -> createCube "Test Cube"

			//0xA0, 0x10, // ANNN -> SET I to address 0x010
			//0x0E, 0xB1, // 0EB1 -> createSphere "Test Cube"

			//0xA0, 0x10, // ANNN -> SET I to address 0x010
			//0x0E, 0xB2, // 0EB2 -> createCylinder "Test Cube"

			//0xA0, 0x10, // ANNN -> SET I to address 0x010
			//0x0E, 0xB3, // 0EB3 -> createCapsule "Test Cube"

			//0xA0, 0x10, // ANNN -> SET I to address 0x010
			//0x0E, 0xB4, // 0EB4 -> createPlane "Test Cube"

			//0xA0, 0x10, // ANNN -> SET I to address 0x010
			//0x0E, 0xB5, // 0EB5 -> createQuad "Test Cube"

			// move target object
			0xA0, 0x10,	// ANNN -> SET I to address 0x010
			0x60, 0x03, // 6XNN -> Set V0 to 3
			0x0E, 0x10, // 0E1N -> moveX "Test Cube" to V[0] (3)

			0xA0, 0x10,	// ANNN -> SET I to address 0x010
			0x60, 0x01, // 6XNN -> Set V0 to 1
			0x0E, 0x20, // 0E2N -> moveY "Test Cube" to V[0] (1)

			0xA0, 0x10,	// ANNN -> SET I to address 0x010
			0x60, 0x02, // 6XNN -> Set V0 to 2
			0x0E, 0x30, // 0E3N -> moveZ "Test Cube" to V[0] (2)

			// rotate target object
			0x60, 0x2D, // 6XNN -> Set V0 TO 45
			0xA0, 0x10,	// ANNN -> SET I to address 0x010
			0x0E, 0x40, // 0E4N -> rotateX "Test Cube" to V[0] (45)

			0xA0, 0x10,	// ANNN -> SET I to address 0x010
			0x0E, 0x50, // 0E5N -> rotateY "Test Cube" to V[0] (45)

			0xA0, 0x10,	// ANNN -> SET I to address 0x010
			0x0E, 0x60, // 0E6N -> rotateZ "Test Cube" to V[0] (45)

			// scale target object
			0x60, 0x02, // 6XNN -> Set V0 TO 2
			0xA0, 0x10,	// ANNN -> SET I to address 0x010
			0x0E, 0x70, // 0E7N -> scaleX "Test Cube" to V[0] (2)

			0xA0, 0x10,	// ANNN -> SET I to address 0x010
			0x0E, 0x80, // 0E8N -> scaleY "Test Cube" to V[0] (2)

			0xA0, 0x10,	// ANNN -> SET I to address 0x010
			0x0E, 0x90, // 0E9N -> scaleZ "Test Cube" to V[0] (2)

			// destroy target object
			//0xA0, 0x10,	// ANNN -> SET I to address 0x010
			//0x0E, 0xBF, // 0xBF -> destroy "Test Cube"


			// create a different object
			0xA0, 0x1A, // ANNN -> SET I to address 0x01A: starting address for target GameObject name, "Test 2"
			0x0E, 0xB2, // 0EB2 -> createCylinder "Test 2"

			0x60, 0x03, // 6XNN -> Set V0 to 3
			0x61, 0x02, // 6XNN -> Set V1 to 2
			0x62, 0x01, // 6XNN -> Set V2 to 1
			0xA0, 0x1A, // ANNN -> SET I to address 0x01A
			0x0E, 0xB6, // 0EB6 -> move "Test 2" to Vector3(1, 2, 3)

			0x60, 0x00, // 6XNN -> Set V0 to 0
			0x61, 0x00, // 6XNN -> Set V1 to 0
			0x62, 0x2D, // 6XNN -> Set V2 to 45
			0xA0, 0x1A, // ANNN -> SET I to address 0x01A
			0x0E, 0xB7, // 0EB6 -> rotate "Test 2" to Vector3(0, 0, 45)

			0x60, 0x02, // 6XNN -> Set V0 to 2
			0x61, 0x02, // 6XNN -> Set V1 to 2
			0x62, 0x02, // 6XNN -> Set V2 to 2
			0xA0, 0x1A, // ANNN -> SET I to address 0x01A
			0x0E, 0xB8, // 0EB6 -> scale "Test 2" to Vector3(2, 2, 2)

			0xA0, 0x1A,	// ANNN -> SET I to address 0x01A
			0x0E, 0xBA, // 0EBA -> addMaterial to "Test 2"

			0xA0, 0x1A,	// ANNN -> SET I to address 0x01A
			0x60, 0x7F, // 6XNN -> SET V0 to 127
			0x61, 0x00, // 6XNN -> SET V1 to 0
			0x62, 0x00, // 6XNN -> SET V2 to 0
			0x63, 0xFF, // 6XNN -> SET V3 to 255
			0x0E, 0xBB, // 0EBB -> setMaterialColor to Rgba(0.5f, 0f, 0f, 0f)

			//0xA0, 0x1A, // ANNN -> SET I to address 0x01A
			//0x0E, 0xBF, // 0EBF -> destroy "Test 2"

			// reparent
			0x60, 0x00, // 6XNN -> SET V0 to 0x00
			0x61, 0x10, // 6XNN -> SET V1 to 0x10
			0xA0, 0x1A, // ANNN -> SET I to address 0x01A
			0x0E, 0xB9, // 0EB9 -> reparent "Test 2" to "Test Cube"
*/

			// network data transfer tests
			0x60, 0x4E, // 6XNN -> SET V0 to 78   // N
			0x61, 0x65, // 6XNN -> SET V1 to 101  // e
			0x62, 0x74, // 6XNN -> SET V2 to 116  // t
			0x63, 0x00, // 6XNN -> SET V3 to 0    // (terminator)
			0x64, 0x04, // 6XNN -> SET V3 to 4
			0xAD, 0x50, // ANNN -> SET I to the default UniCHIP8 data port address
			0xF4, 0x55, // FX55 -> STORE V4 (4) registers (V0-V3) to the data port buffer
			0xA0, 0x21, // ANNN -> SET I to address 0x021
			0x0E, 0xF0, // 0EF0 -> send contents of the data port buffer ("Net") to "Blue CPU"

			// machine state tests
/*
			//0x60, 0x01, // 6XNN -> SET V0 to 1
			//0x0E, 0xF9, // 0EF9 -> enable logging

			//0x60, 0x01, // 6XNN -> SET V0 to 1
			//0x0E, 0xFA, // 0EFA -> enable compatibility mode

			//0x60, 0x05, // 6XNN -> SET V0 to 5
			//0x0E, 0xFB, // 0EFB -> SET clock multiplier to 5

			//0x0E, 0xFC, // 0EFC -> pause
			//0x0E, 0xFD, // 0EFD -> halt
			//0x0E, 0xFE, // 0EFE -> reset
			//0x0E, 0xFF, // 0EFF -> powerDown
*/
		};

		for (int i=0; i<programData.Length; i++)
			ram[0x200 + i] = programData[i];

		// halt at end of program
		ram [(0x200 + programData.Length)] = 0x0E;
		ram [(0x200 + programData.Length + 1)] = 0xFD;

		if (logging)
			print ("LoadROM: wrote " + (programData.Length + 2) + " bytes to RAM");
	}
	
	void LoadROMFromFile(string path) {
		FileInfo fileInfo = new FileInfo (path);
		int fileSize = (int) fileInfo.Length;
		FileStream stream = new FileStream(path, System.IO.FileMode.Open);
		BinaryReader reader = new BinaryReader(stream);
		
		// copy to ram beginning at 0x200
		int j = 0;
		byte[] tmp = new byte[ram.Length];
		tmp = reader.ReadBytes (fileSize);
		reader.Close ();
		stream.Close ();
		
		for (int i=bootAddress; i < ram.Length; i++) {
			if (j < tmp.Length)
				ram[i] = tmp[j++];

			else
				i = ram.Length;
		}
	}
	
	// Convert 0-999 to array of bytes, encoded as BCD (big-endian)
	// based on https://stackoverflow.com/a/2448511/773209
	byte[] IntToBCD(int input) {
		if (input < 0)
			input = 0;
		
		if (input > 999)
			input = 999;
		
		int thousands = input / 1000;
		int hundreds = (input -= thousands * 1000) / 100;
		int tens = (input -= hundreds * 100) / 10;
		int ones = (input -= tens * 10);
		
		byte[] bcd = new byte[] {
			(byte)(hundreds),
			(byte)(tens),
			(byte)(ones)
		};
		
		return bcd;
	}

	static string ByteToHex(byte theByte) {
		char[] c = new char[2];
		int b;
		
		b = theByte >> 4;
		c [0] = (char)(55 + b + (((b - 10) >> 31) & -7));
		b = theByte & 0xF;
		c[1] = (char)(55+b+(((b-10)>>31)&-7));
		
		return new string (c);
	}

	static string UshortToHex(ushort theUshort) {
		string lsb = ByteToHex((byte) (theUshort & 0xFF));
		string msb = ByteToHex((byte) (theUshort >> 8));
		return msb + lsb;
	}

	static string ToHex(byte b) {
		return ByteToHex (b);
	}

	static string ToHex(ushort u) {
		return UshortToHex (u);
	}

	static string ToHexNibble(byte b) {
		string hexString = ToHex (b);
		return hexString [hexString.Length - 1] + "";
	}

	static string ToHexString(byte b) {
		return "0x" + ToHex (b);
	}

	static string ToHexString(ushort u) {
		return "0x" + ToHex (u);
	}

	void WriteASCIIString(ushort address, string str) {
		int length = str.Length;

		if (length > 31)
			length = 31;

		for (int i=0; i<length; i++)
			ram [(address + i)] = (byte) str [i];

		ram[(address + length + 1)] = 0; // add terminal null byte
	}

	string ReadASCIIString(ushort address) {
		int maxLength = 32;
		int length = 0;
		int i = 0;
		char[] tmpChars = new char[maxLength];
		char[] chars;
		string output = "";

		// copy ascii chars, determine actual length of string data
		for (i=0; i < maxLength; i++) {
			char c = (char)ram [(address + i)];

			if (((int)c > 31) && ((int)c < 127)) {
				tmpChars [i] = c;
				length++;

			} else {
				break;
			}
		}

		// build & return the string
		chars = new char[length];

		for (i=0; i < length; i++)
			chars [i] = tmpChars [i];

		output = new String (chars);
		return output.Trim ();
	}

	void LogIteration(ushort opcode) {
		string historyLog = "";
		historyLog += "T:" + tickCount;
		historyLog += " PC:" + ToHexString (PC) + "(" + PC + ")";
		historyLog += " OPCODE " + ToHexString(opcode) + "(" + opcode + ")"; 
		historyLog += " I:"  + ToHexString(I) + "(" + I + ")";
		historyLog += " SP:" + ToHexString(SP) + "(" + SP + ")";
		historyLog += " DT:" + ToHexString(DT) + "(" + DT + ")";
		historyLog += " ST:" + ToHexString(ST) + "(" + ST + ")";

		for (int i=0; i<16; i++)
			historyLog += " V" + ToHexNibble((byte) i) + ":" + ToHexString(V[i]) + "(" + V[i] + ")";

		print (historyLog);
	}

	void Halt(string msg) {
		print("HALT: " + msg);
		powerState = false;
	}
	
	void FixedUpdate () {
		if (! powerState)
			return;

		// Set clockMultiplier > 1 to process multiple opcodes per FixedUpdate()
		for (int iteration = 0; iteration < clockMultiplier; iteration++) {
			float proximityTargetDistance = 1000f;

			if (proximityKeyboard && proximityTarget != null)
				proximityTargetDistance = Vector3.Distance(proximityTarget.transform.position, transform.position);

			bool keyboardFocused = (!proximityKeyboard) || (proximityTargetDistance < proximityDistance);

			if (hasKeyboard && keyboardFocused) {
				// Handle Keys
				Keys [ 0] = Input.GetKey ("x");
				Keys [ 1] = Input.GetKey ("1");
				Keys [ 2] = Input.GetKey ("2");
				Keys [ 3] = Input.GetKey ("3");
				Keys [ 4] = Input.GetKey ("q");
				Keys [ 5] = Input.GetKey ("w");
				Keys [ 6] = Input.GetKey ("e");
				Keys [ 7] = Input.GetKey ("a");
				Keys [ 8] = Input.GetKey ("s");
				Keys [ 9] = Input.GetKey ("d");
				Keys [10] = Input.GetKey ("z");
				Keys [11] = Input.GetKey ("c");
				Keys [12] = Input.GetKey ("4");
				Keys [13] = Input.GetKey ("r");
				Keys [14] = Input.GetKey ("f");
				Keys [15] = Input.GetKey ("v");
			}

			if (waitingForKeypress) {
				for (int k=0; k < Keys.Length; k++) {
					if (Keys[k] == true) {
						V[waitingRegister] = (byte) k;
						waitingForKeypress = false;
						break;
					}
				}
				
				if (waitingForKeypress)
					return;
			}
		
			// Process Iteration
			tickCount += 1;
			
			bool shouldIncrementPC = true;
			bool skipNextInstruction = false;
			
			// fetch
			ushort opcode = (ushort) (ram[PC] << 8 | ram[PC + 1]);
			byte F     = 0xF;
			byte X     = (byte)   ((opcode & 0x0F00) >> 8);
			byte Y     = (byte)   ((opcode & 0x00F0) >> 4);
			byte N     = (byte)   (opcode & 0x000F);
			byte NN    = (byte)   (opcode & 0x00FF);
			ushort NNN = (ushort) (opcode & 0x0FFF);

			if (logging)
				LogIteration(opcode);

			// decode & execute
			switch ((opcode & 0xF000)) { // check first nibble
			case 0x0000:
				if (opcode == 0x0000) {
					// NOP
				
				} else if ((opcode & 0x00FF) == 0x00E0)	// 00E0 Clear Screen
					ClearScreen();
				
				else if ((opcode & 0x000F) == 0x000E) {	// 00EE Return from Subroutine
					PC = Stack[SP];
					SP -= 1;
				}

				// Unity integration opcodes
				if (! compatibilityMode) {
					string targetName = ReadASCIIString(I);

					if ((opcode & 0x0FFF) == 0x0E00) {	// 0E00 router test
						if (router != null) {
							router.SendMessage("Command", this.name + "|call~Beep");
							//router.SendMessage("Data", this.name + "|Hello World!");
						}
					}

					else if ((opcode & 0x0FFF) == 0x0E01) { // 0E01 (call) 
						if (router != null) {
							string methodName = ""; // read from v0,v1
							router.SendMessage("Command", targetName + "|call~" + methodName);
						}
					}

					else if ((opcode & 0x0FFF) == 0x0E02) { // 0E02 (send) send the bytes in the dataport to the targetGameObject
						string data = ReadASCIIString(dataPortAddress);
						router.SendMessage ("Data", targetName + "|" + data);
					}

					else if ((opcode & 0x0FF0) == 0x0E10) { // 0E1N (moveX) targetGameObject.transform.position.x = V[N]
						if (router != null)
							router.SendMessage("Command", targetName + "|moveX~" + V[N]);
					}

					else if ((opcode & 0x0FF0) == 0x0E20) { // 0E2N (moveY) targetGameObject.transform.position.y = V[N]
						if (router != null)
							router.SendMessage("Command", targetName + "|moveY~" + V[N]);
					}
					
					else if ((opcode & 0x0FF0) == 0x0E30) { // 0E3N (moveZ) targetGameObject.transform.position.z = V[N]
						if (router != null)
							router.SendMessage("Command", targetName + "|moveZ~" + V[N]);
					}
					
					else if ((opcode & 0x0FF0) == 0x0E40) { // 0E4N (rotateX) targetGameObject.transform.localRotation.x = V[N]
						if (router != null)
							router.SendMessage("Command", targetName + "|rotateX~" + V[N]);
					}
					
					else if ((opcode & 0x0FF0) == 0x0E50) { // 0E5N (rotateY) targetGameObject.transform.localRotation.y = V[N]
						if (router != null)
							router.SendMessage("Command", targetName + "|rotateY~" + V[N]);
					}
					
					else if ((opcode & 0x0FF0) == 0x0E60) { // 0E6N (rotateZ) targetGameObject.transform.localRotation.z = V[N]
						if (router != null)
							router.SendMessage("Command", targetName + "|rotateZ~" + V[N]);
					}

					else if ((opcode & 0x0FF0) == 0x0E70) { // 0E7N (scaleX) targetGameObject.transform.localScale.x = V[N]
						if (router != null)
							router.SendMessage("Command", targetName + "|scaleX~" + V[N]);
					}

					else if ((opcode & 0x0FF0) == 0x0E80) { // 0E8N (scaleY) targetGameObject.transform.localScale.y = V[N]
						if (router != null)
							router.SendMessage("Command", targetName + "|scaleY~" + V[N]);
					}

					else if ((opcode & 0x0FF0) == 0x0E90) { // 0E9N (scaleZ) targetGameObject.transform.localScale.z = V[N]
						if (router != null)
							router.SendMessage("Command", targetName + "|scaleZ~" + V[N]);
					}

					else if ((opcode & 0x0FF0) == 0x0EA0) { // 0EAN (create) create targetGameObject from prefabs[V[N]]
						if (router != null) {
							print ("0EAN: Loading prefab index from V[" + N + "]");
							int prefabIndex = V[N];
							GameObject go = (GameObject) Instantiate(prefabs[prefabIndex], new Vector3(0, 0, 0), Quaternion.identity);
							go.name = targetName;
							go.AddComponent<UniCHIP8Node>();
							go.GetComponent<UniCHIP8Node>().router = router;
							router.SendMessage("RegisterNode", go);
						}
					}

					else if ((opcode & 0x0FFF) == 0x0EB0) { // 0EB0 (createCube) create targetGameObject from a cube primitive
						if (router != null) {
							GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
							go.name = targetName;
							go.transform.position = new Vector3(0, 0, 0);
							go.AddComponent<UniCHIP8Node>();
							go.GetComponent<UniCHIP8Node>().router = router;
							router.SendMessage("RegisterNode", go);
						}
					}

					else if ((opcode & 0x0FFF) == 0x0EB1) { // 0EB1 (createSphere) create targetGameObject from a sphere primitive
						if (router != null) {
							GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
							go.name = targetName;
							go.transform.position = new Vector3(0, 0, 0);
							go.AddComponent<UniCHIP8Node>();
							go.GetComponent<UniCHIP8Node>().router = router;
							router.SendMessage("RegisterNode", go);
						}
					}

					else if ((opcode & 0x0FFF) == 0x0EB2) { // 0EB2 (createCylinder) create targetGameObject from a cylinder primitive
						if (router != null) {
							GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
							go.name = targetName;
							go.transform.position = new Vector3(0, 0, 0);
							go.AddComponent<UniCHIP8Node>();
							go.GetComponent<UniCHIP8Node>().router = router;
							router.SendMessage("RegisterNode", go);
						}
					}

					else if ((opcode & 0x0FFF) == 0x0EB3) { // 0EB3 (createCapsule) create targetGameObject from a capsule primitive
						if (router != null) {
							GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
							go.name = targetName;
							go.transform.position = new Vector3(0, 0, 0);
							go.AddComponent<UniCHIP8Node>();
							go.GetComponent<UniCHIP8Node>().router = router;
							router.SendMessage("RegisterNode", go);
						}
					}

					else if ((opcode & 0x0FFF) == 0x0EB4) { // 0EB4 (createPlane) create targetGameObject from a plane primitive
						if (router != null) {
							GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);
							go.name = targetName;
							go.transform.position = new Vector3(0, 0, 0);
							go.AddComponent<UniCHIP8Node>();
							go.GetComponent<UniCHIP8Node>().router = router;
							router.SendMessage("RegisterNode", go);
						}
					}

					else if ((opcode & 0x0FFF) == 0x0EB5) { // 0EB5 (createQuad) create targetGameObject from a quad primitive
						if (router != null) {
							GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
							go.name = targetName;
							go.transform.position = new Vector3(0, 0, 0);
							go.AddComponent<UniCHIP8Node>();
							go.GetComponent<UniCHIP8Node>().router = router;
							router.SendMessage("RegisterNode", go);
						}
					}

					else if ((opcode & 0x0FFF) == 0x0EB6) { // 0EB6 (move) targetGameObject.position = new Vector3(V[0],V[1],V[2])
						if (router != null)
							router.SendMessage("Command", targetName + "|move~" + V[0] + "~" + V[1] + "~" + V[2]);
					}

					else if ((opcode & 0x0FFF) == 0x0EB7) { // 0EB7 (rotate) targetGameObject.transform.localRotation = new Vector3(V[0],V[1],V[2])
						if (router != null)
							router.SendMessage("Command", targetName + "|rotate~" + V[0] + "~" + V[1] + "~" + V[2]);
					}
					
					else if ((opcode & 0x0FFF) == 0x0EB8) { // 0EB8 (scale) targetGameObject.transform.localScale = new Vector3(V[0],V[1],V[2])
						if (router != null)
							router.SendMessage("Command", targetName + "|scale~" + V[0] + "~" + V[1] + "~" + V[2]);
					}

					else if ((opcode & 0x0FFF) == 0x0EB9) { // 0EB9 (reparent) targetGameObject to parentGameObject, whose name is stored in the string at address pointed to by V0 and V1
						if (router != null) {
							ushort stringAddress = (ushort) (((ushort) V[0] << 8) | (ushort) V[1]);
							string parentTarget = ReadASCIIString(stringAddress);
							router.SendMessage("Command", targetName + "|reparent~" + parentTarget);
						}
					}

					else if ((opcode & 0x0FFF) == 0x0EBA) { // 0EBA (addMaterial) adds a material to targetGameObject
						if (router != null)
							router.SendMessage("Command", targetName + "|addMaterial");
					}
					
					else if ((opcode & 0x0FFF) == 0x0EBB) { // 0EBB (setMaterialColor) sets the targetGameObject main material color
						if (router != null)
							router.SendMessage("Command", targetName + "|setMaterialColor~" + V[0] + "~" + V[1] + "~" + V[2] + "~" + V[3]);
					}

					/*
					else if ((opcode & 0x0FFF) == 0x0EBC) { // 0EBC () targetGameObject
						if (router != null)
							router.SendMessage("Command", targetName + "|");
					}
					
					else if ((opcode & 0x0FFF) == 0x0EBD) { // 0EBD () targetGameObject
						if (router != null)
							router.SendMessage("Command", targetName + "|");
					}

					else if ((opcode & 0x0FFF) == 0x0EBE) { // 0EBE () targetGameObject
						if (router != null)
							router.SendMessage("Command", targetName + "|");
					}					
					*/

					else if ((opcode & 0x0FFF) == 0x0EBF) { // 0EBF (destroy) targetGameObject
						if (router != null)
							router.SendMessage ("Command", targetName + "|destroy");
					}

					// 0EC0..0EF8

					else if ((opcode & 0x0FFF) == 0x0EF9) { // 0EF9 (logging) enable or disable logging using V0 as flag
						logging = (V[0] > 0) ? true : false;
					}

					else if ((opcode & 0x0FFF) == 0x0EFA) { // 0EFA (compatibilityMode) enable CHIP-8 compatibility
						compatibilityMode = true;			//		note: compatibilityMode must be false to use this opcode
					}

					else if ((opcode & 0x0FFF) == 0x0EFB) { // 0EFB (clockMultiplier) set the clock multiplier specified in V0
						clockMultiplier = V[0];
					}

					else if ((opcode & 0x0FFF) == 0x0EFC) { // 0EFC (pause) set manual breakpoints in code
						Halt("program paused");
					}

					else if ((opcode & 0x0FFF) == 0x0EFD) { // 0EFD (halt) an alias for pause
						Halt ("program halted");
					}

					else if ((opcode & 0x0FFF) == 0x0EFE) { // 0EFE (reset) resets registers to their initial power on states. Any
						Reset ();                           //      GameObjects previously created via an associated
					}										//      UniCHIP8Router, and having their .destroyOnReset
															//      properties set to true will be destroyed. (By default
															//      UniCHIP8, which inherits from UniCHIP8Node, sets it's
															//      destroyOnReset to false so the virtual machine does not
															//      get destroyed.)
					
					else if ((opcode & 0x0FFF) == 0x0EFF) { // 0EFF (powerDown) simulates a power down and resets the system
						ClearScreen();
						Halt ("power down");

						Reset ();

						if (router != null)
							router.SendMessage("Reset");
					}
				}
				break;
				
			case 0x1000:								// 1NNN Jump to address
				PC = NNN;
				shouldIncrementPC = false;
				break;
				
			case 0x2000: 								// 2NNN Call Subroutine at address
				SP += 1;
				Stack[SP] = PC;
				PC = NNN;
				shouldIncrementPC = false;
				break;
				
			case 0x3000:								// 3XNN Skip next if Vx == NN
				if (V [X] == NN)
					skipNextInstruction = true;
				break;
				
			case 0x4000:								// 4XNN Skip next if Vx != NN
				if (V [X] != NN)
					skipNextInstruction = true;
				break;
				
			case 0x5000:								// 5XY0 Skip next if Vx == Vy
				if (V [X] == V [Y])
					skipNextInstruction = true;
				break;
				
			case 0x6000: 								// 6XNN Set Vx to NN
				V [X] = NN;
				break;
				
			case 0x7000:								// 7XNN Add NN to Vx (Cary flag not changed)
				V [X] += NN;
				break;
				
			case 0x8000:
				if ((opcode & N) == 0x0000) 			// 8XY0 Sets Vx to Vy
					V [X] = V [Y];
				
				else if ((opcode & N) == 0x0001)		// 8XY1 Sets Vx to Vx|Vy
					V [X] = (byte) (V [X] | V [Y]);
				
				else if ((opcode & N) == 0x0002)		// 8XY2 Sets Vx to Vx&Vy
					V [X] = (byte) (V [X] & V [Y]);
				
				else if ((opcode & N) == 0x0003)		// 8XY3 Sets Vx to Vx^Vy
					V [X] = (byte) (V [X] ^ V [Y]);
				
				else if ((opcode & N) == 0x0004) {		// 8XY4 Add Vx to Vy (sets Vf (carry flag))
					byte regX = V[X];
					byte regY = V[Y];
					ushort newValue = (ushort) (regX + regY);
					
					if (newValue > 255) {
						newValue -= 256;
						V[F] = 1;
					} else {
						V[F] = 0;
					}
					
					V [X] = (byte) newValue;
				}
				
				else if ((opcode & N) == 0x0005) {		// 8XY5 Subtract Vy from Vx (sets Vf (borrow flag))
					byte regX = V[X];
					byte regY = V[Y];
					ushort newValue = (ushort) (regX - regY);
					
					if (newValue < 0) {
						newValue += 256;
						V[F] = 1;

					} else
						V[F] = 0;
					
					V [X] = (byte) newValue;
				}
				
				else if ((opcode & N) == 0x0006) {		// 8XY6 Shifts Vx right by one, stores result in Vx
					V [F] = (byte) (V [X] & 0x1);
					V [X] = (byte) (V [Y] >> 1);
				}
				
				else if ((opcode & N) == 0x0007) {		// 8XY7 Vx = Vy - Vx
					byte regX = V[X];
					byte regY = V[Y];
					ushort newValue = (ushort) (regY - regX);
					
					if (newValue < 0) {
						newValue += 256;
						V[F] = 1;

					} else
						V[F] = 0;
					
					V [X] = (byte) newValue;
				}
				
				else if ((opcode & N) == 0x000E) { 		// 8XYE Shifts Vx Left by one, stores result in Vx
					byte regX = V[X];
					ushort newValue = (ushort) (regX << 1);
					
					if (newValue > 255) {
						newValue -= 256;
						V[F] = 1;

					} else
						V[F] = 0;
					
					V [X] = (byte) newValue;
				}
				break;
				
			case 0x9000: 								// 9XY0 Skip next if Vx != Vy
				if (V [X] != V [Y])
					skipNextInstruction = true;
				break;
				
			case 0xA000: 								// ANNN Set I to address
				I = NNN;
				break;
				
			case 0xB000: 								// BNNN Jump to address NNN + V0
				PC = (ushort) (NNN + V [0]);
				shouldIncrementPC = false;
				break;
				
			case 0xC000: 								// CXNN Sets VX to the result of a bitwise and operation on a random number (Typically: 0 to 255) and NN.
				int num = UnityEngine.Random.Range (0, 255);
				V [X] = (byte) (num & NN);
				break;
				
			case 0xD000: 								// DXYN Draws a sprite at coordinate (VX, VY) that has a width of 8 pixels and a height of N pixels.
				V[F] = 0;
				
				byte regX = V[X];
				byte regY = V[Y];
				int height = N;
				byte x = 0;
				byte y = 0;
				int spr = 0;
				byte drawX = 0;
				byte drawY = 0;
				
				for (y = 0; y < height; y++) {
					drawY = (byte) (regY + y);
					spr = ram[I + y];
					
					for (x = 0; x < 8; x++) {
						if ((spr & (int) Mathf.Pow (2, x)) > 0) {
							drawX = (byte) (regX + (8-x)); // Note: regX+(8-x) instead of regX+x: textures are rtl not ltr
							if (SetPixel(drawX, drawY))
								V[F] = 1;
						}
					}
				}
				
				RenderScreen ();
				break;
				
			case 0xE000:
				if ((opcode & N) == 0x000E) {			// EX9E Skip next if key stored in Vx is pressed
					int keyId = V[X];
					bool keyPressed = Keys[keyId];
					
					if (keyPressed)
						skipNextInstruction = true;
				}
				
				else if ((opcode & N) == 0x0001) {		// EXA1 Skip next if key stored in Vx isn't pressed
					int keyId = V[X];
					if (keyId < 0)
						keyId = 0;
					
					if (keyId > 15)
						keyId = 15;
					
					bool keyPressed = Keys[keyId];
					
					if (!keyPressed)
						skipNextInstruction = true;
				}
				break;
				
			case 0xF000:
				if ((opcode & NN) == 0x0007)			// FX07 Set Vx to DT
					V [X] = DT;
				
				else if ((opcode & NN) == 0x000A) {		// FX0A Wait for keypress, store in Vx (BLOCKING OP)
					V [X] = 0;
					waitingRegister = X;
					waitingForKeypress = true;
				}
				
				else if ((opcode & NN) == 0x0015)		// FX15 Set DT to Vx
					DT = V [X];
				
				else if ((opcode & NN) == 0x0018)		// FX18 Set ST to Vx
					ST = V [X];
				
				else if ((opcode & NN) == 0x001E)		// FX1E Add Vx to I
					I += (ushort) V [X];
				
				else if ((opcode & NN) == 0x0029)		// FX29 Set I to Font-Sprite location for char in Vx
					I = (ushort) (fontDataStart + (ushort) (V[X] * 5));
				
				else if ((opcode & NN) == 0x0033) {		// FX33 Stores BCD representation of Vx at I,I+1,I+2
					int value = V[X];
					byte[] bcdBytes = IntToBCD(value);
					
					ram[I]     = bcdBytes[0];
					ram[I + 1] = bcdBytes[1];
					ram[I + 2] = bcdBytes[2];
				}
				
				else if ((opcode & NN) == 0x0055) {		// FX55 Dump V0 through Vx to addresses I through I+x
					byte numRegistersToStore = (byte) (V[X] & 0x0F);

					for (int i = 0; i <= numRegistersToStore; i++) {
						ram [I + i] = (byte)V [i];
					}
				}
				
				else if ((opcode & NN) == 0x0065) {		// FX65 Read V0 through Vx from addresses I through I+VX
					byte numRegistersToLoad = (byte) (V[X] & 0x0F);

					for (int i = 0; i <= numRegistersToLoad; i++)
						V [i] = ram [(I + i)];
				}
				
				break;
				
			default:
				print ("Unknown opcode");
				break;
			}
			// END DECODE & EXECUTE
			
			
			if (shouldIncrementPC) {
				// HALT IF PC GOES OUT OF RANGE
				if (PC >= ram.Length - 2)
					Halt ("PC out of bounds");
				else
					PC += 2; // opcodes are 2 bytes
				
				if (skipNextInstruction) {
					if (PC >= ram.Length - 2)
						Halt("PC out of bounds");
					else
						PC += 2;
				}
			}
		
			//update timers
			if (Time.deltaTime > 1 / 60) {
				if (DT > 0)
					DT -= 1;
				
				if (ST > 0) {
					if (ST == 1)
						Beep();
					ST -= 1;
				}
			}
			
			RenderScreen();
		} // end iteration
	}

	override public void Receive(string data) {
		print ("\"" + name + "\" received data: \"" + data + "\" (STUB: CALL AN INTERRUPT HANDLER!)");
		
		if (hasDataPort) {
			WriteASCIIString(dataPortAddress, data);
			// execute an "interrupt" to process the data (ie. handle GameObject collisions)
		}
	}
}