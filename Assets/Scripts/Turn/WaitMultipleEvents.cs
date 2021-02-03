﻿using System;
using System.Collections.Generic;
using UnityEngine.Events;

public class WaitMultipleEvents : TurnState
{
    UnityEvent onEvent = new UnityEvent();
    List<EventListener> elList = new List<EventListener>();
    Action onWaitEnter; Action onWaitExecute; Action onWaitExit;
    List<Event> events;
    TurnState nextState;
    /// <summary>
    /// Event를 기다리는 State입니다.
    /// </summary>
    /// <param name="e">기다릴 Event</param>
    /// <param name="nextState">Event발생시 Transition할 다음 State</param>
    /// <param name="onEvent">state를 바꾸기 전에 호출할 함수가 잇다면 추가하세요.</param>
    public WaitMultipleEvents(TurnMgr owner, Unit unit, List<Event> events,
        TurnState nextState, Action onWaitEnter = null, Action onWaitExecute = null, Action onWaitExit = null) : base(owner, unit)
    {
        this.events = events;
        foreach (var e in events)
        {
            EventListener el = new EventListener();
            elList.Add(el);
            el.OnNotify.AddListener(() => { elList.Remove(el); e.Unregister(el); });
            e.Register(el);
        }
        this.nextState = nextState;

        if (onWaitEnter != null) this.onWaitEnter = onWaitEnter;
        if (onWaitExecute != null) this.onWaitExecute = onWaitExecute;
        if (onWaitExit != null) this.onWaitExit = onWaitExit;
    }

    public override void Enter()
    {
        if (onWaitEnter != null) this.onWaitEnter.Invoke();
    }

    public override void Execute()
    {
        if (onWaitExecute != null) this.onWaitExecute.Invoke();

        if (elList.Count == 0) owner.stateMachine.ChangeState(nextState, StateMachine<TurnMgr>.StateChangeMethod.JustPush);
    }

    public override void Exit()
    {
        if (onWaitExit != null) this.onWaitExit.Invoke();
    }

}