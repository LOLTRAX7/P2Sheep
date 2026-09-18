// Clase base que define cómo funcionan los estados de la FSM.
// Todos los estados concretos deben decir qué hacen al entrar, durante su ejecución y al salir.
public abstract class State<T>
{
    protected FiniteStateMachine<T> _fsm;

    // La FSM llama esto al registrar el estado para que el estado conozca a qué FSM pertenece.
    // Gracias a eso puede pedir un cambio de estado desde su propia lógica.
    public void SetFSM(FiniteStateMachine<T> fsm) => _fsm = fsm;

    // Cada estado implementa su propia respuesta en estas tres etapas del ciclo de vida.
    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}
