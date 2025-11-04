using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovementController : MonoBehaviour
{
    private static readonly int IsMoving = Animator.StringToHash("isMoving");

    [Header("Settings")]
    [SerializeField] public float speed = 5f;

    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator; // Animator to set IsMoving
    [SerializeField] private SpriteRenderer spriteRenderer; // optional: sprite to flip when moving left/right

    private float _gravity = -9.81f;
    private Vector3 _velocity;

    void Start()
    {
        // Auto-assign CharacterController if not set in inspector
        if (controller == null)
            controller = GetComponent<CharacterController>();

        // Auto-assign Animator if not set
        if (animator == null)
            animator = GetComponent<Animator>();

        // Auto-assign SpriteRenderer if not set (search children)
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (controller == null) return;

        // Read raw input (legacy Input system kept as in original)
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Determine camera-relative axes (fall back to player transform if no camera)
        Transform cam = Camera.main != null ? Camera.main.transform : null;
        Vector3 camForward = cam != null ? cam.forward : transform.forward;
        Vector3 camRight = cam != null ? cam.right : transform.right;

        // Flatten to horizontal plane and normalize
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Calculate movement direction relative to camera
        Vector3 move = camRight * moveX + camForward * moveZ;

        // Prevent faster diagonal movement
        if (move.sqrMagnitude > 1f)
            move.Normalize();

        // Update Animator parameter
        bool isMoving = move.sqrMagnitude > 0.0001f;
        if (animator != null)
            animator.SetBool(IsMoving, isMoving);

        // Flip sprite based on lateral movement relative to camera
        if (spriteRenderer != null && isMoving)
        {
            // lateral > 0 means moving to camera's right, lateral < 0 means left
            float lateral = Vector3.Dot(move.normalized, camRight);
            // If lateral is negative, face left (flipX true), else face right
            spriteRenderer.flipX = lateral < 0f;
        }

        // Move the character horizontally
        Vector3 horizontalMove = move * speed;
        controller.Move(horizontalMove * Time.deltaTime);

        // Reset vertical velocity when grounded
        if (controller.isGrounded && _velocity.y < 0f)
        {
            _velocity.y = -2f;
        }

        // Apply gravity
        _velocity.y += _gravity * Time.deltaTime;
        controller.Move(_velocity * Time.deltaTime);
    }
}
