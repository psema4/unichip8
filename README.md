# UniCHIP8

A CHIP-8 implementation for Unity 3D based on Laurence Muller's tutorial, "[How to write an emulator (CHIP-8 interpreter)](http://www.multigesture.net/articles/how-to-write-an-emulator-chip-8-interpreter/)"

<img src="https://github.com/psema4/unichip8/raw/master/Assets/UniCHIP8/unichip8-v1.png" width="640" />

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

## UniCHIP8 Extensions

UniCHIP8 offers a number of custom opcodes that enable integration with your Unity 3D scenes through the use of a simple message router.

To enable UniCHIP8 Extensions:

* Disable the Compatibility Mode property on your UniCHIP8 instance
* Ensure your scene has a UniCHIP Router in it.  If not, add one from the UniCHIP8 prefabs folder and assign it to the Router property on your UniCHIP8 instance

See the [UniCHIP8 wiki](https://github.com/psema4/unichip8/wiki) for more information.
   
## License

MIT
  
## Other

The default UniCHIP8 system beep sound is modified from beep-6 of the CC-0 ["Interface beeps" collection](https://opengameart.org/content/interface-beeps)
