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
