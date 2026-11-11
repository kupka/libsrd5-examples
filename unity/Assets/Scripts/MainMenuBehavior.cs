using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using srd5;
using System;
public class MainMenuBehavior : MonoBehaviour, KeyInputReceiver {
    enum MainMenuState {
        DEFAULT,
        DELETE_CHARACTER,
        PREPARE_CHARACTER,
        ARRANGE_CHARACTERS_1,
        ARRANGE_CHARACTERS_2,
        RETURN_ONLY,
        BATTLE,
        LIBRARY
    }
    public Text MenuText, MainText;

    private MainMenuState state = MainMenuState.DEFAULT;
    private Library library;
    // Main menu art (and logo), replaced by a category icon while the library is open
    private GameObject dwarf, ogre, logo;
    private Image libraryIcon;

    private const string DEFAULT_TEXT =
@"Remember to equip ranged weapons to characters in the back and prepare spells for casters.

1. Create a Character
2. Delete a Character
3. Equip / Prepare a Chracter
4. Arrange Characters
5. Start a battle
6. Library";

    private const string DELETE_TEXT =
@"Input number of character to delete or 0 to cancel.";

    private const string PREPARE_TEXT =
@"Input number of character to prepare or 0 to cancel.";

    private const string BATTLE_TEXT =
@"Make sure you equipped your heroes properly before engaging the enemy!
1. Boars (easy)
2. Orcs (medium)
3. Ogres (hard)
0. Return";

    private const string TOO_MANY_CHARACTERS =
@"You cannot create more than 2 characters.
0. Return";

    private const string NOT_ENOUGH_CHARACTERS =
    @"You need more than two characters to arrange them.
0. Return";

    private const string ARRANGE_CHARACTERS_1 = "Set the first character for the front line. Characters in the back can only attack with ranged attacks.";
    private const string ARRANGE_CHARACTERS_2 = "Set the second character for the front line. Characters in the back can only attack with ranged attacks.";

    void Start() {
        dwarf = GameObject.Find("Dwarf");
        ogre = GameObject.Find("Ogre");
        logo = GameObject.Find("Logo");
        createLibraryIcon();
        MenuText.text = DEFAULT_TEXT;
        updateCharacters();
        srd5.Random.State = (uint)DateTime.Now.ToFileTime();
    }

