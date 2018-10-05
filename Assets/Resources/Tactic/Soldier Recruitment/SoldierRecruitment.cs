﻿using System.Collections.Generic;
using UnityEngine;

public class SoldierRecruitment : TacticTrigger {

    public override void Activate(Location loc)
    {
        GameController.AddPiece(Collection.Soldier, loc, Login.playerID);
    }

    public override List<Location> ValidTargets()
    {
        List<Location> target = new List<Location>();
        foreach (Location loc in GameController.FindCastles("Soldier"))
            if (!OnEnterGame.gameInfo.board.ContainsKey(loc))
                target.Add(loc);
        return target;
    }
}
