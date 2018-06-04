using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// A CHIP-8 Implementation for Unity 3D

public class UniCHIP8 : UniCHIP8Node {
	[Header("Machine State")]
	public bool powerState;
	public int clockMultiplier = 1;
	public bool compatibilityMode = true;
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
	public byte[] V;
	public ushort I;
	public ushort PC;
	public byte DT;
	public byte ST;
	public byte SP;
	public ushort[] Stack;

	[Header("ROM")]
	public string romFolder = "Assets/UniCHIP8/Roms";
	public string romFilename = "";
	
	[Header("RAM")]
	public ushort bootAddress = 0x200;
	public ushort fontDataStart = 0xD7F;
	public byte[] ram;
	public byte[] vram;

	[Header("Hardware")]
	public GameObject screenObject;
	private Texture2D screenTexture;
	public Color backgroundColor = Color.black;
	public Color foregroundColor = Color.green;
	public AudioClip speakerSound;
	private AudioSource audioSource;

	[Header("Scene Integration")]
	public GameObject[] prefabs;

	void Start () {
		screenTexture = new Texture2D (64, 32);
		screenObject.GetComponent<Renderer> ().material.mainTexture = screenTexture;
		audioSource = GetComponent<AudioSource> ();
		
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

		if (router != null) {
			router.BroadcastMessage("RegisterNode", gameObject);
		}
	}
	
	void Beep() {
		if (speakerSound == null)
			print ("BEEP");

		else
			audioSource.PlayOneShot (speakerSound, 1F);
	}
	
	void ClearScreen() {
		for (int i=0; i<2048; i++)
			vram[i] = 0;
		
		RenderScreen ();
	}
	
