using System;
using Godot;
using srd5;

public partial class MainMenu : Control, IKeyInputReceiver {
    enum MainMenuState {
        DEFAULT,
        DELETE_CHARACTER,
        PREPARE_CHARACTER,
        ARRANGE_CHARACTERS_1,
        ARRANGE_CHARACTERS_2,
        RETURN_ONLY,
        BATTLE,
        LIBRARY,
        ADVENTURE
    }
    private Label MenuText, MainText;

    private MainMenuState state = MainMenuState.DEFAULT;
    private Library library;
    // Main menu art (and logo), replaced by a category icon while the library is open
    private Control dwarf, ogre, logo;
    private TextureRect libraryIcon;

    private const string DEFAULT_TEXT =
@"Remember to equip ranged weapons to characters in the back and prepare spells for casters.

1. Create a Character
2. Delete a Character
3. Equip / Prepare a Chracter
4. Arrange Characters
5. Start a battle
6. Library
7. Adventure";

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

    public override void _Ready() {
        MenuText = GetNode<Label>("MenuText");
        MainText = GetNode<Label>("MainText");
        dwarf = GetNode<Control>("Dwarf");
        ogre = GetNode<Control>("Ogre");
        logo = GetNode<Control>("Logo");
        createLibraryIcon();
        GetNode<ConditionIconBehavior>("IconPulse").Icons.Add(GetNode<Control>("Logo"));

        MenuText.Text = DEFAULT_TEXT;
        updateCharacters();
        srd5.Random.State = (uint)DateTime.Now.ToFileTime();
        if (Adventure.Current != null) enterAdventure();
    }

    public void KeyPressHandler(Key code) {
        if (state == MainMenuState.DEFAULT) {
            if (code == Key.Key1) {
                if (Game.Characters.Count >= 4) {
                    MenuText.Text = TOO_MANY_CHARACTERS;
                    state = MainMenuState.RETURN_ONLY;
                } else {
                    Game.LoadScene(this, "CharacterCreation");
                }
            } else if (code == Key.Key2) {
                MenuText.Text = DELETE_TEXT;
                state = MainMenuState.DELETE_CHARACTER;
            } else if (code == Key.Key3) {
                MenuText.Text = PREPARE_TEXT;
                state = MainMenuState.PREPARE_CHARACTER;
            } else if (code == Key.Key4) {
                if (Game.Characters.Count < 3) {
                    MenuText.Text = NOT_ENOUGH_CHARACTERS;
                    state = MainMenuState.RETURN_ONLY;
                } else {
                    MenuText.Text = ARRANGE_CHARACTERS_1;
                    state = MainMenuState.ARRANGE_CHARACTERS_1;
                }
            } else if (code == Key.Key5) {
                MenuText.Text = BATTLE_TEXT;
                state = MainMenuState.BATTLE;
            } else if (code == Key.Key7) {
                Adventure.Current = new Adventure();
                enterAdventure();
            } else if (code == Key.Key6) {
                library = new Library();
                state = MainMenuState.LIBRARY;
                MenuText.Text = library.MenuText;
                MainText.Text = library.MainText;
                showLibraryIcon(true);
            }
        } else if (state == MainMenuState.DELETE_CHARACTER) {
            if (code > Key.Key0) {
                try {
                    Game.Characters.RemoveAt(Game.Digit(code) - 1);
                } catch (ArgumentOutOfRangeException) {
                    // don't care
                }
                Game.FrontLineChar1 = 0;
                Game.FrontLineChar2 = 1;
                updateCharacters();
                state = MainMenuState.DEFAULT;
                MenuText.Text = DEFAULT_TEXT;
            } else {
                state = MainMenuState.DEFAULT;
                MenuText.Text = DEFAULT_TEXT;
            }
        } else if (state == MainMenuState.PREPARE_CHARACTER) {
            if (code > Key.Key0) {
                int selected = Game.Digit(code) - 1;
                if (selected >= Game.Characters.Count) return;
                Game.SelectedCharacter = selected;
                Game.LoadScene(this, "CharacterPrepare");
            } else {
                state = MainMenuState.DEFAULT;
                MenuText.Text = DEFAULT_TEXT;
            }
        } else if (state == MainMenuState.RETURN_ONLY) {
            if (code == Key.Key0) {
                state = MainMenuState.DEFAULT;
                MenuText.Text = DEFAULT_TEXT;
            }
        } else if (state == MainMenuState.ARRANGE_CHARACTERS_1) {
            int selected = Game.Digit(code) - 1;
            if (selected < 0 || selected >= Game.Characters.Count) return;
            Game.FrontLineChar1 = selected;
            state = MainMenuState.ARRANGE_CHARACTERS_2;
            MenuText.Text = ARRANGE_CHARACTERS_2;
            updateCharacters();
        } else if (state == MainMenuState.ARRANGE_CHARACTERS_2) {
            int selected = Game.Digit(code) - 1;
            if (selected < 0 || selected >= Game.Characters.Count) return;
            if (Game.FrontLineChar1 == selected) return;
            Game.FrontLineChar2 = selected;
            state = MainMenuState.DEFAULT;
            MenuText.Text = DEFAULT_TEXT;
            updateCharacters();
        } else if (state == MainMenuState.BATTLE) {
            if (code == Key.Key0) {
                state = MainMenuState.DEFAULT;
                MenuText.Text = DEFAULT_TEXT;
            } else if (code == Key.Key1) {
                Game.Scene = Game.CombatScene.BOARS;
                Game.LoadScene(this, "Combat");
            } else if (code == Key.Key2) {
                Game.Scene = Game.CombatScene.ORCS;
                Game.LoadScene(this, "Combat");
            } else if (code == Key.Key3) {
                Game.Scene = Game.CombatScene.OGRES;
                Game.LoadScene(this, "Combat");
            }
        } else if (state == MainMenuState.ADVENTURE) {
            AdventureAction action = Adventure.Current.HandleKey(Game.Digit(code));
            if (action == AdventureAction.CREATE_CHARACTER) {
                Game.LoadScene(this, "CharacterCreation");
            } else if (action == AdventureAction.START_FIGHT) {
                Game.LoadScene(this, "Combat");
            } else if (action == AdventureAction.LEAVE) {
                state = MainMenuState.DEFAULT;
                MenuText.Text = DEFAULT_TEXT;
                updateCharacters();
                showMenuArt(true);
            } else {
                MenuText.Text = Adventure.Current.MenuText;
                MainText.Text = Adventure.Current.MainText;
            }
        } else if (state == MainMenuState.LIBRARY) {
            if (library.HandleKey(Game.Digit(code))) {
                MenuText.Text = library.MenuText;
                MainText.Text = library.MainText;
                showLibraryIcon(true);
            } else {
                showLibraryIcon(false);
                library = null;
                state = MainMenuState.DEFAULT;
                MenuText.Text = DEFAULT_TEXT;
                updateCharacters();
            }
        }
    }

