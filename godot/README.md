# libsrd5-examples-godot

A very simple example of how to integrate [libsrd5](https://github.com/kupka/libsrd5) in a Godot 4 (.NET / C#) project.
It is a port of the Unity3D example in `../unity` with the same four screens and game flow.

## Requirements

* Godot 4.6+ **.NET edition** (the standard build has no C# support)
* .NET SDK 8.0 or newer

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

libsrd5 is referenced as the same prebuilt DLL the Unity project ships (`lib/libsrd5.dll`, version 0.1.15).
Newer libsrd5 versions changed parts of the API (e.g. `Combattant` → `Combatant`, `Item.IsThisA`), so
upgrading needs small code changes.

## Web export

Godot 4 cannot export C# projects to the web yet (support is being worked on upstream). Desktop and mobile
exports work. Until then, the WebGL live demo stays on the Unity build.

## License

Same as the Unity example: AGPL-3.0 for the code, Share Tech Mono under the OFL, Golden UI elements by Buch,
Wesnoth graphics under GPLv2 / CC-BY-SA (see `../unity/README.md`).
