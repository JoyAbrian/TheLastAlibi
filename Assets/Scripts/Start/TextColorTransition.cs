using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextColorTransition : MonoBehaviour
{
    private TextMeshProUGUI targetText;
    public Color startColor = Color.white;
    public Color targetColor = Color.black;
    public float duration = 1.5f;
    public bool loop = false;

    private float timer = 0f;
    private bool reversing = false;

    private void Start()
    {
        targetText = GetComponent<TextMeshProUGUI>();
        targetText.color = startColor;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        float t = Mathf.Clamp01(timer / duration);
        Color newColor = Color.Lerp(startColor, targetColor, t);
        targetText.color = newColor;

        if (t >= 1f)
        {
            if (loop)
            {
                timer = 0f;
                reversing = !reversing;
                (startColor, targetColor) = (targetColor, startColor);
            }
            else
            {
                enabled = false;
            }
        }
    }
}
