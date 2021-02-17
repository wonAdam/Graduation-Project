﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerTurnSkill : TurnState
{
    List<Cube> cubesCanCast;
    List<Cube> cubesCastRange;
    Cube cubeClicked;

    public PlayerTurnSkill(TurnMgr owner, Unit unit) : base(owner, unit)
    {
        // get all cubes in range
        cubesCastRange = owner.mapMgr.GetCubes(
            unit.skillRange.range,
            unit.skillRange.centerX,
            unit.skillRange.centerZ,
            unit.GetCube
            );

        // filter cubes
        cubesCanCast = cubesCastRange
            .Where(CubeCanCastConditions)
            .ToList();
    }
    public override void Enter()
    {
        owner.cameraMove.SetTarget(unit);

        owner.mapMgr.BlinkCubes(cubesCastRange, 0.3f);
        owner.mapMgr.BlinkCubes(cubesCanCast, 0.7f);

        unit.StartBlink();
        owner.endTurnBtn.SetActive(true);
        owner.backBtn.SetActive(true);
    }

    public override void Execute()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            if (RaycastWithCubeMask(out hit))
            {
                Cube cubeClicked = hit.transform.GetComponent<Cube>();
                if (cubesCanCast.Contains(cubeClicked))
                {
                    string popupContent = "It is " + cubeClicked.GetUnit().name + " u use Skill?";

                    owner.stateMachine.ChangeState(new PlayerTurnPopup(owner, unit, owner.Popup, Input.mousePosition, popupContent, ()=>OnClickCubeCanCast(cubeClicked)),
                        StateMachine<TurnMgr>.StateTransitionMethod.JustPush);
   
                }
            }
        }
    }

    public override void Exit()
    {
        unit.StopBlink();
        owner.mapMgr.StopBlinkAll();
        owner.endTurnBtn.SetActive(false);
        owner.backBtn.SetActive(false);
    }
    private bool CubeCanCastConditions(Cube cube)
        => cube != unit.GetCube;
        //&& cube.GetUnit() != null // 유닛이 있지않아도 사용가능합니다.
            //&& unit.team.enemyTeams.Contains(cube.GetUnit().team); // 스킬은 범위로 팀 관계없이 영향을 줍니다.

    private void OnClickCubeCanCast(Cube cubeClicked)
    {
        this.cubeClicked = cubeClicked;

        TurnState nextState = new PlayerTurnBegin(owner, unit);
        owner.stateMachine.ChangeState(
            new WaitSingleEvent(owner, unit, owner.e_onUnitSkillExit, nextState),
            StateMachine<TurnMgr>.StateTransitionMethod.JustPush);

        unit.StopBlink();

        List<Cube> cubesToCast = owner.mapMgr.GetCubes(
            unit.skillSplash.range,
            unit.skillSplash.centerX,
            unit.skillSplash.centerX,
            cubeClicked);

        unit.CastSkill(cubesToCast, cubeClicked);
    }

    private bool RaycastWithCubeMask(out RaycastHit hit)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Cube"));
    }
}