    private void createLibraryIcon() {
        libraryIcon = new TextureRect();
        libraryIcon.Name = "LibraryIcon";
        libraryIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        libraryIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        libraryIcon.TextureFilter = TextureFilterEnum.Nearest;
        libraryIcon.MouseFilter = MouseFilterEnum.Ignore;
        libraryIcon.Visible = false;
        AddChild(libraryIcon);
        // centre (-322, 210), 160x160 in the Unity layout (y up)
        libraryIcon.SetAnchorsPreset(LayoutPreset.Center);
        libraryIcon.OffsetLeft = -402;
        libraryIcon.OffsetRight = -242;
        libraryIcon.OffsetTop = -290;
        libraryIcon.OffsetBottom = -130;
    }

    private void showMenuArt(bool visible) {
        dwarf.Visible = visible;
        ogre.Visible = visible;
        logo.Visible = visible;
    }

    private void showLibraryIcon(bool visible) {
        showMenuArt(!visible);
        libraryIcon.Visible = visible;
        if (visible) libraryIcon.Texture = GD.Load<Texture2D>("res://assets/sprites/library/library_" + library.Icon + ".png");
    }

    // Adventure mode uses the menu and main text areas; the menu art is hidden meanwhile
    private void enterAdventure() {
        state = MainMenuState.ADVENTURE;
        Adventure.Current.Refresh();
        MenuText.Text = Adventure.Current.MenuText;
        MainText.Text = Adventure.Current.MainText;
        showMenuArt(false);
    }

    private void updateCharacters() {
        MainText.Text = "";
        int i = 1;
        foreach (CharacterSheet sheet in Game.Characters) {
            string frontline = "(back)";
            if (i - 1 == Game.FrontLineChar1 || i - 1 == Game.FrontLineChar2) frontline = "(front)";
            MainText.Text += string.Format("{0}. {1} the {2} {3} {4}\n", i++, sheet.Name, sheet.Race.Name, sheet.Levels[0].Class.Name, frontline);
        }
    }
}
