using UnityEngine;

/// <summary>
/// Attach this to the Hidden_Tool GameObject.
/// Makes the tool pulse with a glowing light effect to help the player find it.
/// Requires a Point Light as a child of this GameObject.
/// </summary>
public class GlowPulse : MonoBehaviour
{
    [Header("Glow Settings")]
    [Tooltip("The Point Light child of this GameObject")]
    public Light glowLight;

    [Tooltip("Minimum light intensity during pulse")]
    public float minIntensity = 0.5f;

    [Tooltip("Maximum light intensity during pulse")]
    public float maxIntensity = 3.0f;

    [Tooltip("How fast the glow pulses (higher = faster)")]
    public float pulseSpeed = 2.0f;

    [Header("Float Animation")]
    [Tooltip("Enable floating up and down animation")]
    public bool enableFloat = true;

    [Tooltip("How high the tool floats")]
    public float floatHeight = 0.15f;

    [Tooltip("How fast the tool floats")]
    public float floatSpeed = 1.5f;

    [Header("Rotation")]
    [Tooltip("Enable slow spinning")]
    public bool enableRotation = true;

    [Tooltip("Rotation speed in degrees per second")]
    public float rotationSpeed = 45f;

    // Private vars
    private Vector3 startPosition;
    private Renderer[] renderers;
    private float emissionIntensity;

    void Start()
    {
        startPosition = transform.position;

        // Auto-find the Point Light child if not set
        if (glowLight == null)
            glowLight = GetComponentInChildren<Light>();

        // Get all renderers for emission pulsing
        renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        float pulse = Mathf.Sin(Time.time * pulseSpeed);
        float t = (pulse + 1f) / 2f; // Normalize 0 to 1

        // --- Pulse the light intensity ---
        if (glowLight != null)
        {
            glowLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
        }

        // --- Pulse the material emission ---
        foreach (Renderer rend in renderers)
        {
            foreach (Material mat in rend.materials)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    Color baseEmission = mat.GetColor("_EmissionColor").linear;
                    float brightness = Mathf.Lerp(0.5f, 2.5f, t);
                    mat.SetColor("_EmissionColor", baseEmission * brightness);
                }
            }
        }

        // --- Float up and down ---
        if (enableFloat)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        // --- Slow spin ---
        if (enableRotation)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
}
