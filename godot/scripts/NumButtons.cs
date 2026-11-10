using Godot;

/// <summary>
/// Port of NumButtonBehavior: forwards clicks on the child buttons "Button0".."Button9"
/// and releases of the number keys 0-9 to the receiver (the screen's root node).
/// </summary>
public partial class NumButtons : Control {
    [Export] public NodePath Receiver = "..";

    private IKeyInputReceiver receiver;

    public override void _Ready() {
        receiver = GetNodeOrNull(Receiver) as IKeyInputReceiver;
        if (receiver == null) {
            GD.PushError("NumButtons: receiver does not implement IKeyInputReceiver");
            return;
        }
        foreach (Node child in GetChildren()) {
            if (!(child is BaseButton button)) continue;
            string name = button.Name.ToString();
            if (!name.StartsWith("Button") || !int.TryParse(name.Substring("Button".Length), out int digit)) continue;
            Key code = Key.Key0 + digit;
            button.Pressed += () => receiver.KeyPressHandler(code);
        }
    }

    // Unhandled input: keys typed into a focused LineEdit never reach this handler.
    public override void _UnhandledKeyInput(InputEvent @event) {
        if (receiver == null) return;
        if (@event is InputEventKey key && !key.Pressed && !key.Echo
            && key.Keycode >= Key.Key0 && key.Keycode <= Key.Key9) {
            GetViewport().SetInputAsHandled();
            receiver.KeyPressHandler(key.Keycode);
        }
    }
}
