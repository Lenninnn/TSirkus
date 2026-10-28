
using UnityEngine;

public class RopeManager : MonoBehaviour
{
    public enum Axis
    {
        X = 0,
        Y = 1,
        Z = 2
    }

    [System.Serializable]
    public class RopeSettings
    {
        [Header("Identificación")]
        public string ropeName;
        public Transform ropeObject;

        [Header("Jugador que controla la cuerda")]
        public BalanceSystem balanceSystem;

        [Header("Ejes locales")]
        public Axis lengthAxis = Axis.X;
        public Axis verticalAxis = Axis.Y;
        public Axis lateralAxis = Axis.Z;

        [Header("Movimiento normal")]
        [Range(0f, 0.1f)]
        public float sagFraction = 0.02f;

        [Range(0f, 0.05f)]
        public float lateralAmplitude = 0.006f;

        [Range(0f, 0.05f)]
        public float verticalAmplitude = 0.004f;

        [Min(0f)]
        public float oscillationSpeed = 1.0f;

        [Range(0f, 8f)]
        public float wavesAlongRope = 2f;

        [Header("Reacción al desequilibrio")]
        [Min(1f)]
        public float maximumAmplitudeMultiplier = 3f;

        [Min(1f)]
        public float maximumSpeedMultiplier = 1.5f;

        [Tooltip("Tiempo de respuesta de la cuerda al desequilibrio.")]
        [Min(0.01f)]
        public float responseSpeed = 2f;
    }

    [Header("Las cuatro cuerdas")]
    public RopeSettings[] ropes = new RopeSettings[4];

    [Header("Configuración global")]
    public bool animate = true;
    public bool recalculateNormals = true;

    private class RopeRuntime
    {
        public Mesh mesh;
        public Vector3[] originalVertices;
        public Vector3[] deformedVertices;

        public float minLength;
        public float ropeLength;
        public float phase;

        public float currentInstability;
    }

    private RopeRuntime[] runtimeRopes;

    private void Awake()
    {
        runtimeRopes = new RopeRuntime[ropes.Length];

        for (int i = 0; i < ropes.Length; i++)
        {
            InitializeRope(i);
        }
    }

    private void InitializeRope(int index)
    {
        RopeSettings settings = ropes[index];

        if (settings == null || settings.ropeObject == null)
        {
            Debug.LogWarning(
                "RopeManager: falta asignar la cuerda " + index,
                this
            );
            return;
        }

        if (settings.lengthAxis == settings.verticalAxis ||
            settings.lengthAxis == settings.lateralAxis ||
            settings.verticalAxis == settings.lateralAxis)
        {
            Debug.LogError(
                "RopeManager: los tres ejes deben ser diferentes en "
                + settings.ropeName,
                settings.ropeObject
            );
            return;
        }

        MeshFilter filter =
            settings.ropeObject.GetComponent<MeshFilter>();

        if (filter == null)
        {
            filter = settings.ropeObject
                .GetComponentInChildren<MeshFilter>();
        }

        if (filter == null || filter.sharedMesh == null)
        {
            Debug.LogError(
                "RopeManager: no se encontró MeshFilter en "
                + settings.ropeName,
                settings.ropeObject
            );
            return;
        }

        Mesh meshCopy = Instantiate(filter.sharedMesh);
        meshCopy.name = filter.sharedMesh.name + "_Runtime_" + index;
        filter.sharedMesh = meshCopy;

        RopeRuntime runtime = new RopeRuntime
        {
            mesh = meshCopy,
            originalVertices = meshCopy.vertices,
            deformedVertices = new Vector3[meshCopy.vertexCount],
            phase = Random.Range(0f, Mathf.PI * 2f),
            currentInstability = 0f
        };

        Bounds bounds = meshCopy.bounds;

        runtime.minLength =
            GetAxis(bounds.min, (int)settings.lengthAxis);

        float maxLength =
            GetAxis(bounds.max, (int)settings.lengthAxis);

        runtime.ropeLength = maxLength - runtime.minLength;

        if (runtime.ropeLength <= 0.0001f)
        {
            Debug.LogError(
                "RopeManager: longitud casi cero. Revisa el eje de "
                + settings.ropeName,
                settings.ropeObject
            );

            Destroy(meshCopy);
            return;
        }

        runtimeRopes[index] = runtime;

        Debug.Log(
            "Cuerda inicializada: " + settings.ropeName,
            settings.ropeObject
        );

        DeformRope(settings, runtime, 0f);
    }

