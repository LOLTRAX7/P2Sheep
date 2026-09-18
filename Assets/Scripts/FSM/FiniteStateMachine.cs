using System.Collections.Generic;
using UnityEngine;

// Es el motor de la FSM. Se encarga de guardar los estados, saber cuál está activo
// y realizar los cambios entre ellos.
public class FiniteStateMachine<T>
{
    private State<T> _currentState;
    private readonly Dictionary<T, State<T>> _allStates = new();

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
        _currentState.Enter();
    }

    public void Update()
    {
        // Cada frame, la FSM deja que el estado actualmente activo ejecute su lógica.
        if (_currentState != null) _currentState.Update();
    }
}
