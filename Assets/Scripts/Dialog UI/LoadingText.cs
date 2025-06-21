using TMPro;
using UnityEngine;

public class LoadingText : MonoBehaviour
{
    public float interval = 0.5f;

    private TextMeshProUGUI loadingText;
    private int dotCount = 0;
    private float timer = 0f;

    private void Start()
    {
        loadingText = GetComponent<TextMeshProUGUI>();
        loadingText.text = "Loading";
    }
    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= interval)
        {
            dotCount = (dotCount + 1) % 4;
            loadingText.text = "Loading" + new string('.', dotCount);
            timer = 0f;
        }
    }
}