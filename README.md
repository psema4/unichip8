# UniCHIP8

A CHIP-8 implementation for Unity 3D based on Laurence Muller's tutorial, "[How to write an emulator (CHIP-8 interpreter)](http://www.multigesture.net/articles/how-to-write-an-emulator-chip-8-interpreter/)"

![UniCHIP8 version 1.0 screenshot](/Assets/UniCHIP8/unichip8-v1.png?raw=true "UniCHIP8 Version 1.0")

## Usage

1. Import [the UniCHIP asset package](https://github.com/psema4/unichip8/raw/master/PackageBuild/UniCHIP8.unitypackage) into your project
1. Create a quad and assign the UniCHIP8Screen material to it
1. Create an empty GameObject and add the UniCHIP8.cs script to it
1. Assign your screen quad to UniCHIP8's Screen Object property
1. Optionally, assign the beep sound to UniCHIP8's Speaker Sound property
1. Optionally, type in the name of a ROM file located in the Roms folder
1. Ensure the UniCHIP8's Power State property is checked
1. Position the camera near the quad so it takes up most of the view
1. Press Play in the Unity Editor

## Keyboard Layout

    Original           QWERTY
    +---+---+---+---+  +---+---+---+---+
    | 1 | 2 | 3 | C |  | 1 | 2 | 3 | 4 |
    +---+---+---+---+  +---+---+---+---+
    | 4 | 5 | 6 | D |  | q | w | e | r |
    +---+---+---+---+  +---+---+---+---+
    | 7 | 8 | 9 | E |  | a | s | d | f |
    +---+---+---+---+  +---+---+---+---+
    | A | 0 | B | F |  | z | x | c | v |
    +---+---+---+---+  +---+---+---+---+

	
## Playing the Blitz rom

Press 'w' to proceed from the title and level screens.  Destroy buildings by dropping bombs with the 'w' key.

## UniCHIP8 Extensions

UniCHIP8 offers a number of custom opcodes that enable integration with your Unity 3D scenes through the use of a simple message router.

To enable UniCHIP8 Extensions:

* Disable the Compatibility Mode property on your UniCHIP8 instance
* Ensure your scene has a UniCHIP Router in it.  If not, add one from the UniCHIP8 prefabs folder and assign it to the Router property on your UniCHIP8 instance

### UniCHIP8 Router & Nodes

The UniCHIP8Router acts as a gateway between the UniCHIP8 and registered nodes (GameObjects having the UniCHIP8Node component).

The UniCHIP8Node component provides a basic command interpreter for the commands generated by UniCHIP8 Extension opcodes.

### Caveats

* UniCHIP8 Router command arguments are drawn from byte sources (the V registers), limiting numeric values to the positive integers from 0 to 255
* Transform commands do not take Time.deltaTime into account
* When adding a material to a GameObject, that material will default to the "Standard" shader.

### Calling Extensions

| opcode(s) | requires |
|-----------|--------------|
| 0E00 through 0x0EEF | that I be set to the starting address of a string containing the targetGameObject's name before executing opcodes in this range |
| 0EB6 through 0x0EB8 | values in V0, V1, and V2; these are used to construct a Vector3 for the relevant transformations |
| 0EB9 | the address of a null-terminated string (the name of the intended parent GameObject) in V0 and V1 |
| 0EBB | an RGBA value stored in V0, V1, V2, and V3 respectively |
| 0EF9 | V0 as a flag (0/non-zero) to enable or disable UniCHIP8 logging |
| 0EFB | sets the clock multiplier to the value in V0 |

### Extension opcodes

| opcode | mnemonic | description |
|--------|----------|-------------|
| 0E1N | moveX | targetGameObject.transform.position.x = V[N] |
| 0E2N | moveY | targetGameObject.transform.position.y = V[N] |
| 0E3N | moveZ | targetGameObject.transform.position.z = V[N] |
| 0E4N | rotateX | targetGameObject.transform.localRotation.x = V[N] |
| 0E5N | rotateY | targetGameObject.transform.localRotation.y = V[N] |
| 0E6N | rotateZ | targetGameObject.transform.localRotation.z = V[N] |
| 0E7N | scaleX | targetGameObject.transform.localScale.x = V[N] |
| 0E8N | scaleY | targetGameObject.transform.localScale.y = V[N] |
| 0E9N | scaleZ | targetGameObject.transform.localScale.z = V[N] |
| 0EAN | create | create targetGameObject from prefabs[V[N]] |
| 0EB0 | createCube | create targetGameObject from a cube primitive |
| 0EB1 | createSphere | create targetGameObject from a sphere primitive |
| 0EB2 | createCylinder | create targetGameObject from a cylinder primitive |
| 0EB3 | createCapsule | create targetGameObject from a capsule primitive |
| 0EB4 | createPlane | create targetGameObject from a plane primitive |
| 0EB5 | createQuad | create targetGameObject from a quad primitive |
| 0EB6 | move | targetGameObject.position = new Vector3(V0, V1, V2) |
| 0EB7 | rotate | targetGameObject.transform.localRotation = new Vector3(V0, V1, V2) |
| 0EB8 | scale | targetGameObject.transform.localScale = new Vector3(V0, V1, V2) |
| 0EB9 | reparent | targetGameObject to parentGameObject, whose name is stored in the string at the address pointed to by V0 and V1 |
| 0EBA | addMaterial | adds a specular material to targetGameObject |
| 0EBB | setMaterialColor | sets the targetGameObject main material color |
| ... | | |
| 0EF9 | logging | enable or disable logging using V0 as flag |
| 0EFA | compatibilityMode | enable CHIP-8 compatibility |
| 0EFB | clockMultiplier | set the clock multiplier specified in V0 |
| 0EFC | pause | set manual breakpoints in code |
| 0EFD | halt | an alias for pause |
| 0EFE | reset | resets registers to their initial power on states. Any GameObjects previously created via an associated UniCHIP8Router, and having their .destroyOnReset properties set to true will be destroyed. By default UniCHIP8, which inherits from UniCHIP8Node, sets it's destroyOnReset to false so the virtual machine does not get destroyed. |
| 0EFF | powerDown | simulates a power down and resets the system |

### Related conventions

* CHIP-8 rom file extension .ch8
* UniCHIP8 rom file extension .uc8
* UniCHIP8 strings are null-terminated and must not be greater than 32 characters
 
## Bugs

* https://github.com/psema4/unichip8/issues

## Development Repository

* https://github.com/psema4/unichip8

## References

* https://en.wikipedia.org/wiki/CHIP-8
* http://devernay.free.fr/hacks/chip8/C8TECH10.HTM
* http://mattmik.com/files/chip8/mastering/chip8.html
  
## Tools

* http://johnearnest.github.io/Octo/
  
## ROMs

* https://web.archive.org/web/20130903155600/http://chip8.com/downloads/Chip-8%20Pack.zip
  
## License

MIT
  
## Other

beep.wav is a modified beep-6.wav from the CC-0 "Interface beeps" collection https://opengameart.org/content/interface-beeps
