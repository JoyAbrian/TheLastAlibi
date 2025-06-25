using UnityEngine;

public class DialogDoneSign : MonoBehaviour
{
    public float zoomSpeed = 2f;
    public float maxScaleMultiplier = 1.25f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool scalingUp = true;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale * maxScaleMultiplier;
    }

    void Update()
    {
        if (scalingUp)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * zoomSpeed);
            if (Vector3.Distance(transform.localScale, targetScale) < 0.01f)
            {
                transform.localScale = targetScale;
                scalingUp = false;
            }
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * zoomSpeed);
            if (Vector3.Distance(transform.localScale, originalScale) < 0.01f)
            {
                transform.localScale = originalScale;
                scalingUp = true;
            }
        }
    }
}