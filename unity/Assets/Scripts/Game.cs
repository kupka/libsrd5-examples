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

public struct CombattantData {
    public Combattant Combattant;
    public Image Image;
    public Sprite[] Sprites;

    public CombattantData(Combattant combattant, Image image, Sprite[] sprites) {
        Combattant = combattant;
        Image = image;
        Sprites = sprites;
    }
}