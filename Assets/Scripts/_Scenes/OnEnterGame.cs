﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System;

public class OnEnterGame : MonoBehaviour, IPointerClickHandler
{
    public static int current_tactic = -1;
    public static int history_limit = 9;
    public static GameInfo gameInfo;

    public GameObject gameStartImage, victoryImage, defeatImage, drawImage, yourTurnImage, notEnoughCoinsImage, notEnoughOresImage, fullTacticBag, freezeText, winReward;
    public GameObject pathDot, targetDot, oldLocation, explosion, askTriggerPanel, settingsPanel;
    public GameObject pieceInfoCard, showInfoCard, playerFlag, enemyFlag, freezeImage;
    public Transform tacticBag, history;
    public Button endTurnButton;
    public Text roundCount, timer, modeName;
    public Text playerName, playerRank;
    public Text opponentName, opponentRank;
    public Text oreText, coinText, endTurnText;
    [HideInInspector] public GameObject board;
    [HideInInspector] public BoardSetup boardSetup;

    private static Lineup lineup;
    private static List<GameObject> historySlots;
    private static List<Transform> tacticObjs;
    private static List<Button> tacticButtons;
    private static List<TacticTrigger> tacticTriggers;
    private static Dictionary<String, int> credits = new Dictionary<string, int>()
    {
        { "Chariot", 8 }, { "Horse", 4}, {"Elephant", 3}, {"Advisor", 2}, {"General", 10}, {"Cannon", 4}, {"Soldier", 2}
    };

    // Used for ask trigger
    private static Trigger trigger;
    private static string triggerMessage;

