# libsrd5-examples
Examples of libsrd5 usage in Unity3D, Godot and Blazor/WASM.

All three versions implement the same small demo game: create a party of characters, equip them and fight
boars, orcs or ogres. The game rules come from [libsrd5](https://github.com/kupka/libsrd5).

# Unity3D

Live Demo at (https://kupka.github.io/libsrd5-examples/unity/index.html)

See [unity/README.md](unity/README.md).

# Godot

Godot 4 (.NET / C#) port of the Unity3D demo in the `godot` folder. Builds for Windows, Linux and Android.
See [godot/README.md](godot/README.md).

# Blazor

Blazor WebAssembly port of the Unity3D demo in the `blazor` folder. Runs in the browser as a static web app.

Live Demo at (https://kupka.github.io/libsrd5-examples/blazor/)
See [blazor/README.md](blazor/README.md).

# Builds

Every pull request builds the Godot (Windows, Linux, Android) and Blazor versions; the builds are attached to the
workflow run as artifacts. When changes are merged into `main`, the same builds are published as a
[GitHub release](https://github.com/kupka/libsrd5-examples/releases) and the Blazor live demo on GitHub Pages
(`docs/blazor`) is updated.

# Disclaimer

The Godot and Blazor versions have been created using [Claude Code](https://claude.com/claude-code) and have
not been thoroughly reviewed. Expect rough edges; issues and pull requests are welcome.
