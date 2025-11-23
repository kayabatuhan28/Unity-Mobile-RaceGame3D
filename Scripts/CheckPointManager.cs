using System.Collections;
using TMPro;
using UnityEngine;

public class CheckPointManager : MonoBehaviour
{
    int TotalCheckPoints = 9;
    public int NextCheckPointIndex = 1;
    float RemainingTime = 15;

    [SerializeField] TextMeshProUGUI RemainingTimeText;
    [SerializeField] GameObject WinPanel;
    [SerializeField] GameObject LosePanel;
    [SerializeField] GameObject RemainingTimePanel;
    Coroutine TimerCoroutine;

    void Start()
    {
        TimerCoroutine = StartCoroutine(CountdownTimer());
    }

    public void OnCheckpointPassed()
    {
        if (NextCheckPointIndex != TotalCheckPoints)
        {
            NextCheckPointIndex++;
            RemainingTime += Random.Range(7, 10);
            RemainingTimeText.text = RemainingTime.ToString();
        }

        // Butun checkpointler gecildiyse
        if (NextCheckPointIndex >= TotalCheckPoints)
        {
            StopCoroutine(TimerCoroutine);
            RemainingTimePanel.SetActive(false);
            WinPanel.SetActive(true);
            GameManager.instance.AddMoney(2250);
            Time.timeScale = 0f;
        }
    }

    IEnumerator CountdownTimer()
    {
        RemainingTimeText.text = RemainingTime.ToString();
        while (true)
        {
            yield return new WaitForSeconds(1);
            RemainingTime--;
            RemainingTimeText.text = RemainingTime.ToString();

            if (RemainingTime <= 0)
            {              
                LosePanel.SetActive(true);
                Time.timeScale = 0f;
                break;
            }

        }
    }
}