	void RenderScreen() {
		int i = 0;
		int j = 64 * 31; // Textures are bottom-to-top not top-to-bottom; skip to last row
		Color[] screenData = new Color[2048];
		
		for (int y=0; y<32; y++) {
			for (int x = 0; x < 64; x++) {
				byte srcByte = vram[i++];
				screenData[j++] = srcByte > 0 ? foregroundColor : backgroundColor;
			}
			
			j -= 128; // go to start of previous row (64 bytes-per-row)
		}
		screenTexture.SetPixels (screenData);
		screenTexture.Apply ();
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
		// FIXME: USE WriteString() or Octo
		ram [0x010] = 84;  // T
		ram [0x011] = 101; // e
		ram [0x012] = 115; // s
		ram [0x013] = 116; // t
		ram [0x014] = 32;  // (space)
		ram [0x015] = 67;  // C
		ram [0x016] = 117; // u
		ram [0x017] = 98;  // b
		ram [0x018] = 101; // e
		ram [0x019] = 0;   // (null terminator)
		ram [0x01A] = 84;  // T
		ram [0x01B] = 101; // e
		ram [0x01C] = 115; // s
		ram [0x01D] = 116; // t
		ram [0x01E] = 32;  // (space)
		ram [0x01F] = 50;  // 2
		ram [0x020] = 0;   // (null terminator)

		// Draws a BCD number (042) to the top-left of the texture
		byte[] programData = new byte[] {
			0x65, 0x03, // 6XNN -> SET V5 to 3
//			0xF5, 0x18, // 0x200: FX18 -> BEEP
			
			0x00, 0xE0, // 0x202: 00E0 -> CLEAR screen
			
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
		};

		for (int i=0; i<programData.Length; i++)
			ram[0x200 + i] = programData[i];

		// hang at end of program (if length <= 255 opcodes) using a jump-to-self opcode
		if (programData.Length <= 0xFF) {
			ram [(0x200 + programData.Length)] = 0x12;
			ram [(0x200 + programData.Length + 1)] = (byte)programData.Length;

			if (logging)
				print ("LoadROM: wrote " + (programData.Length + 2) + " bytes to RAM");

		} else {
			// FIXME: build opcode using 12 bit addresses (we're only using the least significant byte above)

			if (logging)
				print ("LoadROM: wrote " + programData.Length + " bytes to RAM");
		}
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
		// FIXME
		print ("WriteASCIIString STUB");
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
					// opcodes 0x0E00 through 0x0EFF expect a target gameObject.name, stored in ram[] as a
					// null-terminated ascii string upto 32 characters in length; SET I to the starting
					// address of the string before executing opcodes in this range.

					// opcodes 0x0EB6 through 0x0EB8 also expect values in V0, V1, and V2; these are used
					// to construct a Vector3 for the transformations.
				
					// FIXME: encode as floats in V0 through VB (4 bytes per float), support negative values
					// and values larger than 256 in general (ie. a byte)
					//
					// opcode 0x0EB9 expects the address of a string in V0 and V1; this is the name of the
					// GameObject to parent to.
					//
					// opcode 0x0EBB expects an rgba value in V0, V1, V2, and V3

					string targetName = ReadASCIIString(I);

					if ((opcode & 0x0FF0) == 0x0E00) {	// 0E00 router test
						if (router != null) {
							router.SendMessage("Command", this.name + "|call~Beep");

							/* Other examples:
								// transform a test object
								router.SendMessage("Command", "Test Cube|move~0~0~4");
								router.SendMessage("Command", "Test Cube|rotate~0~45~0");
								router.SendMessage("Command", "Test Cube|scale~1~10~1");

								// transform our own GameObject (UniCHIP8 inherits from UniCHIP8Node)
								router.SendMessage("Command", this.name + "|move~0~0~0");
								router.SendMessage("Command", this.name + "|scale~1~1~1");

								// translate a non-existent object
								router.SendMessage ("Command", "nonexistent|move~0~0~0");
								
								// send data
								router.SendMessage ("Data", "Test Cube|Hello, World!");
								router.SendMessage ("Data", this.name + "|Hello, World too!");

								router.SendMessage("Command", "Bridge 21|call~Explode");
							*/
						}
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

					else if ((opcode & 0x0FF0) == 0x0EA0) { // 0EAN (create) targetGameObject from prefabs[V[N]]
						if (router != null) {
							int prefabIndex = V[N];
							GameObject go = (GameObject) Instantiate(prefabs[prefabIndex], new Vector3(0, 0, 0), Quaternion.identity);
							go.name = targetName;
							go.AddComponent<UniCHIP8Node>();
							go.GetComponent<UniCHIP8Node>().router = router;
							router.SendMessage("RegisterNode", go);
						}
					}

					else if ((opcode & 0x0FFF) == 0x0EB0) { // 0EB0 (createCube) targetGameObject
						if (router != null) {
							GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
							go.name = targetName;
							go.transform.position = new Vector3(0, 0, 0);
							go.AddComponent<UniCHIP8Node>();
							go.GetComponent<UniCHIP8Node>().router = router;
							router.SendMessage("RegisterNode", go);
						}
					}

					else if ((opcode & 0x0FFF) == 0x0EB1) { // 0EB1 (createSphere) targetGameObject
						if (router != null) {
							GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
							go.name = targetName;
							go.transform.position = new Vector3(0, 0, 0);
							go.AddComponent<UniCHIP8Node>();
							go.GetComponent<UniCHIP8Node>().router = router;
							router.SendMessage("RegisterNode", go);
						}
					}

					else if ((opcode & 0x0FFF) == 0x0EB2) { // 0EB2 (createCylinder) targetGameObject
						if (router != null) {
							GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
							go.name = targetName;
							go.transform.position = new Vector3(0, 0, 0);
							go.AddComponent<UniCHIP8Node>();
							go.GetComponent<UniCHIP8Node>().router = router;
							router.SendMessage("RegisterNode", go);
						}
					}

					else if ((opcode & 0x0FFF) == 0x0EB3) { // 0EB3 (createCapsule) targetGameObject
						if (router != null) {
							GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
							go.name = targetName;
							go.transform.position = new Vector3(0, 0, 0);
							go.AddComponent<UniCHIP8Node>();
							go.GetComponent<UniCHIP8Node>().router = router;
							router.SendMessage("RegisterNode", go);
						}
					}

					else if ((opcode & 0x0FFF) == 0x0EB4) { // 0EB4 (createPlane) targetGameObject
						if (router != null) {
							GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);
							go.name = targetName;
							go.transform.position = new Vector3(0, 0, 0);
							go.AddComponent<UniCHIP8Node>();
							go.GetComponent<UniCHIP8Node>().router = router;
							router.SendMessage("RegisterNode", go);
						}
					}

					else if ((opcode & 0x0FFF) == 0x0EB5) { // 0EB5 (createQuad) targetGameObject
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

					else if ((opcode & 0x0FFF) == 0x0EB9) { // 0EB9 (reparent) targetGameObject to parentGameObject (name string address stored
						if (router != null) {				//      in V0 and V1
							ushort stringAddress = (ushort) (((ushort) V[0] << 8) | (ushort) V[1]);
							string parentTarget = ReadASCIIString(stringAddress);
							router.SendMessage("Command", targetName + "|reparent~" + parentTarget);
						}
					}

					else if ((opcode & 0x0FFF) == 0x0EBA) { // 0EBA (addMaterial) targetGameObject
						if (router != null)
							router.SendMessage("Command", targetName + "|addMaterial");
					}
					
					else if ((opcode & 0x0FFF) == 0x0EBB) { // 0EBB (setMaterialColor) targetGameObject
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
					
					else if ((opcode & 0x0FFF) == 0x0EBF) { // 0EBF () targetGameObject
						if (router != null)
							router.SendMessage("Command", targetName + "|");
					}
					*/

					else if ((opcode & 0x0FFF) == 0x0EBF) { // 0EBF (destroy) targetGameObject
						if (router != null)
							router.SendMessage ("Command", targetName + "|destroy");
					}

					// 0EC0..0EFF
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
					for (int i = 0; i < V[X]; i++)
						ram [I + i] = (byte)V [i];
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
		print ("\"" + name + "\" received data: " + data + " (STUB: WRITE TO ram[] AND CALL AN INTERRUPT HANDLER!)");

		// FIXME: Write data to ram[], then execute an "interrupt" to process the data (ie. handle GameObject collisions)
	}
}