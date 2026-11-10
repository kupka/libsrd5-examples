using System;
using Godot;
using srd5;

public enum CreateCharacterState {
    DEFAULT,
    CHOSE_RACE,
    CHOSE_CLASS,
    DONE,
    ENTER_NAME
}

public partial class CreateCharacter : Control, IKeyInputReceiver {
    private Label MenuText, CharacterText;
    private Control NameDialog;
    private LineEdit NameInput;
    private BaseButton OKButton, DiceButton;
    private TextureRect CharacterImage;
    private Texture2D Elf, Dwarf, Human;

    private Race selectedRace = Race.HILL_DWARF;
    private CreateCharacterState state = CreateCharacterState.DEFAULT;

    private string[] namePrefix = new string[] { "Bro", "Clo", "Dor", "Fir", "Gum", "Hub", "Nub", "Pil", "Vyr", "Xom" };
    private string[] nameMiddle = new string[] { "f", "g", "e", "gh", "m", "n", "p", "s", "t", "o" };
    private string[] nameMiddle2 = new string[] { "e", "i", "u", "o", "a", "y" };
    private string[] nameSuffix = new string[] { "l", "r", "m", "f", "p", "t" };
    private string randomName {
        get {
            return namePrefix[Die.D10.Value - 1] + nameMiddle[Die.D10.Value - 1] + nameMiddle2[Die.D6.Value - 1] + nameSuffix[Die.D6.Value - 1];
        }
    }
    private const string DEFAULT_TEXT =
@"1. Select Race
2. Re-Roll Abilities
3. Chose Class
0. Back to Menu";
    private const string RACE_SELECTION_TEXT =
@"1. Hill Dwarf
2. High Elf
3. Human
0. Back";
    private const string CLASS_SELECTION_TEXT =
@"1. Barbarian
2. Druid
3. Wizard
0. Back";

    private const string DONE_TEXT =
@"1. Accept Character
0. Back";
    private CharacterSheet hero;

    public override void _Ready() {
        MenuText = GetNode<Label>("MenuText");
        CharacterText = GetNode<Label>("CharacterText");
        CharacterImage = GetNode<TextureRect>("CharacterImage");
        NameDialog = GetNode<Control>("Darken");
        NameInput = GetNode<LineEdit>("Darken/NameInput");
        OKButton = GetNode<BaseButton>("Darken/NameInput/ButtonOK");
        DiceButton = GetNode<BaseButton>("Darken/NameInput/ButtonRandom");
        Elf = GD.Load<Texture2D>(Game.WESNOTH + "elf_berserker.png");
        Dwarf = GD.Load<Texture2D>(Game.WESNOTH + "dwarf_berserker.png");
        Human = GD.Load<Texture2D>(Game.WESNOTH + "human_berserker.png");

        // Unity: InputField.onEndEdit + check for Return
        NameInput.TextSubmitted += (string value) => KeyPressHandler(Key.Enter);

        OKButton.Pressed += () => KeyPressHandler(Key.Enter);

        DiceButton.Pressed += () => {
            NameInput.Text = randomName;
            NameInput.CaretColumn = NameInput.Text.Length;
            NameInput.GrabFocus();
        };

        MenuText.Text = DEFAULT_TEXT;
        reRollAbilities();
    }

