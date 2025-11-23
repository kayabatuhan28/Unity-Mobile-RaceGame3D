using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RaceManager : MonoBehaviour
{
    public Transform[] Waypoints;
    public List<RaceStatus> _RacingCars;

    public TextMeshProUGUI TotalRacersText;
    public TextMeshProUGUI PlayerPositionText;

    RaceStatus PlayerRaceStatus;

    // -------------------------------------------
    public GameObject[] Panels;
    public TextMeshProUGUI CurrentPositionText;

    int playerCarPosition;
    public bool IsGameStart;

    private void Start()
    {
        TotalRacersText.text = _RacingCars.Count.ToString();
    }


    void Update()
    {
        _RacingCars.Sort((a,b) => b.GetVehicleProgress().CompareTo(a.GetVehicleProgress()));
        playerCarPosition = _RacingCars.IndexOf(PlayerRaceStatus) + 1;
        PlayerPositionText.text = playerCarPosition.ToString();     
    }

    public void AddCar(RaceStatus InRaceStatus, bool IsPlayerCar = false)
    {
        _RacingCars.Add(InRaceStatus);
        if (IsPlayerCar)
        {
            PlayerRaceStatus = InRaceStatus;
        }
    }

    public void OnRaceFinished()
    {
        StartCoroutine(RaceFinishPanels());
        
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    System.Collections.IEnumerator RaceFinishPanels()
    {
        Time.timeScale = 0.9f;
        yield return new WaitForSeconds(0.2f);
        Time.timeScale = 0.7f;
        yield return new WaitForSeconds(0.2f);
        Time.timeScale = 0.5f;
        yield return new WaitForSeconds(0.2f);
        Time.timeScale = 0.3f;
        yield return new WaitForSeconds(0.2f);
                 
        if (playerCarPosition == 1) 
        {
            Panels[0].SetActive(true);
            GameManager.instance.AddMoney(2500);
        }
        else // player kaybetti
        {
            CurrentPositionText.text = playerCarPosition.ToString();
            Panels[1].SetActive(true);
        }

        yield return new WaitForSeconds(0.2f);
        Time.timeScale = 0f;

    }

}
