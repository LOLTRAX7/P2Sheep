using System;
using System.Collections.Generic;
using UnityEngine;

// Base de clase: StateMachine (Clase 5) y FiniteStateMachine<T> (Pathfinding + LoS + FoV).
// Adaptaciones: asociación estado/FSM, ID para feedback y transiciones globales prioritarias.

// Es el motor de la FSM. Se encarga de guardar los estados, saber cuál está activo
// y realizar los cambios entre ellos.
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

    // Parcial 2: ID del estado activo y aviso de cada cambio (lo usan el feedback y los HUD de los lobos).
    public T CurrentStateID { get; private set; }
    public event Action<T> OnStateChanged = delegate { };

    public void AddState(T ID, State<T> state)
    {
        // Conecta el estado con esta FSM para que el estado pueda pedir cambios después.
        state.SetFSM(this);

        // Guarda el estado usando su ID. Si el ID ya existe, reemplaza el estado anterior.
        if (!_allStates.ContainsKey(ID))
            _allStates.Add(ID, state);
        else
            _allStates[ID] = state;
    }

    public void ChangeState(T ID)
    {
        // No se puede cambiar a un estado que la FSM no conoce.
        if (!_allStates.ContainsKey(ID))
        {
            Debug.LogError("Missing State!");
            return;
        }

        // Primero sale del estado anterior, después cambia la referencia al nuevo
        // y finalmente entra al nuevo estado.
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

        // Cada frame, la FSM deja que el estado actualmente activo ejecute su lógica.
        if (_currentState != null) _currentState.Update();
    }
}
