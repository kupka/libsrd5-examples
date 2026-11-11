# libsrd5-examples-godot

A very simple example of how to integrate [libsrd5](https://github.com/kupka/libsrd5) in a Godot 4 (.NET / C#) project.
It is a port of the Unity3D example in `../unity` with the same four screens and game flow.

> **Disclaimer:** this version has been created using Claude Code and has not been thoroughly reviewed.

## Requirements

* Godot 4.6+ **.NET edition** (the standard build has no C# support)
* .NET SDK 8.0 or newer (.NET 9 for Android exports)

## Running

1. Open `godot/project.godot` in the Godot .NET editor.
2. Let it import the assets, then press **Build** (or Alt+B) and **Run** (F5).

Or from the command line: `dotnet build` in this folder, then `godot --path .`.

## Unity → Godot mapping

| Unity                                   | Godot                                                        |
|-----------------------------------------|--------------------------------------------------------------|
| Scene (`Assets/Scenes/*.unity`)         | Scene (`scenes/*.tscn`), root `Control` node with the screen script |
| `MonoBehaviour.Start/Update/OnDestroy`  | `_Ready` / `_Process` / `_ExitTree`                          |
| `SceneManager.LoadScene`                | `GetTree().ChangeSceneToFile` (`Game.LoadScene`)             |
| `UI.Text` / rich text `<color>`         | `Label` / `RichTextLabel` with BBCode `[color]`              |
| `UI.Image` + `Sprite`                   | `TextureRect` + `Texture2D`                                  |
| `InputField`                            | `LineEdit`                                                   |
| `KeyCode.AlphaN`, `Input.GetKeyUp`      | `Key.KeyN`, `_UnhandledKeyInput` (key release)               |
| Inspector references to sprites/images  | `GetNode<T>("Name")` and `GD.Load<Texture2D>(...)` in `_Ready` |
| `Instantiate` + `SetParent`             | `Duplicate()` + `AddChild`                                   |
| Canvas, 1280×720, constant pixel size   | Viewport 1280×720, stretch mode `canvas_items`               |

`MainMenu.cs`, `CreateCharacter.cs`, `ModifyCharacter.cs` and `Combat.cs` keep the game logic of the Unity scripts
line for line; only engine calls changed.

libsrd5 is referenced as a prebuilt DLL (`lib/libsrd5.dll`, currently 0.3.4), the same one the Unity project uses.
It is built by CI from the libsrd5 version pinned in `../libsrd5.ref` (see the root README).

## Builds

CI exports Windows and Linux (x86_64) release builds and a debug-signed Android APK (arm64). They are attached to
every pull request run and published as a GitHub release when merged into `main`. The Android APK targets .NET 9
(Godot's Android export templates require it); desktop builds use .NET 8.

Godot 4 cannot export C# projects to the web yet (support is being worked on upstream); use the Blazor version
(`../blazor`) or the Unity WebGL build for the browser.

## License

Same as the Unity example: AGPL-3.0 for the code, Share Tech Mono under the OFL, Golden UI elements by Buch,
Wesnoth graphics under GPLv2 / CC-BY-SA (see `../unity/README.md`).