    // Use this for initialization
    void Awake () {
        lineup = Login.user.lineups[Login.user.lastLineupSelected];
        board = Instantiate(Resources.Load<GameObject>("Board/" + lineup.boardName + "/Board"));
        board.transform.SetSiblingIndex(1);
        boardSetup = board.GetComponent<BoardSetup>();
        boardSetup.Setup(lineup, Login.playerID);  // Set up Player Lineup
        boardSetup.Setup(gameInfo.lineups[gameInfo.TheOtherPlayer()], gameInfo.TheOtherPlayer());  // Set up Enemy Lineup
        GameEvent.SetBoard(boardSetup.boardAttributes);

        modeName.text = gameInfo.mode;
        // Set up Player Info
        playerName.text = Login.user.username;
        playerRank.text = "Rank: " + Login.user.rank.ToString();
        // Set up Opponent Info
        opponentName.text = gameInfo.matchInfo[gameInfo.TheOtherPlayer()].playerName;
        opponentRank.text = "Rank: " + gameInfo.matchInfo[gameInfo.TheOtherPlayer()].rank.ToString();

        foreach (var item in gameInfo.triggers)
            item.Value.StartOfGame();
        SetOreText();
        SetCoinText();
        // SetupTactics
        tacticObjs = new List<Transform>();
        tacticButtons = new List<Button>();
        tacticTriggers = new List<TacticTrigger>();
        for (int i = 0; i < Lineup.tacticLimit; i++)
        {
            Transform tacticSlot = tacticBag.Find(String.Format("TacticSlot{0}", i));
            tacticButtons.Add(tacticSlot.GetComponent<Button>());
            tacticObjs.Add(tacticSlot.Find("Tactic"));
            tacticObjs[i].GetComponent<TacticInfo>().SetAttributes(Database.FindTacticAttributes(lineup.tactics[i].tacticName));
            tacticTriggers.Add(tacticObjs[i].GetComponent<TacticInfo>().trigger);
        }
        historySlots = new List<GameObject>();
        for (int i = 0; i < history_limit; i++) historySlots.Add(history.Find("Slot" + i.ToString()).gameObject);
        //foreach(var item in gameInfo.triggers)
        //{
        //    if (item.Value.passive == "Tactic")
        //    {
        //        foreach (Tactic tactic in lineup.tactics)
        //            if (item.Value.PassiveCriteria(tactic))
        //                item.Value.Passive(tactic);
        //    }
        //    else if(item.Value.passive == "Piece")
        //    {
        //        foreach (var pair in gameInfo.board)
        //            if (item.Value.PassiveCriteria(pair.Value))
        //                item.Value.Passive(pair.Value);
        //    }
        //}
        StartCoroutine(GameStartAnimation());
        StartCoroutine(Timer());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!gameInfo.gameOver) return;
        if (victoryImage.activeSelf && Login.user.winsToday <= UserInfo.maxWinPerDay)
        {
            victoryImage.SetActive(false);
            winReward.SetActive(true);
            Login.user.ChangeCoins(1);
            return;
        }
        gameInfo.gameOver = false;
        SceneManager.LoadScene("PlayerMatching");
        gameInfo.Clear();
        Destroy(board);
    }

    private IEnumerator GameStartAnimation()
    {
        gameStartImage.SetActive(true);
        yield return new WaitForSeconds(3f);
        gameStartImage.SetActive(false);
    }

    private IEnumerator Timer()
    {
        while (true)
        {
            if (gameInfo.gameOver) break;
            string seconds = (gameInfo.time % 60).ToString();
            if (seconds.Length == 1) seconds = "0" + seconds;
            timer.text = (gameInfo.time / 60).ToString() + ":" + seconds;
            if (gameInfo.time < 15) timer.color = Color.red;
            else timer.color = Color.white;
            yield return new WaitForSeconds(1.0f);
            if (--gameInfo.time < 0) NextTurn();
        }
    }

    public void Victory(bool upload = true)
    {
        gameInfo.victory = Login.playerID;
        victoryImage.SetActive(true);
        Login.user.Win();
        GameOver();
        if(upload) new GameEvent("GameOver").Upload();
    }
    public void Defeat(bool upload = true)
    {
        gameInfo.victory = gameInfo.TheOtherPlayer();
        defeatImage.SetActive(true);
        Login.user.Lose();
        GameOver();
        if (upload) new GameEvent("GameOver").Upload();
    }
    public void Draw(bool upload = true)
    {
        bool playerTrophy = gameInfo.FindUsedTactic("Winner's Trophy", Login.playerID) != -1,
             enemyTrophy = gameInfo.FindUsedTactic("Winner's Trophy", gameInfo.TheOtherPlayer()) != -1;
        if (playerTrophy && !enemyTrophy) Victory();
        else if (!playerTrophy && enemyTrophy) Defeat();
        else
        {
            gameInfo.victory = -1;
            drawImage.SetActive(true);
            Login.user.Draw();
            GameOver();
            if (upload) new GameEvent("GameOver").Upload();
        }
    }

    public void GameOver()
    {
        StopAllCoroutines();
        Login.user.SetGameID(-1);
        pieceInfoCard.SetActive(false);
        gameInfo.time = gameInfo.maxTime;
        gameInfo.gameOver = true;
        if (settingsPanel.activeSelf) settingsPanel.SetActive(false);
        foreach(KeyValuePair<Location,GameObject> pair in boardSetup.pieces)
        {
            Trigger trigger = pair.Value.GetComponent<PieceInfo>().trigger;
            if (trigger != null) trigger.EndOfGame();
        }
        // CalculateNewRank(); // should be done by server
        foreach (KeyValuePair<Location, Collection> pair in new Dictionary<Location, Collection>(lineup.cardLocations))
        {
            bool alive = false;
            foreach(Piece piece in gameInfo.activePieces[Login.playerID])
                if (piece.original && piece.GetCastle() == pair.Key)
                {
                    alive = true;
                    lineup.cardLocations[pair.Key] = piece.collection;
                    break;
                }
            if (!alive && pair.Value.health > 0 && --pair.Value.health == 0)
            {
                int index = Login.user.FindCollection(pair.Value);
                Login.user.RemoveCollection(index);
                int next = Login.user.FindCollection(pair.Value.name);
                if (next != -1) lineup.cardLocations[pair.Key] = Login.user.collection[next];
                else lineup.cardLocations[pair.Key] = Collection.StandardCollection(pair.Value.type);
            }
        }
        foreach(Tactic tactic in new List<Tactic>(lineup.tactics))
        {
            if (gameInfo.unusedTactics[Login.playerID].Contains(tactic)) continue;
            int index = Login.user.FindCollection(tactic.tacticName);
            if (Login.user.collection[index].count > 1) Login.user.ChangeCollectionCount(index, -1, false);
            else lineup.tactics.Remove(tactic);
        }
        Login.user.ModifyLineup(lineup, Login.user.lastLineupSelected);
        Login.user.SetGameID(-1);
    }

    public void Concede()
    {
        Defeat();
    }

    private void CalculateNewRank()
    {
        int credit = 0;
        foreach(Piece piece in gameInfo.activePieces[Login.playerID])
            credit += credits[piece.GetPieceType()];
        foreach (Piece piece in gameInfo.inactivePieces[gameInfo.TheOtherPlayer()])
            credit += credits[piece.GetPieceType()];
    }

    public void EndTurn()
    {
        endTurnButton.interactable = false;
        NextTurn();
    }

    public void YourTurn()
    {
        if (gameInfo.MultiActions(Login.playerID))
        {
            endTurnButton.interactable = true;
            endTurnText.text = "End Turn";
        }
        else
        {
            endTurnButton.interactable = false;
            endTurnText.text = "Your Turn";
        }
        StartCoroutine(ShowYourTurn());
        gameInfo.ResetActions(Login.playerID);
        foreach (KeyValuePair<Location, GameObject> pair in boardSetup.pieces)
        {
            Trigger trigger = pair.Value.GetComponent<PieceInfo>().trigger;
            if (trigger != null) trigger.StartOfTurn();
        }
        SetTacticInteractable();
    }
    private IEnumerator ShowYourTurn(float time = 2f)
    {
        yourTurnImage.SetActive(true);
        yield return new WaitForSeconds(time);
        yourTurnImage.SetActive(false);
    }

    public void NextTurn(bool upload = true)
    {
        // Database Down Error
        return;
        foreach (KeyValuePair<Location, GameObject> pair in boardSetup.pieces)
        {
            Piece piece = pair.Value.GetComponent<PieceInfo>().piece;
            Trigger trigger = pair.Value.GetComponent<PieceInfo>().trigger;
            if (trigger != null)
            {
                if (piece.freeze > 0 && --piece.freeze == 0) Defreeze(pair.Key);
                trigger.EndOfTurn();
            }
        }

        // Send EndTurn GameEvent
        if(upload) new GameEvent("EndTurn").Upload();
        gameInfo.NextTurn();

        roundCount.text = gameInfo.round.ToString();
        if (gameInfo.round == 150)
        {
            Draw();
            return;
        }

        if (gameInfo.currentTurn == Login.playerID) YourTurn();
        else EnemyTurn();
    }

    public void EnemyTurn()
    {
        // Set Button interactable
        SetTacticInteractable(false);
        endTurnButton.interactable = false;
        endTurnText.text = "Enemy Turn";
    }

    public static void CancelTacticHighlight()
    {
        if (current_tactic == -1) return;
        tacticButtons[current_tactic].GetComponent<Image>().sprite = tacticButtons[current_tactic].spriteState.disabledSprite;
        current_tactic = -1;
    }

    public void TriggerTrap(Location location)
    {
        if (!gameInfo.traps.ContainsKey(location)) return;
        explosion.transform.position = new Vector3(location.x * MovementController.scale, location.y * MovementController.scale, -3);
        explosion.transform.SetParent(boardSetup.boardCanvas);
        TrapAttributes trap = Database.FindTrapAttributes(gameInfo.traps[location].Key);
        showInfoCard.GetComponent<CardInfo>().SetAttributes(trap, gameInfo.traps[location].Value);
        trap.trigger.Activate(location);
        gameInfo.traps.Remove(location);
        // upload
        AddToHistory(new GameEvent(trap.Name, gameInfo.traps[location].Value, gameInfo.board[location]));
        StartCoroutine(ShowTrapInfo());
    }

    private IEnumerator ShowTrapInfo(float time = 3f)
    {
        showInfoCard.SetActive(true);
        explosion.SetActive(true);
        yield return new WaitForSeconds(time);
        showInfoCard.SetActive(false);
        explosion.SetActive(false);
    }

    public void AddTactic(Tactic tactic)
    {
        int count = gameInfo.unusedTactics[Login.playerID].Count;
        if (count == Lineup.tacticLimit) StartCoroutine(ShowFullTacticBag());
        else
        {
            gameInfo.AddTactic(tactic);
            tacticObjs[count].parent.gameObject.SetActive(true);
            int index = 0;
            if (count != 0)
            {
                index = gameInfo.unusedTactics[Login.playerID].IndexOf(tactic);
                for (int i = count; i > index; i--)
                {
                    TacticInfo tacticInfo = tacticObjs[i - 1].GetComponent<TacticInfo>();
                    tacticObjs[i].GetComponent<TacticInfo>().SetAttributes(tacticInfo.tacticAttributes, Login.playerID, tacticInfo.tactic.original);
                }
            }
            TacticAttributes tacticAttributes = Database.FindTacticAttributes(tactic.tacticName);
            tacticObjs[index].GetComponent<TacticInfo>().SetAttributes(tacticAttributes, Login.playerID, false);
            tacticTriggers.Insert(index, tacticAttributes.trigger);
            SetTacticInteractable();
        }
    }

    public void RemoveTactic(Tactic tactic)
    {
        int index = gameInfo.FindUnusedTactic(tactic.tacticName, Login.playerID);
        int count = gameInfo.unusedTactics[Login.playerID].Count;
        if (count > 1)
        {
            for (int i = index; i < count - 1; i++)
            {
                TacticInfo tacticInfo = tacticObjs[i + 1].GetComponent<TacticInfo>();
                tacticObjs[i].GetComponent<TacticInfo>().SetAttributes(tacticInfo.tacticAttributes, Login.playerID, tacticInfo.tactic.original);
            }
        }
        else tacticObjs[0].GetComponent<TacticInfo>().Clear();
        tacticTriggers.RemoveAt(index);
        tacticObjs[count - 1].parent.gameObject.SetActive(false);
        gameInfo.RemoveTactic(index);
        SetTacticInteractable();
        CancelTacticHighlight();
    }

    public void SetTacticInteractable(bool interactable = true)
    {
        for (int i = 0; i < tacticTriggers.Count; i++)
            tacticButtons[i].interactable = interactable && tacticTriggers[i].Activatable();
    }

    public void Defreeze(Location location)
    {
        Destroy(GameController.freezeImages[location]);
        GameController.freezeImages.Remove(location);
    }

    public void ChangeTacticOreCost(int index, int deltaAmount)
    {
        tacticObjs[index].GetComponent<TacticInfo>().ChangeOreCost(deltaAmount);
    }
    public void ChangeTacticGoldCost(int index, int deltaAmount)
    {
        tacticObjs[index].GetComponent<TacticInfo>().ChangeGoldCost(deltaAmount);
    }

    public void AddToHistory(GameEvent gameEvent)
    {
        int index = 0;
        for(int i = 0; i < history_limit; i++)
            if (!historySlots[i].activeSelf)
            {
                historySlots[i].SetActive(true);
                index = i;
                break;
            }
        for(int i = index + 1; i < history_limit; i++)
            historySlots[i].GetComponent<MouseOverHistory>().SetAttributes(historySlots[i - 1].GetComponent<MouseOverHistory>().gameEvent);
        historySlots[index].GetComponent<MouseOverHistory>().SetAttributes(gameEvent);
    }

    public void AskTrigger(Piece piece, Trigger trigger_para, string message)
    {
        if (trigger_para.ReceiveMesseage(message) && trigger_para.piece.oreCost <= gameInfo.ores[Login.playerID])
        {
            gameInfo.Act("ability", Login.playerID, 1);
            trigger = trigger_para;
            triggerMessage = message;
            pieceInfoCard.SetActive(false);
            showInfoCard.SetActive(true);
            showInfoCard.GetComponent<CardInfo>().SetAttributes(piece.collection);
            askTriggerPanel.SetActive(true);
        }
    }
    public void ConfirmTrigger()
    {
        if (triggerMessage == "BloodThirsty") trigger.BloodThirsty();
        else if (triggerMessage == "AfterMove") trigger.AfterMove();
        else if (triggerMessage == "InEnemyCastle") trigger.InEnemyCastle();
        else if (triggerMessage == "InEnemyRegion") trigger.InEnemyRegion();
        else if (triggerMessage == "InEnemyPalace") trigger.InEnemyPalace();
        else if (triggerMessage == "AtEnemyBottom") trigger.AtEnemyBottom();
        CancelTrigger();
    }
    public void CancelTrigger()
    {
        trigger = null;
        triggerMessage = "";
        askTriggerPanel.SetActive(false);
        showInfoCard.SetActive(false);
        gameInfo.Act("ability", Login.playerID, -1);
        if (!gameInfo.Actable(Login.playerID)) NextTurn();
    }
    private IEnumerator ShowFullTacticBag(float time = 1.5f)
    {
        fullTacticBag.SetActive(true);
        yield return new WaitForSeconds(time);
        fullTacticBag.SetActive(false);
    }
    public void ShowPieceFrozen()
    {
        StartCoroutine(PieceFrozen());
    }
    private IEnumerator PieceFrozen(float time = 1.5f)
    {
        freezeText.SetActive(true);
        yield return new WaitForSeconds(time);
        freezeText.SetActive(false);
    }
    public void ShowNotEnoughCoins()
    {
        StartCoroutine(NotEnoughCoins());
    }
    private IEnumerator NotEnoughCoins(float time = 1.5f)
    {
        notEnoughCoinsImage.SetActive(true);
        yield return new WaitForSeconds(time);
        notEnoughCoinsImage.SetActive(false);
    }
    public void ShowNotEnoughOres()
    {
        StartCoroutine(NotEnoughOres());
    }
    private IEnumerator NotEnoughOres(float time = 1.5f)
    {
        notEnoughOresImage.SetActive(true);
        yield return new WaitForSeconds(time);
        notEnoughOresImage.SetActive(false);
    }
    public void SetOreText()
    {
        oreText.text = gameInfo.ores[Login.playerID].ToString();
    }
    public void SetCoinText()
    {
        coinText.text = Login.user.coins.ToString();
    }
}
