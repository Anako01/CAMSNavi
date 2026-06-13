using UnityEngine;

public class WaypointMarker : MonoBehaviour
{
    [Header("Float")]
    public float floatHeight = 0.15f;
    public float floatSpeed  = 2.0f;

    [Header("Spin")]

    public float spinSpeed   = 60f;

    [Header("Pulse Scale")]
    public float pulseAmount = 0.06f;
    public float pulseSpeed  = 2.5f;

    [Header("Emission Pulse")]
    public bool  pulseEmission = true;
    public Color emissionColor = new Color(0f, 0.8f, 1f);

    [Header("Billboard — face the AR camera")]
    public bool billboardToCamera = true;


    private Vector3  _originPos;
    private Vector3  _originScale;
    private Material _mat;

    void OnEnable()
    {
        _originPos   = transform.localPosition;
        _originScale = transform.localScale;

        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            _mat = rend.material;
            if (pulseEmission) _mat.EnableKeyword("_EMISSION");
        }
    }

    void Update()
    {
        float t = Time.time;

    
        // FIX: rotate to face the AR camera every frame so the marker is
        // always legible no matter where the user is standing.
        if (billboardToCamera && Camera.main != null)
        {
            // LookAt flips the forward axis, so we negate the direction
            // to keep the mesh facing the camera rather than away from it.
            Vector3 dirToCamera = Camera.main.transform.position - transform.position;
            if (dirToCamera != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(-dirToCamera);
        }

        // Float 
        float y = Mathf.Sin(t * floatSpeed) * floatHeight;
        transform.localPosition = _originPos + new Vector3(0f, y, 0f);

        // Spin (applied on top of billboard if both are on) 
        if (!billboardToCamera && spinSpeed != 0f)
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        // Scale pulse 
        float s = 1f + Mathf.Sin(t * pulseSpeed) * pulseAmount;
        transform.localScale = _originScale * s;

        // Emission pulse 
        if (pulseEmission && _mat != null)
        {
            float intensity = (Mathf.Sin(t * pulseSpeed) + 1f) * 0.5f;
            _mat.SetColor("_EmissionColor", emissionColor * intensity * 2f);
        }
    }

    void OnDisable()
    {
        // Reset transform so re-enabling starts cleanly from the origin
        transform.localPosition = _originPos;
        transform.localScale    = _originScale;
        if (_mat != null) _mat.SetColor("_EmissionColor", Color.black);
    }
}
