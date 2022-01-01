using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using srd5;

public class MainMenuBehavior : MonoBehaviour, KeyInputReceiver {
    public Text MenuText, MainText;

    void Start() {
        MainText.text = "";
        foreach (CharacterSheet sheet in Game.Characters) {
            MainText.text += string.Format("{0} the {1}\n", sheet.Name, sheet.Levels[0].Class.Class.Name());
        }
    }

    public void KeyPressHandler(KeyCode code) {
        if (code == KeyCode.Alpha1)
            SceneManager.LoadScene("CharacterCreation");
    }

    void Update() {

    }
}