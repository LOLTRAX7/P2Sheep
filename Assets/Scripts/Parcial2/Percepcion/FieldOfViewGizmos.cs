using UnityEngine;

// Dibuja el campo de visión (radio + cono + línea al objetivo detectado).
// Por consigna, los Gizmos de los lobos deben verse siempre; igualmente se pueden apagar desde el inspector.
[RequireComponent(typeof(FieldOfViewSensor))]
public class FieldOfViewGizmos : MonoBehaviour
{
    [SerializeField] private bool _drawAlways = true;
    [SerializeField] private bool _drawDetectionLine = true;

    private FieldOfViewSensor _sensor;

    private Vector3 DirFromAngle(float angleDegrees)
    {
        float angleRad = (transform.eulerAngles.y + angleDegrees) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad));
    }

    private void OnDrawGizmos()
    {
        if (_drawAlways) Draw();
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawAlways) Draw();
    }

    private void Draw()
    {
        if (_sensor == null) _sensor = GetComponent<FieldOfViewSensor>();
        if (_sensor == null) return;

        Gizmos.color = _sensor.CanSeeTarget ? Color.red : new Color(1f, 0.9f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, _sensor.ViewRadius);

        if (_sensor.ViewAngle < 359f)
        {
            Vector3 left = DirFromAngle(-_sensor.ViewAngle * 0.5f);
            Vector3 right = DirFromAngle(_sensor.ViewAngle * 0.5f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + left * _sensor.ViewRadius);
            Gizmos.DrawLine(transform.position, transform.position + right * _sensor.ViewRadius);
        }

        if (!_drawDetectionLine || _sensor.Target == null) return;

        Gizmos.color = _sensor.CanSeeTarget ? Color.red : Color.gray;
        Gizmos.DrawLine(transform.position + Vector3.up * _sensor.EyeHeight, _sensor.Target.position + Vector3.up * _sensor.EyeHeight);
    }
}
