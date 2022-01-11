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
    private float scale = 1f;

    void Update() {
        float diff = Time.deltaTime / Slowness;
        if (scalingDown) {
            scale -= diff * (1f - Factor);
            if (scale < Factor)
                scalingDown = false;
        } else {
            scale += diff * (1f - Factor);
            if (scale > 1f)
                scalingDown = true;
        }
        foreach (Image icon in Icons) {
            if (!icon.gameObject.activeSelf) continue;
            icon.gameObject.transform.localScale = new Vector3(scale, scale);
        }
    }
}