    private void Update()
    {
        if (!animate || runtimeRopes == null)
            return;

        for (int i = 0; i < ropes.Length; i++)
        {
            RopeRuntime runtime = runtimeRopes[i];

            if (runtime == null || ropes[i] == null)
                continue;

            RopeSettings settings = ropes[i];

            float targetInstability = GetInstability(settings);

            // La intensidad cambia suavemente, sin saltos.
            runtime.currentInstability = Mathf.MoveTowards(
                runtime.currentInstability,
                targetInstability,
                settings.responseSpeed * Time.deltaTime
            );

            DeformRope(settings, runtime, Time.time);
        }
    }

    private float GetInstability(RopeSettings settings)
    {
        BalanceSystem balance = settings.balanceSystem;

        if (balance == null)
            return 0f;

        if (!balance.IsBalancing || balance.IsCalibrating)
            return 0f;

        // CORRECCIÓN:
        // IsBalanced = true  -> SafeZone -> movimiento lento.
        // IsBalanced = false -> fuera de SafeZone -> movimiento rápido.
        //
        // Si el comportamiento real sigue invertido, habrá que verificar
        // cómo BalanceSystem calcula IsBalanced.
        return balance.IsBalanced ? 0f : 1f;
    }

    private void DeformRope(
        RopeSettings settings,
        RopeRuntime runtime,
        float time)
    {
        float instability = runtime.currentInstability;

        float sag = runtime.ropeLength * settings.sagFraction;

        // La amplitud aumenta al perder el equilibrio.
        float amplitudeMultiplier = Mathf.Lerp(
            1f,
            settings.maximumAmplitudeMultiplier,
            instability
        );

        // La velocidad también aumenta al perder el equilibrio.
        float speedMultiplier = Mathf.Lerp(
            1f,
            settings.maximumSpeedMultiplier,
            instability
        );

        float lateral = runtime.ropeLength
            * settings.lateralAmplitude
            * amplitudeMultiplier;

        float vertical = runtime.ropeLength
            * settings.verticalAmplitude
            * amplitudeMultiplier;

        float speed = settings.oscillationSpeed * speedMultiplier;

        for (int i = 0; i < runtime.originalVertices.Length; i++)
        {
            Vector3 original = runtime.originalVertices[i];
            Vector3 vertex = original;

            float distance =
                GetAxis(original, (int)settings.lengthAxis);

            float t = Mathf.Clamp01(
                (distance - runtime.minLength) / runtime.ropeLength
            );

            // Mantener los extremos casi fijos.
            float envelope = Mathf.Sin(Mathf.PI * t);

            float sagOffset = sag * envelope;

            float wave = Mathf.Sin(
                time * speed
                + t * settings.wavesAlongRope * Mathf.PI * 2f
                + runtime.phase
            );

            float verticalWave = Mathf.Sin(
                time * speed * 1.3f
                + t * settings.wavesAlongRope * Mathf.PI * 2f
                + runtime.phase + 1.2f
            );

            SetAxis(
                ref vertex,
                (int)settings.verticalAxis,
                GetAxis(vertex, (int)settings.verticalAxis)
                - sagOffset + vertical * verticalWave * envelope
            );

            SetAxis(
                ref vertex,
                (int)settings.lateralAxis,
                GetAxis(vertex, (int)settings.lateralAxis)
                + lateral * wave * envelope
            );

            runtime.deformedVertices[i] = vertex;
        }

        runtime.mesh.vertices = runtime.deformedVertices;
        runtime.mesh.RecalculateBounds();

        if (recalculateNormals)
            runtime.mesh.RecalculateNormals();
    }

    private float GetAxis(Vector3 v, int axis)
    {
        switch (axis)
        {
            case 0: return v.x;
            case 1: return v.y;
            default: return v.z;
        }
    }

    private void SetAxis(ref Vector3 v, int axis, float value)
    {
        switch (axis)
        {
            case 0: v.x = value; break;
            case 1: v.y = value; break;
            default: v.z = value; break;
        }
    }

    private void OnDestroy()
    {
        if (runtimeRopes == null)
            return;

        foreach (RopeRuntime runtime in runtimeRopes)
        {
            if (runtime == null || runtime.mesh == null)
                continue;

            if (Application.isPlaying)
                Destroy(runtime.mesh);
            else
                DestroyImmediate(runtime.mesh);
        }
    }
}
