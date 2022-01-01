using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting;
using srd5;
using System;

public class NumButtonBehavior : MonoBehaviour {
    public GameObject Receiver;

    private KeyInputReceiver receiver;

    // Start is called before the first frame update
    void Start() {
        receiver = Receiver.GetComponent<KeyInputReceiver>();
        foreach (Button button in gameObject.GetComponentsInChildren<Button>()) {
            string keyCodeName = button.name.Replace("Button", "Alpha");
            KeyCode code;
            Enum.TryParse<KeyCode>(keyCodeName, out code);
            button.onClick.AddListener(delegate () {
                receiver.KeyPressHandler(code);
            });
        }
    }

    // Update is called once per frame
    void Update() {
        if (receiver == null) return;
        KeyCode[] codes = new KeyCode[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0 };
        foreach (KeyCode code in codes)
            if (Input.GetKeyUp(code))
                receiver.KeyPressHandler(code);

    }
}
