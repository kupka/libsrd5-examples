using System.Collections.Generic;
using srd5;
using UnityEngine;
using UnityEngine.UI;

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

    /// <summary>
    /// libsrd5 0.3 made Name read-only for apps (internal setter). The example sets a hero's
    /// name through reflection until libsrd5 offers a public way.
    /// </summary>
    public static void SetName(CharacterSheet hero, string name) {
        System.Reflection.PropertyInfo property = typeof(GuidClass).GetProperty("Name",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        property.GetSetMethod(true).Invoke(hero, new object[] { name });
    }

    /// <summary>The heroes of the current mode: the adventure party, or the free-mode characters.</summary>
    public static List<CharacterSheet> Party {
        get { return Adventure.Current != null ? Adventure.Current.Party : Characters; }
    }

    /// <summary>True if the hero at this index fights in the front line.</summary>
    public static bool IsFrontLine(int index) {
        if (Adventure.Current != null) return Adventure.Current.IsFront(index);
        return index == FrontLineChar1 || index == FrontLineChar2;
    }

    /// <summary>The enemies of the next fight: the adventure's fight, or two of the selected kind.</summary>
    public static Monster[] CreateEnemies() {
        if (Adventure.Current != null && Adventure.Current.InFight) return Adventure.Current.CreateEnemies();
        switch (Scene) {
            case CombatScene.BOARS: return new Monster[] { Monsters.Boar, Monsters.Boar };
            case CombatScene.OGRES: return new Monster[] { Monsters.Ogre, Monsters.Ogre };
            default: return new Monster[] { Monsters.Orc, Monsters.Orc };
        }
    }

    public static void StringPad(ref string s, int length) {
        if (s.Length > length)
            s = s.Substring(0, length - 3) + "...";
        else
            s += new string(' ', length - s.Length);
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
    public Image Image;
    public Sprite[] Sprites;
    public Dictionary<ConditionType, Image> ConditionIcons;

    public CombatantData(Combatant combattant, Image image, Sprite[] sprites) {
        Combatant = combattant;
        Image = image;
        Sprites = sprites;
        ConditionIcons = new Dictionary<ConditionType, Image>();
    }
}