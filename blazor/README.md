# libsrd5-examples-blazor

A very simple example of how to integrate [libsrd5](https://github.com/kupka/libsrd5) in a Blazor WebAssembly app.
It is a port of the Unity3D example in `../unity` with the same four screens and game flow, running entirely in
the browser.

> **Disclaimer:** this version has been created using Claude Code and has not been thoroughly reviewed.

## Requirements

* .NET SDK 8.0 or newer

## Running

```
cd blazor
dotnet run
```

Then open the URL printed in the console. To build the static site: `dotnet publish -c Release -o publish` and serve
`publish/wwwroot` from any web server (the app uses a relative base URL, so any folder works; GitHub Pages also needs
a `.nojekyll` file, which the CI build adds).

## Unity → Blazor mapping

| Unity                                  | Blazor                                                          |
|----------------------------------------|-----------------------------------------------------------------|
| Scene (`Assets/Scenes/*.unity`)        | Razor component in `Screens/`, switched by `App.razor`          |
| `MonoBehaviour.Start`                  | `ScreenBase.Start` (called from `OnInitialized`)                |
| `Update` with `Time.deltaTime`         | async loop with `Task.Delay` (combat screen only)               |
| `SceneManager.LoadScene`               | `Game.LoadScene` → `App.razor` re-renders                       |
| `UI.Text` / rich text `<color>`        | `<div>` with `white-space: pre-wrap` / `<span style="color">`   |
| `UI.Image` + `Sprite`                  | `<img>` with the sprite path                                    |
| `InputField`                           | `<input>` with `@bind`                                          |
| `Input.GetKeyUp(KeyCode.AlphaN)`       | JS `keyup` listener in `index.html` → `App.OnKey`               |
| Canvas, 1280×720, constant pixel size  | 1280×720 `#stage`, scaled to the window with CSS                |
| `ConditionIconBehavior` (pulsing icon) | CSS animation                                                   |

The screen code (`Screens/*.razor.cs`) keeps the game logic of the Unity scripts line for line, including the
`KeyCode` values (see `Game.cs`).

libsrd5 is referenced as the same prebuilt DLL the Unity project ships (`lib/libsrd5.dll`, version 0.1.15).

## License

Same as the Unity example: AGPL-3.0 for the code, Share Tech Mono under the OFL, Golden UI elements by Buch,
Wesnoth graphics under GPLv2 / CC-BY-SA (see `../unity/README.md`).
