using Godot;

/// <summary>
/// Implemented by every screen. Receives the number keys 0-9 (from the keyboard or the
/// on-screen buttons) and Key.Enter (name input on the character creation screen).
/// </summary>
public interface IKeyInputReceiver {
    void KeyPressHandler(Key code);
}
