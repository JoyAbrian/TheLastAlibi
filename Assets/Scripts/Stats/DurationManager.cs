using UnityEngine;

public class DurationManager : MonoBehaviour
{
    private float timeBuffer = 0f;

    private void Update()
    {
        timeBuffer += Time.deltaTime;

        if (timeBuffer >= 1f)
        {
            int secondsPassed = Mathf.FloorToInt(timeBuffer);
            StatisticVariables.totalTimeSpend += secondsPassed;
            timeBuffer -= secondsPassed;
        }
    }
}