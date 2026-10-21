using UnityEngine;

// Sonido del aullido generado por código (no hace falta ningún asset de audio):
// una onda que sube y baja de tono, con fade in/out.
public static class HowlSound
{
    private static AudioClip _clip;

    public static AudioClip Clip
    {
        get
        {
            if (_clip == null) _clip = Create(1.4f, 22050);
            return _clip;
        }
    }

    private static AudioClip Create(float seconds, int sampleRate)
    {
        int samples = (int)(seconds * sampleRate);
        float[] data = new float[samples];
        float phase = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)samples;
            float frequency = Mathf.Lerp(280f, 520f, Mathf.Sin(t * Mathf.PI));
            float envelope = Mathf.Sin(t * Mathf.PI);

            phase += 2f * Mathf.PI * frequency / sampleRate;
            data[i] = Mathf.Sin(phase) * envelope * 0.4f;
        }

        AudioClip clip = AudioClip.Create("Aullido", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
