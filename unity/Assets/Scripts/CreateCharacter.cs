using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using srd5;

public enum CreateCharacterState {
    DEFAULT,
    CHOSE_RACE,
    CHOSE_CLASS,
    DONE
}
public class CreateCharacter : MonoBehaviour, KeyInputReceiver {
    public Text MenuText, CharacterText;

    public GameObject NameInput;

    private Race selectedRace = Race.HILL_DWARF;
    private CreateCharacterState state = CreateCharacterState.DEFAULT;

    private const string DEFAULT_TEXT = "1. Select Race\n2. Re-Roll Abilities\n3. Chose Class";
    private const string RACE_SELECTION_TEXT = "1. Hill Dwarf\n2. High Elf";
    private const string CLASS_SELECTION_TEXT = "1. Barbarian\n2. Druid";
    private const string DONE_TEXT = "1. Accept Character\n2. Cancel";
    private CharacterSheet hero;

    // Start is called before the first frame update
    void Start() {
        NameInput.GetComponent<InputField>().onEndEdit.AddListener(delegate (string value) {
            if (value.Length < 3) {
                NameInput.GetComponent<InputField>().text = "";
                NameInput.GetComponent<InputField>().placeholder.GetComponent<Text>().text = "Too short";
            } else if (value.Length > 15) {
                NameInput.GetComponent<InputField>().text = "";
                NameInput.GetComponent<InputField>().placeholder.GetComponent<Text>().text = "Too long";
            } else {
                hero.SetName(value);
                Game.Characters.Add(hero);
                SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            }
        });

        MenuText.text = DEFAULT_TEXT;
        reRollAbilities();
    }

    // Update is called once per frame
    void Update() {
    }

    public void KeyPressHandler(KeyCode key) {
        if (state == CreateCharacterState.DEFAULT) {
            if (key == KeyCode.Alpha1) {
                state = CreateCharacterState.CHOSE_RACE;
                MenuText.text = RACE_SELECTION_TEXT;
            } else if (key == KeyCode.Alpha2) {
                reRollAbilities();
            } else if (key == KeyCode.Alpha3) {
                state = CreateCharacterState.CHOSE_CLASS;
                MenuText.text = CLASS_SELECTION_TEXT;
            }
        } else if (state == CreateCharacterState.CHOSE_RACE) {
            if (key == KeyCode.Alpha1) {
                selectedRace = Race.HILL_DWARF;
                state = CreateCharacterState.DEFAULT;
                MenuText.text = DEFAULT_TEXT;
                reRollAbilities();
            } else if (key == KeyCode.Alpha2) {
                selectedRace = Race.HIGH_ELF;
                state = CreateCharacterState.DEFAULT;
                MenuText.text = DEFAULT_TEXT;
                reRollAbilities();
            }
        } else if (state == CreateCharacterState.CHOSE_CLASS) {
            if (key == KeyCode.Alpha1) {
                hero.AddLevel(CharacterClasses.Barbarian);
                state = CreateCharacterState.DONE;
                MenuText.text = DONE_TEXT;
                updateCharacterText();
            } else if (key == KeyCode.Alpha2) {
                hero.AddLevel(CharacterClasses.Druid);
                state = CreateCharacterState.DONE;
                MenuText.text = DONE_TEXT;
                updateCharacterText();
            }
        } else if (state == CreateCharacterState.DONE) {
            if (key == KeyCode.Alpha1) {
                NameInput.SetActive(true);
                NameInput.GetComponent<InputField>().Select();
            } else if (key == KeyCode.Alpha2) {
                SceneManager.LoadScene("MainMenu");
            }
        }
    }

    private void reRollAbilities() {
        hero = new CharacterSheet(selectedRace, true);
        updateCharacterText();
    }

    private void updateCharacterText() {
        CharacterText.text = String.Format("STR: {0,2} ({1,2})   DEX: {2,2} ({3,2})\nCON: {4,2} ({5,2})   CHA: {6,2} ({7,2})\nINT: {8,2} ({9,2})   WIS: {10,2} ({11,2})\n\n{12}\n",
            hero.Strength.Value, hero.Strength.Modifier.ToString("+#;-#;0"), hero.Dexterity.Value, hero.Dexterity.Modifier.ToString("+#;-#;0"),
            hero.Constitution.Value, hero.Constitution.Modifier.ToString("+#;-#;0"), hero.Charisma.Value, hero.Charisma.Modifier.ToString("+#;-#;0"),
            hero.Intelligence.Value, hero.Intelligence.Modifier.ToString("+#;-#;0"), hero.Wisdom.Value, hero.Wisdom.Modifier.ToString("+#;-#;0"),
            selectedRace.Description());
        foreach (Feat feat in hero.Feats) {
            CharacterText.text += String.Format("\n{0}", feat.Description());
        }
    }
}
