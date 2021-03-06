﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AITurnAction : TurnState
{
    Queue<APActionNode> _actions;
    APActionNode _currAction;
    public AITurnAction(TurnMgr owner, Unit unit, List<APActionNode> actions) : base(owner, unit)
    {
        this._actions = new Queue<APActionNode>(actions);
    }

    public override void Enter()
    {
        unit.StartBlink();
        owner.cameraMove.SetTarget(unit);

        // 이전 액션한 결과, ShouldReplan이라면 AITurnPlan로 돌아가서 Replan
        // 첫 Enter는 _currAction == null
        if (_currAction != null && _currAction.ShouldReplan(owner.turns.ToList(), owner.mapMgr.map.Cubes.ToList()))
        {
            owner.stateMachine.ChangeState(
            new AITurnPlan(owner, unit, owner.actionPlanner),
            StateMachine<TurnMgr>.StateTransitionMethod.JustPush);

            return;
        }

        // 액션 고갈
        if (_actions.Count == 0)
        {
            owner.StartCoroutine(SomeDelayBeforeNextTurn());
            return;
        }

        owner.StartCoroutine(SomeDelayBeforeAction());
    }

    public override void Execute()
    {
    }

    public override void Exit()
    {
        unit.StopBlink();
    }

    private IEnumerator SomeDelayBeforeNextTurn()
    {
        float sec = Random.Range(0.5f, 1.5f);
        yield return new WaitForSeconds(sec);

        owner.NextTurn();
    }

    private IEnumerator SomeDelayBeforeAction()
    {
        float sec = Random.Range(0.5f, 1.5f);
        yield return new WaitForSeconds(sec);

        // 다음 액션
        _currAction = _actions.Dequeue();

        // 액션이 끝나는 이벤트를 wait
        owner.stateMachine.ChangeState(
            new WaitSingleEvent(owner, unit, owner.e_onUnitIdleEnter, this, null,
            _currAction.OnWaitEnter, _currAction.OnWaitExecute, _currAction.OnWaitExit),
            StateMachine<TurnMgr>.StateTransitionMethod.PopNPush);

        // 액션 실행
        _currAction.Perform();
    }

}
