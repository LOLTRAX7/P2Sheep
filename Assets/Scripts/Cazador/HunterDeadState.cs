using UnityEngine;

// Estado Dead (máxima prioridad).
//
// Se entra cuando un lobo alcanza al Cazador, desde cualquier estado. Desaparece de la escena,
// no responde a ningún otro comportamiento y, pasado RespawnTime (5 s por defecto), reaparece en la
// Zona inicial con el contador de recolecciones en cero y vuelve a Search.
public class HunterDeadState : State<HunterAgent.HunterState>
{
    private readonly HunterAgent _hunter;
    private float _respawnTimer;

    public HunterDeadState(HunterAgent hunter)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _respawnTimer = _hunter.RespawnTime;

        _hunter.StopMoving();
        _hunter.SetBodyVisible(false);
        _hunter.SetFeedback(HunterAgent.HunterState.Dead, null, "Muerto: reaparece en " + _respawnTimer.ToString("0.0") + " s");

        FeedbackLog.Add("[Cazador] DEAD. Reaparece en la Zona inicial en " + _respawnTimer.ToString("0.0") + " s.", new Color(1f, 0.2f, 0.2f));
    }

    public override void Update()
    {
        // No responde a nada más: sólo espera el respawn.
        _respawnTimer -= Time.deltaTime;

        if (_respawnTimer > 0f) return;

        _hunter.RespawnAtInitialZone();
        FeedbackLog.Add("[Cazador] Reapareció en la Zona inicial con el contador en cero.", new Color(0.3f, 1f, 0.4f));
        _fsm.ChangeState(HunterAgent.HunterState.Search);
    }

    public override void Exit()
    {
        // Nada: el cuerpo se vuelve a mostrar en RespawnAtInitialZone().
    }
}
