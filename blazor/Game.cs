using System.Collections.Generic;
using System.Globalization;
using srd5;

namespace Libsrd5Blazor {

    /// <summary>
    /// Global game state shared between screens. Static fields survive screen changes,
    /// just like Unity statics survived SceneManager.LoadScene.
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

        public const string SPRITES = "assets/sprites/";
        public const string WESNOTH = "assets/sprites/wesnoth/";

        /// <summary>Name of the screen App.razor shows (the Unity scene name).</summary>
        public static string CurrentScene = "MainMenu";
        public static event System.Action SceneChanged;

        /// <summary>The active screen; receives number keys from App.razor.</summary>
        public static IKeyInputReceiver KeyReceiver;

        public static void StringPad(ref string s, int length) {
            if (s.Length > length)
                s = s.Substring(0, length - 3) + "...";
            else
                s += new string(' ', length - s.Length);
        }

        /// <summary>Replacement for Unity's SceneManager.LoadScene(name).</summary>
        public static void LoadScene(string sceneName) {
            CurrentScene = sceneName;
            SceneChanged?.Invoke();
        }

        /// <summary>Paths of the idle/attack/defend sprites of a Wesnoth unit.</summary>
        public static string[] UnitSprites(string unit) {
            return new string[] {
                WESNOTH + unit + ".png",
                WESNOTH + unit + "_attack.png",
                WESNOTH + unit + "_defend.png"
            };
        }
    }

    /// <summary>Mirrors the Unity key codes the demo uses, so the game logic stays unchanged.</summary>
    public enum KeyCode {
        Return = 13,
        Alpha0 = 48, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9
    }

    public interface IKeyInputReceiver {
        void KeyPressHandler(KeyCode code);
    }

    /// <summary>
    /// Converts Unity UI rects (centre anchored, y up, size) on the 1280x720 canvas to CSS.
    /// </summary>
    public static class Layout {
        public static string Rect(double x, double y, double w, double h) {
            return string.Format(CultureInfo.InvariantCulture, "left:{0}px;top:{1}px;width:{2}px;height:{3}px;",
                640 + x - w / 2, 360 - y - h / 2, w, h);
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

    /// <summary>A unit image on the combat screen (replaces a Unity Image GameObject).</summary>
    public class Slot {
        public double X, Y, W, H;
        public bool Visible;
        public string Sprite;
        /// <summary>Conditions whose icon is shown on top of the unit.</summary>
        public HashSet<ConditionType> Icons = new HashSet<ConditionType>();

        public Slot(double x, double y, double w, double h) {
            X = x; Y = y; W = w; H = h;
        }
    }

    public struct CombattantData {
        public Combattant Combattant;
        public Slot Image;
        public string[] Sprites;

        public CombattantData(Combattant combattant, Slot image, string[] sprites) {
            Combattant = combattant;
            Image = image;
            Sprites = sprites;
        }
    }
}
