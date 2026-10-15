using UnityEngine;

/// <summary>
/// Screen shake + a short "uhh" grunt when the player gets hit.
/// Call HitFeedback.PlayerHit(customClip). If no clip is given, a small grunt is generated in code.
/// </summary>
public static class HitFeedback
{
    private static AudioClip generatedGrunt;
    private static AudioSource source;

    private static AudioClip[] hurtClips;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { hurtClips = null; }

    /// <summary>Hurt sounds in Assets/Combat/Resources/Sfx (player_hurt_1, _2, _3 ...). Empty = use the generated grunt.</summary>
    private static AudioClip RandomHurtClip()
    {
        if (hurtClips == null)
        {
            System.Collections.Generic.List<AudioClip> list = new System.Collections.Generic.List<AudioClip>();
            foreach (AudioClip c in Resources.LoadAll<AudioClip>("Sfx"))
                if (c.name.StartsWith("player_hurt")) list.Add(c);
            hurtClips = list.ToArray();
        }
        return hurtClips.Length > 0 ? hurtClips[Random.Range(0, hurtClips.Length)] : null;
    }

    public static void PlayerHit(AudioClip clip, float volume, float shakeStrength, float shakeTime)
    {
        if (clip == null) clip = RandomHurtClip();
        PlaySound(clip != null ? clip : Grunt(), volume);
        CameraShake.Shake(shakeStrength, shakeTime);
    }

    /// <summary>Short bright "ding" for picking up coins (generated in code).</summary>
    public static void CoinDing()
    {
        if (generatedDing == null)
        {
            const int rate = 44100;
            int n = (int)(rate * 0.18f);
            float[] d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float f = t < 0.06f ? 1320f : 1760f;               // two quick notes
                float env = Mathf.Exp(-t * 22f) * Mathf.Clamp01(t / 0.003f);
                d[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.5f;
            }
            generatedDing = AudioClip.Create("Coin (generated)", n, 1, rate, false);
            generatedDing.SetData(d, 0);
        }
        PlaySound(generatedDing, 0.35f, false);
    }

    private static AudioClip generatedDing;

    public static void PlaySound(AudioClip clip, float volume, bool randomPitch = true)
    {
        if (clip == null) return;
        if (source == null)
        {
            GameObject go = new GameObject("HitFeedback Audio (auto)");
            Object.DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D
        }
        source.pitch = randomPitch ? Random.Range(0.92f, 1.08f) : 1f; // slight variety each hit
        source.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// Builds a short (~0.25s) low "uhh" grunt: a falling-pitch voiced buzz
    /// shaped by two vowel formants, with a quick attack and fade.
    /// </summary>
    private static AudioClip Grunt()
    {
        if (generatedGrunt != null) return generatedGrunt;

        const int rate = 44100;
        const float length = 0.26f;
        int n = (int)(rate * length);
        float[] data = new float[n];

        // Two band-pass filters at "uh" vowel formants
        Biquad f1 = Biquad.BandPass(rate, 600f, 4f);
        Biquad f2 = Biquad.BandPass(rate, 1150f, 5f);

        float phase = 0f;
        System.Random rng = new System.Random(7);
        float peak = 0.0001f;

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float k = t / length;

            float pitch = Mathf.Lerp(165f, 105f, k);        // voice drops: "UHh"
            phase += pitch / rate;
            if (phase >= 1f) phase -= 1f;

            float buzz = (phase < 0.12f ? 1f : 0f) - 0.12f; // glottal-like pulse train
            float breath = ((float)rng.NextDouble() * 2f - 1f) * 0.08f;
            float src = buzz + breath;

            float s = f1.Process(src) * 1.0f + f2.Process(src) * 0.55f;

            float attack = Mathf.Clamp01(t / 0.015f);
            float release = Mathf.Pow(1f - k, 1.6f);
            s *= attack * release;

            data[i] = s;
            peak = Mathf.Max(peak, Mathf.Abs(s));
        }

        for (int i = 0; i < n; i++) data[i] = data[i] / peak * 0.8f; // normalise

        generatedGrunt = AudioClip.Create("Grunt (generated)", n, 1, rate, false);
        generatedGrunt.SetData(data, 0);
        return generatedGrunt;
    }

    private class Biquad
    {
        private float b0, b1, b2, a1, a2, x1, x2, y1, y2;

        public static Biquad BandPass(float rate, float freq, float q)
        {
            float w = 2f * Mathf.PI * freq / rate;
            float alpha = Mathf.Sin(w) / (2f * q);
            float a0 = 1f + alpha;
            return new Biquad
            {
                b0 = alpha / a0,
                b1 = 0f,
                b2 = -alpha / a0,
                a1 = -2f * Mathf.Cos(w) / a0,
                a2 = (1f - alpha) / a0
            };
        }

        public float Process(float x)
        {
            float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
            x2 = x1; x1 = x;
            y2 = y1; y1 = y;
            return y;
        }
    }
}

/// <summary>
/// Shakes the main camera. Added to the camera automatically the first time it's needed.
/// Works with a camera that follows by script (CameraFollow) or one parented to the player.
/// </summary>
[DefaultExecutionOrder(10000)]
public class CameraShake : MonoBehaviour
{
    private float strength;
    private float duration;
    private float timeLeft;
    private Vector3 appliedOffset;

    public static void Shake(float strength, float duration)
    {
        // Shake every enabled camera tagged MainCamera (some scenes have more than one)
        foreach (Camera cam in Camera.allCameras)
        {
            if (cam == null || !cam.CompareTag("MainCamera")) continue;
            CameraShake shake = cam.GetComponent<CameraShake>();
            if (shake == null) shake = cam.gameObject.AddComponent<CameraShake>();
            shake.strength = Mathf.Max(shake.timeLeft > 0f ? shake.strength : 0f, strength);
            shake.duration = duration;
            shake.timeLeft = duration;
        }
    }

    // Remove last frame's offset before other scripts move the camera
    private void Update()
    {
        transform.position -= appliedOffset;
        appliedOffset = Vector3.zero;
    }

    // Add this frame's offset (world units) after everything else has moved the camera
    private void LateUpdate()
    {
        if (timeLeft <= 0f) return;
        timeLeft -= Time.deltaTime;
        float fade = Mathf.Clamp01(timeLeft / duration);
        Vector2 r = Random.insideUnitCircle * strength * fade;
        appliedOffset = new Vector3(r.x, r.y, 0f);
        transform.position += appliedOffset;
    }

    private void OnDisable()
    {
        transform.position -= appliedOffset;
        appliedOffset = Vector3.zero;
    }
}
