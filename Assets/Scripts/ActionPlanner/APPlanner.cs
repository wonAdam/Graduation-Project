﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class APPlanner
{
    public APGameState _gameState;
    public Event e_onUnitActionExit;

    public abstract void Simulate(MonoBehaviour coroutineOwner, Action OnSimulationCompleted, out List<APActionNode> actionNodes);
    public abstract bool IsAvailable(APActionNode prevNode);
}

public class MovePlanner : APPlanner
{
    ActionPointPanel _actionPointPanel;
    Pathfinder _pathfinder;
    public MovePlanner(APGameState gameState, Event e_onUnitMoveExit, Pathfinder pathfinder, ActionPointPanel actionPointPanel)
    {
        _gameState = gameState.Clone();
        e_onUnitActionExit = e_onUnitMoveExit;

        _actionPointPanel = actionPointPanel;
        _pathfinder = pathfinder;
    }

    public override bool IsAvailable(APActionNode prevNode) 
        => prevNode.GetType() != typeof(ActionNode_Move) && _gameState.self.actionPoint > 0;

    public override void Simulate(MonoBehaviour coroutineOwner, Action OnSimulationCompleted, out List<APActionNode> moveNodes)
    {
        moveNodes = new List<APActionNode>();
        coroutineOwner.StartCoroutine(Simulate_Coroutine(OnSimulationCompleted, moveNodes));
    }

    private IEnumerator Simulate_Coroutine(Action OnSimulationCompleted, List<APActionNode> moveNodes)
    {
        // pathfind
        bool pathServed = false;
        List<PFPath> paths = new List<PFPath>();
        _pathfinder.RequestAsync(_gameState, (p) => { paths = p; pathServed = true; });

        // pathfind가 끝날때까지 대기
        while (!pathServed) yield return null;

        // 가능한 곳으로 이동하는 모든 경우의 수는 List로 생성
        foreach (var path in paths)
        {
            // 움직이지 않는 Move Action은 예외처리
            if (path.destination == path.start) continue;

            // path에 따라 이동하고 actionPoint를 소모하는 MoveActionNode
            moveNodes.Add(new ActionNode_Move(_gameState, e_onUnitActionExit, path, _actionPointPanel));

            yield return null;
        }

        OnSimulationCompleted();
    }
}


public class AttackPlanner : APPlanner
{
    MapMgr _mapMgr;
    public AttackPlanner(APGameState gameState, Event e_onUnitAttackExit, MapMgr mapMgr)
    {
        _gameState = gameState.Clone();
        e_onUnitActionExit = e_onUnitAttackExit;

        _mapMgr = mapMgr;
    }

    public override bool IsAvailable(APActionNode prevNode)
    {
        if (_gameState.self.actionPoint < _gameState.self.owner.GetActionSlot(ActionType.Attack).cost)
            return false;
        else
            return true;
    }

    public override void Simulate(MonoBehaviour coroutineOwner, Action OnSimulationCompleted, out List<APActionNode> attackNodes)
    {
        attackNodes = new List<APActionNode>();
        coroutineOwner.StartCoroutine(Simulate_Coroutine(OnSimulationCompleted, attackNodes));
    }

    private IEnumerator Simulate_Coroutine(Action OnSimulationCompleted, List<APActionNode> attackNodes)
    {
        // 액션포인트가 충분한지부터 체크
        if(_gameState.self.actionPoint < _gameState.self.owner.GetActionSlot(ActionType.Attack).cost)
        {
            OnSimulationCompleted();
            yield break;
        }

        // 현실 큐브로 기본공격 범위를 먼저 Get
        Cube centerCube = _gameState.unitPos.FirstOrDefault(p => p.Value == _gameState.self).Key.owner;
        List<Cube> cubesInAttackRange = _mapMgr.GetCubes(
            _gameState.self.owner.basicAttackRange.range,
            _gameState.self.owner.basicAttackRange.centerX,
            _gameState.self.owner.basicAttackRange.centerZ,
            centerCube
            );

        // 공격 가능한 모든 곳으로 공격하는 모든 경우의 수는 List로 생성
        foreach (Cube cube in cubesInAttackRange)
        {
            // 자기 자신의 위치를 공격하는 것은 예외처리
            if (cube == centerCube) continue;

            // cubesInAttackRange안의 적유닛이 있는 한 큐브를 공격하는 AttackActionNode
            APUnit target;
            _gameState.unitPos.TryGetValue(_gameState.APFind(cube), out target);
            if(target != null && target != _gameState.self && _gameState.self.owner.team.enemyTeams.Contains(target.owner.team))
                attackNodes.Add(new ActionNode_Attack(_gameState, e_onUnitActionExit, target.owner, _mapMgr));

            yield return null;
        }

        OnSimulationCompleted();
    }
}

