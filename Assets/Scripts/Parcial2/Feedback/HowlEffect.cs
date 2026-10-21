using UnityEngine;

// Anillo que se expande y se desvanece alrededor del lobo que aúlla: feedback visual de "se emitió una alerta".
// Se crea con HowlEffect.Spawn(...) y se destruye solo.
public class HowlEffect : MonoBehaviour
{
    private const int Segments = 48;

    private LineRenderer _line;
    private Vector3[] _points;
    private Color _color;
    private float _time;
    private float _duration = 1.3f;
    private float _maxRadius = 16f;

    public static void Spawn(Vector3 position, Color color)
    {
        GameObject effect = new GameObject("AnilloDeAullido");
        effect.transform.position = new Vector3(position.x, 0.25f, position.z);

        effect.AddComponent<HowlEffect>().Init(color);
    }

    private void Init(Color color)
    {
        _color = color;
        _points = new Vector3[Segments];

        _line = gameObject.AddComponent<LineRenderer>();
        _line.loop = true;
        _line.useWorldSpace = true;
        _line.positionCount = Segments;
        _line.widthMultiplier = 0.25f;
        _line.sharedMaterial = FeedbackMaterials.CreateUnlit(Color.white);
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows = false;
    }

    private void Update()
    {
        _time += Time.deltaTime;

        float t = _time / _duration;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        float radius = Mathf.Lerp(0.5f, _maxRadius, t);
        Vector3 center = transform.position;

        for (int i = 0; i < Segments; i++)
        {
            float angle = i / (float)Segments * Mathf.PI * 2f;
            _points[i] = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        }

        _line.SetPositions(_points);

        Color faded = _color;
        faded.a = 1f - t;
        _line.startColor = faded;
        _line.endColor = faded;
    }
}