    public void KeyPressHandler(Key key) {
        if (state == CreateCharacterState.DEFAULT) {
            if (key == Key.Key0) {
                Game.LoadScene(this, "MainMenu");
            } else if (key == Key.Key1) {
                state = CreateCharacterState.CHOSE_RACE;
                MenuText.Text = RACE_SELECTION_TEXT;
            } else if (key == Key.Key2) {
                reRollAbilities();
            } else if (key == Key.Key3) {
                state = CreateCharacterState.CHOSE_CLASS;
                MenuText.Text = CLASS_SELECTION_TEXT;
            }
        } else if (state == CreateCharacterState.CHOSE_RACE) {
            if (key == Key.Key0) {
                state = CreateCharacterState.DEFAULT;
                MenuText.Text = DEFAULT_TEXT;
            } else if (key == Key.Key1) {
                selectedRace = Race.HILL_DWARF;
                state = CreateCharacterState.DEFAULT;
                MenuText.Text = DEFAULT_TEXT;
                CharacterImage.Texture = Dwarf;
                reRollAbilities();
            } else if (key == Key.Key2) {
                selectedRace = Race.HIGH_ELF;
                state = CreateCharacterState.DEFAULT;
                MenuText.Text = DEFAULT_TEXT;
                CharacterImage.Texture = Elf;
                reRollAbilities();
            } else if (key == Key.Key3) {
                selectedRace = Race.HUMAN;
                state = CreateCharacterState.DEFAULT;
                MenuText.Text = DEFAULT_TEXT;
                CharacterImage.Texture = Human;
                reRollAbilities();
            }
        } else if (state == CreateCharacterState.CHOSE_CLASS) {
            if (key == Key.Key0) {
                state = CreateCharacterState.DEFAULT;
                MenuText.Text = DEFAULT_TEXT;
            } else if (key == Key.Key1) {
                hero.AddLevel(CharacterClasses.Barbarian);
                state = CreateCharacterState.DONE;
                MenuText.Text = DONE_TEXT;
                updateCharacterText();
            } else if (key == Key.Key2) {
                hero.AddLevel(CharacterClasses.Druid);
                state = CreateCharacterState.DONE;
                MenuText.Text = DONE_TEXT;
                updateCharacterText();
            } else if (key == Key.Key3) {
                hero.AddLevel(CharacterClasses.Wizard);
                state = CreateCharacterState.DONE;
                MenuText.Text = DONE_TEXT;
                updateCharacterText();
            }
        } else if (state == CreateCharacterState.DONE) {
            if (key == Key.Key1) {
                NameDialog.Visible = true;
                NameInput.GrabFocus();
                state = CreateCharacterState.ENTER_NAME;
            } else if (key == Key.Key0) {
                state = CreateCharacterState.DEFAULT;
                MenuText.Text = DEFAULT_TEXT;
            }
        } else if (state == CreateCharacterState.ENTER_NAME && key == Key.Enter) {
            string value = NameInput.Text;
            if (value.Length < 3) {
                NameInput.Text = "";
                NameInput.PlaceholderText = "Too short";
            } else if (value.Length > 15) {
                NameInput.Text = "";
                NameInput.PlaceholderText = "Too long";
            } else {
                foreach (CharacterSheet sheet in Game.Characters) {
                    if (sheet.Name == value) {
                        NameInput.Text = "";
                        NameInput.PlaceholderText = "Already taken";
                        NameInput.GrabFocus();
                        return;
                    }
                }
                Game.SetName(hero, value);
                Game.Characters.Add(hero);
                Game.LoadScene(this, "MainMenu");
                return;
            }
            // Unity selected the OK button here; keeping focus in the text field is friendlier.
            NameInput.GrabFocus();
        }
    }

    private void reRollAbilities() {
        hero = new CharacterSheet(selectedRace, true);
        updateCharacterText();
    }

    private void updateCharacterText() {
        CharacterText.Text = String.Format("STR: {0,2} ({1,2})   DEX: {2,2} ({3,2})\nCON: {4,2} ({5,2})   CHA: {6,2} ({7,2})\nINT: {8,2} ({9,2})   WIS: {10,2} ({11,2})\n\n{12}\n",
            hero.Strength.Value, hero.Strength.Modifier.ToString("+#;-#;0"), hero.Dexterity.Value, hero.Dexterity.Modifier.ToString("+#;-#;0"),
            hero.Constitution.Value, hero.Constitution.Modifier.ToString("+#;-#;0"), hero.Charisma.Value, hero.Charisma.Modifier.ToString("+#;-#;0"),
            hero.Intelligence.Value, hero.Intelligence.Modifier.ToString("+#;-#;0"), hero.Wisdom.Value, hero.Wisdom.Modifier.ToString("+#;-#;0"),
            selectedRace.Description());
        foreach (Feat feat in hero.Feats) {
            CharacterText.Text += String.Format("\n{0}", feat.Description());
        }
    }
}
