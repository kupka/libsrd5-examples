using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using srd5;

namespace Libsrd5Blazor.Screens {

public enum CreateCharacterState {
    DEFAULT,
    CHOSE_RACE,
    CHOSE_CLASS,
    DONE,
    ENTER_NAME
}
public partial class CharacterCreation {
    private string MenuText = "", CharacterText = "";

    // Name dialog (Unity: InputField inside the "Darken" panel)
    private bool nameDialogVisible = false, focusNameInput = false;
    private string nameValue = "", namePlaceholder = "Enter name...";
    private ElementReference nameInputElement;

    private string CharacterImage = Game.WESNOTH + "dwarf_berserker.png";

    private const string Elf = Game.WESNOTH + "elf_berserker.png", Dwarf = Game.WESNOTH + "dwarf_berserker.png",
        Human = Game.WESNOTH + "human_berserker.png";

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

    protected override void Start() {
        MenuText = DEFAULT_TEXT;
        reRollAbilities();
    }

    protected override void OnKey(KeyCode key) {
        if (state == CreateCharacterState.DEFAULT) {
            if (key == KeyCode.Alpha0) {
                LoadScene("MainMenu");
            } else if (key == KeyCode.Alpha1) {
                state = CreateCharacterState.CHOSE_RACE;
                MenuText = RACE_SELECTION_TEXT;
            } else if (key == KeyCode.Alpha2) {
                reRollAbilities();
            } else if (key == KeyCode.Alpha3) {
                state = CreateCharacterState.CHOSE_CLASS;
                MenuText = CLASS_SELECTION_TEXT;
            }
        } else if (state == CreateCharacterState.CHOSE_RACE) {
            if (key == KeyCode.Alpha0) {
                state = CreateCharacterState.DEFAULT;
                MenuText = DEFAULT_TEXT;
            } else if (key == KeyCode.Alpha1) {
                selectedRace = Race.HILL_DWARF;
                state = CreateCharacterState.DEFAULT;
                MenuText = DEFAULT_TEXT;
                CharacterImage = Dwarf;
                reRollAbilities();
            } else if (key == KeyCode.Alpha2) {
                selectedRace = Race.HIGH_ELF;
                state = CreateCharacterState.DEFAULT;
                MenuText = DEFAULT_TEXT;
                CharacterImage = Elf;
                reRollAbilities();
            } else if (key == KeyCode.Alpha3) {
                selectedRace = Race.HUMAN;
                state = CreateCharacterState.DEFAULT;
                MenuText = DEFAULT_TEXT;
                CharacterImage = Human;
                reRollAbilities();
            }
        } else if (state == CreateCharacterState.CHOSE_CLASS) {
            if (key == KeyCode.Alpha0) {
                state = CreateCharacterState.DEFAULT;
                MenuText = DEFAULT_TEXT;
            } else if (key == KeyCode.Alpha1) {
                hero.AddLevel(CharacterClasses.Barbarian);
                state = CreateCharacterState.DONE;
                MenuText = DONE_TEXT;
                updateCharacterText();
            } else if (key == KeyCode.Alpha2) {
                hero.AddLevel(CharacterClasses.Druid);
                state = CreateCharacterState.DONE;
                MenuText = DONE_TEXT;
                updateCharacterText();
            } else if (key == KeyCode.Alpha3) {
                hero.AddLevel(CharacterClasses.Wizard);
                state = CreateCharacterState.DONE;
                MenuText = DONE_TEXT;
                updateCharacterText();
            }
        } else if (state == CreateCharacterState.DONE) {
            if (key == KeyCode.Alpha1) {
                nameDialogVisible = true;
                focusNameInput = true;
                state = CreateCharacterState.ENTER_NAME;
            } else if (key == KeyCode.Alpha0) {
                state = CreateCharacterState.DEFAULT;
                MenuText = DEFAULT_TEXT;
                updateCharacterText();
            }
        } else if (state == CreateCharacterState.ENTER_NAME && key == KeyCode.Return) {
            string value = nameValue;
            if (value.Length < 3) {
                nameValue = "";
                namePlaceholder = "Too short";
            } else if (value.Length > 15) {
                nameValue = "";
                namePlaceholder = "Too long";
            } else {
                foreach (CharacterSheet sheet in Game.Characters) {
                    if (sheet.Name == value) {
                        nameValue = "";
                        namePlaceholder = "Already taken";
                        return;
                    }
                }
                Game.SetName(hero, value);
                Game.Characters.Add(hero);
                LoadScene("MainMenu");
            }
            focusNameInput = true;
        }
    }

    private void reRollAbilities() {
        hero = new CharacterSheet(selectedRace, true);
        updateCharacterText();
    }

    private void updateCharacterText() {
        // Race description until a class is chosen, then only the class description
        string description = state == CreateCharacterState.DONE ? Texts.ClassDescription(hero) : Texts.RaceDescription(hero);
        CharacterText = String.Format("STR: {0,2} ({1,2})   DEX: {2,2} ({3,2})\nCON: {4,2} ({5,2})   CHA: {6,2} ({7,2})\nINT: {8,2} ({9,2})   WIS: {10,2} ({11,2})\n\n{12}\n",
            hero.Strength.Value, hero.Strength.Modifier.ToString("+#;-#;0"), hero.Dexterity.Value, hero.Dexterity.Modifier.ToString("+#;-#;0"),
            hero.Constitution.Value, hero.Constitution.Modifier.ToString("+#;-#;0"), hero.Charisma.Value, hero.Charisma.Modifier.ToString("+#;-#;0"),
            hero.Intelligence.Value, hero.Intelligence.Modifier.ToString("+#;-#;0"), hero.Wisdom.Value, hero.Wisdom.Modifier.ToString("+#;-#;0"),
            description);
    }
    // Unity: InputField.onEndEdit with Return, OK button and dice button listeners
    private void NameKeyUp(KeyboardEventArgs e) {
        if (e.Key == "Enter") KeyPressHandler(KeyCode.Return);
    }

    private void RollName() {
        nameValue = randomName;
        focusNameInput = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (focusNameInput && nameDialogVisible) {
            focusNameInput = false;
            await nameInputElement.FocusAsync();
        }
    }
}
}
