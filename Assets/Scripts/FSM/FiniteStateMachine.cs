using System;
using System.Collections.Generic;
using UnityEngine;

// Adaptada de la FSM de clase. Los estados deciden sus propias transiciones.
public class FiniteStateMachine<T>
{
    private State<T> _currentState;
    private readonly Dictionary<T, State<T>> _allStates = new();
    private readonly List<(T target, Func<bool> condition)> _globalTransitions = new();

    // Las condiciones se evalúan dentro de la FSM, en orden de prioridad.
    public void AddGlobalTransition(T target, Func<bool> condition)
    {
        _globalTransitions.Add((target, condition));
    }

    public T CurrentStateID { get; private set; }
    public event Action<T> OnStateChanged = delegate { };

    public void AddState(T ID, State<T> state)
    {
        state.SetFSM(this);

        if (!_allStates.ContainsKey(ID))
            _allStates.Add(ID, state);
        else
            _allStates[ID] = state;
    }

    public void ChangeState(T ID)
    {
        if (!_allStates.ContainsKey(ID))
        {
            Debug.LogError("Missing State!");
            return;
        }

        _currentState?.Exit();
        _currentState = _allStates[ID];
        CurrentStateID = ID;
        OnStateChanged?.Invoke(ID);
        _currentState.Enter();
    }

    public void Update()
    {
        foreach (var transition in _globalTransitions)
        {
            if (EqualityComparer<T>.Default.Equals(CurrentStateID, transition.target)) continue;
            if (!transition.condition()) continue;
            ChangeState(transition.target);
            return;
        }

        if (_currentState != null) _currentState.Update();
    }
}
