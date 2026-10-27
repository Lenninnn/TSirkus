using UnityEngine;

// Solo visual: mide cuánto se desplaza realmente el Player (CharacterController)
// y actualiza el Animator del modelo. No mueve nada ni lee ningún input,
// así que funciona igual con celular, teclado o cualquier otro origen.
// Va en la raíz del modelo con rig (hijo del Player), junto a su Animator.
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator animator;

    [Tooltip("Raíz del Player que se desplaza. Si está vacío, el CharacterController padre.")]
    [SerializeField] private Transform movingRoot;

    [Header("Velocidad")]
    [Tooltip("Suavizado del parámetro Speed (segundos).")]
    [SerializeField] private float speedDampTime = 0.08f;

    [Tooltip("Por encima de esto se considera un salto (teleport/respawn), no caminar.")]
    [SerializeField] private float maxPlausibleSpeed = 15f;

    [Header("Ritmo del paso")]
    [Tooltip("Velocidad de avance del clip Walk en unidades del modelo por segundo (moving: 4.719 u / 0.7 s).")]
    [SerializeField] private float walkClipSpeed = 6.741f;

    [SerializeField] private Vector2 walkSpeedMultiplierRange = new Vector2(0.5f, 1.5f);

    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int WalkSpeedMultiplierId = Animator.StringToHash("WalkSpeedMultiplier");

    private Vector3 lastPosition;


    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (movingRoot == null)
        {
            CharacterController body = GetComponentInParent<CharacterController>();
            movingRoot = body != null ? body.transform : transform.parent;
        }

        // La animación nunca desplaza al jugador.
        if (animator != null)
            animator.applyRootMotion = false;
    }


    private void OnEnable()
    {
        if (movingRoot != null)
            lastPosition = movingRoot.position;
    }


    private void Update()
    {
        if (animator == null || movingRoot == null)
            return;

        Vector3 delta = movingRoot.position - lastPosition;
        lastPosition = movingRoot.position;
        delta.y = 0f;

        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;

        if (speed > maxPlausibleSpeed)
            speed = 0f;

        animator.SetFloat(SpeedId, speed, speedDampTime, Time.deltaTime);

        // Ajusta el ritmo del paso a la velocidad real para que los pies no patinen.
        float clipWorldSpeed = walkClipSpeed * transform.lossyScale.x;

        if (clipWorldSpeed > 0f)
        {
            animator.SetFloat(
                WalkSpeedMultiplierId,
                Mathf.Clamp(speed / clipWorldSpeed, walkSpeedMultiplierRange.x, walkSpeedMultiplierRange.y));
        }
    }
}
