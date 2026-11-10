using System.Collections.Generic;
using Godot;
using srd5;

/// <summary>
/// Global game state shared between scenes. Static fields survive
/// SceneTree.ChangeSceneToFile, just like Unity statics survived SceneManager.LoadScene.
/// </summary>
public static class Game {
    public enum CombatScene {
        BOARS,
        ORCS,
        OGRES
    }
    public static int SelectedCharacter = -1;
    public static List<CharacterSheet> Characters = new List<CharacterSheet>();

    public static int FrontLineChar1 = 0, FrontLineChar2 = 1;
    public static CombatScene Scene = CombatScene.ORCS;

    public const string SPRITES = "res://assets/sprites/";
    public const string WESNOTH = "res://assets/sprites/wesnoth/";

    /// <summary>
    /// libsrd5 0.3 made Name read-only for apps (internal setter). The example sets a hero's
    /// name through reflection until libsrd5 offers a public way.
    /// </summary>
    public static void SetName(CharacterSheet hero, string name) {
        System.Reflection.PropertyInfo property = typeof(GuidClass).GetProperty("Name",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        property.GetSetMethod(true).Invoke(hero, new object[] { name });
    }

    public static void StringPad(ref string s, int length) {
        if (s.Length > length)
            s = s.Substring(0, length - 3) + "...";
        else
            s += new string(' ', length - s.Length);
    }

    /// <summary>Replacement for Unity's SceneManager.LoadScene(name).</summary>
    public static void LoadScene(Node from, string sceneName) {
        from.GetTree().ChangeSceneToFile("res://scenes/" + sceneName + ".tscn");
    }

    /// <summary>Digit value of a number key (Key.Key0 .. Key.Key9).</summary>
    public static int Digit(Key key) {
        return (int)(key - Key.Key0);
    }

    /// <summary>Loads the idle/attack/defend sprite triple of a Wesnoth unit.</summary>
    public static Texture2D[] LoadUnitSprites(string unit) {
        return new Texture2D[] {
            GD.Load<Texture2D>(WESNOTH + unit + ".png"),
            GD.Load<Texture2D>(WESNOTH + unit + "_attack.png"),
            GD.Load<Texture2D>(WESNOTH + unit + "_defend.png")
        };
    }
}

public class FixedSizedQueue<T> {
    Queue<T> q = new Queue<T>();
    private object lockObject = new object();

    private int limit;

    public FixedSizedQueue(int limit) {
        this.limit = limit;
    }

    public void Enqueue(T obj) {
        q.Enqueue(obj);
        lock (lockObject) {
            while (q.Count > limit) q.Dequeue();
        }
    }

    public T[] ToArray() {
        return q.ToArray();
    }
}

public struct CombatantData {
    public Combatant Combatant;
    public TextureRect Image;
    public Texture2D[] Sprites;
    public Dictionary<ConditionType, Control> ConditionIcons;

    public CombatantData(Combatant combattant, TextureRect image, Texture2D[] sprites) {
        Combatant = combattant;
        Image = image;
        Sprites = sprites;
        ConditionIcons = new Dictionary<ConditionType, Control>();
    }
}
