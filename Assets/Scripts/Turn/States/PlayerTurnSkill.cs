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
                    // 스킬은 유닛이 없는 곳에 구사가능
                    string popupContent = "r u sure you wanna use Skill here?";

                    List<Cube> cubesInSkillSplash = owner.mapMgr.GetCubes(
                        unit.skillSplash.range, unit.skillSplash.centerX, unit.skillSplash.centerZ, cubeClicked);

                    owner.stateMachine.ChangeState(
                        new PlayerTurnPopup(owner, unit, Input.mousePosition, popupContent, 
                        ()=>CastSkillOnCube(cubeClicked), OnClickNo, () => cubesInSkillSplash.ForEach(c => c.SetBlink(0.7f)), null, () => owner.mapMgr.StopBlinkAll()),
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
        => true; // 범위내의 모든 큐브에 Cast가능합니다.

    private void CastSkillOnCube(Cube cubeClicked)
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

    private void OnClickNo() => owner.stateMachine.ChangeState(null, StateMachine<TurnMgr>.StateTransitionMethod.ReturnToPrev);

    private bool RaycastWithCubeMask(out RaycastHit hit)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Cube"));
    }
}