    public void KeyPressHandler(KeyCode code) {
        if (state == MainMenuState.DEFAULT) {
            if (code == KeyCode.Alpha1) {
                if (Game.Characters.Count >= 4) {
                    MenuText.text = TOO_MANY_CHARACTERS;
                    state = MainMenuState.RETURN_ONLY;
                } else {
                    SceneManager.LoadScene("CharacterCreation");
                }
            } else if (code == KeyCode.Alpha2) {
                MenuText.text = DELETE_TEXT;
                state = MainMenuState.DELETE_CHARACTER;
            } else if (code == KeyCode.Alpha3) {
                MenuText.text = PREPARE_TEXT;
                state = MainMenuState.PREPARE_CHARACTER;
            } else if (code == KeyCode.Alpha4) {
                if (Game.Characters.Count < 3) {
                    MenuText.text = NOT_ENOUGH_CHARACTERS;
                    state = MainMenuState.RETURN_ONLY;
                } else {
                    MenuText.text = ARRANGE_CHARACTERS_1;
                    state = MainMenuState.ARRANGE_CHARACTERS_1;
                }
            } else if (code == KeyCode.Alpha5) {
                MenuText.text = BATTLE_TEXT;
                state = MainMenuState.BATTLE;
            } else if (code == KeyCode.Alpha6) {
                library = new Library();
                state = MainMenuState.LIBRARY;
                MenuText.text = library.MenuText;
                MainText.text = library.MainText;
                showLibraryIcon(true);
            }
        } else if (state == MainMenuState.DELETE_CHARACTER) {
            if (code > KeyCode.Alpha0) {
                try {
                    Game.Characters.RemoveAt(code - KeyCode.Alpha1);
                } catch (ArgumentOutOfRangeException) {
                    // don't care
                }
                Game.FrontLineChar1 = 0;
                Game.FrontLineChar2 = 1;
                updateCharacters();
                state = MainMenuState.DEFAULT;
                MenuText.text = DEFAULT_TEXT;
            } else {
                state = MainMenuState.DEFAULT;
                MenuText.text = DEFAULT_TEXT;
            }
        } else if (state == MainMenuState.PREPARE_CHARACTER) {
            if (code > KeyCode.Alpha0) {
                int selected = code - KeyCode.Alpha1;
                try {
                    CharacterSheet hero = Game.Characters[selected];
                } catch (ArgumentOutOfRangeException) {
                    return;
                }
                Game.SelectedCharacter = selected;
                SceneManager.LoadScene("CharacterPrepare");
            } else {
                state = MainMenuState.DEFAULT;
                MenuText.text = DEFAULT_TEXT;
            }
        } else if (state == MainMenuState.RETURN_ONLY) {
            if (code == KeyCode.Alpha0) {
                state = MainMenuState.DEFAULT;
                MenuText.text = DEFAULT_TEXT;
            }
        } else if (state == MainMenuState.ARRANGE_CHARACTERS_1) {
            int selected = code - KeyCode.Alpha1;
            try {
                CharacterSheet hero = Game.Characters[selected];
            } catch (ArgumentOutOfRangeException) {
                return;
            }
            Game.FrontLineChar1 = selected;
            state = MainMenuState.ARRANGE_CHARACTERS_2;
            MenuText.text = ARRANGE_CHARACTERS_2;
            updateCharacters();
        } else if (state == MainMenuState.ARRANGE_CHARACTERS_2) {
            int selected = code - KeyCode.Alpha1;
            try {
                CharacterSheet hero = Game.Characters[selected];
            } catch (ArgumentOutOfRangeException) {
                return;
            }
            if (Game.FrontLineChar1 == selected) return;
            Game.FrontLineChar2 = selected;
            state = MainMenuState.DEFAULT;
            MenuText.text = DEFAULT_TEXT;
            updateCharacters();
        } else if (state == MainMenuState.BATTLE) {
            if (code == KeyCode.Alpha0) {
                state = MainMenuState.DEFAULT;
                MenuText.text = DEFAULT_TEXT;
            } else if (code == KeyCode.Alpha1) {
                Game.Scene = Game.CombatScene.BOARS;
                SceneManager.LoadScene("Combat");
            } else if (code == KeyCode.Alpha2) {
                Game.Scene = Game.CombatScene.ORCS;
                SceneManager.LoadScene("Combat");
            } else if (code == KeyCode.Alpha3) {
                Game.Scene = Game.CombatScene.OGRES;
                SceneManager.LoadScene("Combat");
            }
        } else if (state == MainMenuState.LIBRARY) {
            if (library.HandleKey(code - KeyCode.Alpha0)) {
                MenuText.text = library.MenuText;
                MainText.text = library.MainText;
                showLibraryIcon(true);
            } else {
                showLibraryIcon(false);
                library = null;
                state = MainMenuState.DEFAULT;
                MenuText.text = DEFAULT_TEXT;
                updateCharacters();
            }
        }
    }

    void Update() {

    }

    private void createLibraryIcon() {
        GameObject icon = new GameObject("LibraryIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        icon.transform.SetParent(dwarf.transform.parent, false);
        RectTransform rect = (RectTransform)icon.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(160, 160);
        rect.anchoredPosition = new Vector2(-322, 210);
        libraryIcon = icon.GetComponent<Image>();
        libraryIcon.raycastTarget = false;
        icon.SetActive(false);
    }

    private void showLibraryIcon(bool visible) {
        dwarf.SetActive(!visible);
        ogre.SetActive(!visible);
        logo.SetActive(!visible);
        libraryIcon.gameObject.SetActive(visible);
        if (visible) libraryIcon.sprite = Resources.Load<Sprite>("LibraryIcons/library_" + library.Icon);
    }

    private void updateCharacters() {
        MainText.text = "";
        int i = 1;
        foreach (CharacterSheet sheet in Game.Characters) {
            string frontline = "(back)";
            if (i - 1 == Game.FrontLineChar1 || i - 1 == Game.FrontLineChar2) frontline = "(front)";
            MainText.text += string.Format("{0}. {1} the {2} {3} {4}\n", i++, sheet.Name, Texts.RaceName(sheet.Race.Race), Texts.ClassName(sheet.Levels[0].Class.Class), frontline);
        }
    }
}