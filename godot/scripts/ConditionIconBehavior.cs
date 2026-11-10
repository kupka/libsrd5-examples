using System.Collections.Generic;
using Godot;

/// <summary>Pulses (scales up and down) all registered, visible icons.</summary>
public partial class ConditionIconBehavior : Node {
    [Export] public float Slowness = .5f;
    [Export] public float Factor = .5f;
    public List<Control> Icons = new List<Control>();
    private bool scalingDown = true;
    private float scale = 1f;

    public override void _Process(double delta) {
        float diff = (float)delta / Slowness;
        if (scalingDown) {
            scale -= diff * (1f - Factor);
            if (scale < Factor)
                scalingDown = false;
        } else {
            scale += diff * (1f - Factor);
            if (scale > 1f)
                scalingDown = true;
        }
        foreach (Control icon in Icons) {
            if (!IsInstanceValid(icon) || !icon.Visible) continue;
            icon.PivotOffset = icon.Size / 2; // Unity pivot (0.5, 0.5)
            icon.Scale = new Vector2(scale, scale);
        }
    }
}
