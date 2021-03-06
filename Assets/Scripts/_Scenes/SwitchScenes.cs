﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SwitchScenes : MonoBehaviour, IPointerClickHandler
{
    public static string switchSceneCaller = "Main";

    public GameObject playerInfoPanel, settingsPanel, optionsPanel, missionToday;
    public Transform missions;
    public Text winText, loseText, drawText, percentageText;
    public Text rank, title, nameText, playerIDText;

    private Canvas parentCanvas;
    private List<GameObject> missionList;
    private List<GameObject> newMissionButtonList;

    private void Start()
    {
        parentCanvas = GetComponent<Canvas>();
        missionToday.SetActive(Login.user.missions.Count != 0);
        missionList = new List<GameObject>();
        newMissionButtonList = new List<GameObject>();
        for (int i = 0; i < UserInfo.maxMissions; i++)
        {
            Transform mission = missions.Find("Mission" + i.ToString());
            missionList.Add(mission.Find("Mission").gameObject);
            newMissionButtonList.Add(mission.Find("ChangeMission").gameObject);
        }
    }

    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.Escape))
            settingsPanel.SetActive(true);
    }

    public void EnterCollection()
    {
        SceneManager.LoadScene("Collection");
    }

    public void EnterWar()
    {
        SceneManager.LoadScene("PlayerMatching");
    }

    public void EnterRecruitment()
    {
        SceneManager.LoadScene("Recruitment");
    }

    private void SetPlayerInfo()
    {
        winText.text = Login.user.total.win.ToString();
        loseText.text = Login.user.total.lose.ToString();
        drawText.text = Login.user.total.draw.ToString();
        percentageText.text = Login.user.total.percentage.ToString();
        rank.text = Login.user.rank.ToString();
        title.text = Range.FindTitle(Login.user.rank);
        nameText.text = Login.user.username;
        playerIDText.text = Login.user.playerID.ToString();
        SetMissions();
    }

    public void ShowPlayerInfo()
    {
        SetPlayerInfo();
        playerInfoPanel.SetActive(true);
    }

    private void SetMissions()
    {
        int missions = Login.user.missions.Count;
        for (int i = 0; i < UserInfo.maxMissions; i++)
        {
            missionList[i].SetActive(i < missions);
            newMissionButtonList[i].SetActive(i < missions && !Login.user.missionSwitched);
            if (i < missions) missionList[i].GetComponent<MissionInfo>().SetAttributes(Login.user.missions[i]);
        }
    }

    public void ChangeMission(int number)
    {
        Login.user.ChangeMission(number);
        foreach (GameObject button in newMissionButtonList) button.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (missionToday.activeSelf) missionToday.SetActive(false);
        else if (playerInfoPanel.activeSelf)
        {
            Vector2 mousePosition = AdjustedMousePosition();
            Rect rect = playerInfoPanel.GetComponent<RectTransform>().rect;
            // rect.x and rect.y are negative
            if (mousePosition.x < rect.x || mousePosition.x > -rect.x || mousePosition.y < rect.y || mousePosition.y > -rect.y)
                playerInfoPanel.SetActive(false);
        }
    }

    private Vector2 AdjustedMousePosition()
    {
        Vector2 mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvas.transform as RectTransform, Input.mousePosition, parentCanvas.worldCamera, out mousePosition);
        return mousePosition;
    }
}
