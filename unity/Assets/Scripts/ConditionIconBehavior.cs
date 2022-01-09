using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using srd5;

public class ConditionIconBehavior : MonoBehaviour {
    public float Slowness = .5f, Factor = .5f;
    public List<Image> Icons = new List<Image>();
    private bool scalingDown = true;
    private float scale = 1.0f;

    void Update() {
        float diff = Time.deltaTime / Slowness;
        if (scalingDown) {
            scale -= diff * Factor;
            if (scale < Factor)
                scalingDown = false;
        } else {
            scale += diff * 0.5f;
            if (scale > 1.0f)
                scalingDown = true;
        }
        foreach (Image icon in Icons) {
            if (!icon.gameObject.activeSelf) continue;
            icon.gameObject.transform.localScale = new Vector3(scale, scale);
        }
    }
}