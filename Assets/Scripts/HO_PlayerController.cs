using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Handles prototype player movement, camera look, and exhibit interaction input.
/// </summary>
public sealed class HO_PlayerController : MonoBehaviour
{
    private const string DefaultCinemachineCameraName = "HO_PlayerFollowCamera";
    private const string DefaultPlayerTag = "Player";

    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private bool useCinemachineCamera = true;
    [SerializeField] private float followCameraDistance = 2.75f;
    [SerializeField] private float followCameraRadius = 0.2f;
    [SerializeField] private float followCameraSide = 0.5f;
    [SerializeField] private float followCameraVerticalArmLength = 0.35f;
    [SerializeField] private Vector3 followCameraShoulderOffset = new Vector3(0f, 0.15f, 0f);
    [SerializeField] private Vector3 followCameraDamping = new Vector3(0.1f, 0.1f, 0.1f);
    [SerializeField] private float followCameraDampingIntoCollision = 0.1f;
    [SerializeField] private float followCameraDampingFromCollision = 0.5f;
    [SerializeField] private LayerMask followCameraCollisionFilter = ~0;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float verticalLookLimit = 75f;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayerMask;
    [SerializeField] private HO_UIManager uiManagerReference;
    [SerializeField] private HO_CuratorPresenter curatorPresenterReference;

    private CinemachineBrain cinemachineBrain;
    private CinemachineCamera followCamera;
    private CinemachineThirdPersonFollow thirdPersonFollow;
    private float verticalLookAngle;
    private float verticalVelocity;
    private bool isControlLocked;
    private Collider currentInteractTarget;

    /// <summary>
    /// Resolves required references and prepares the optional Cinemachine rig.
    /// </summary>
    private void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (cameraRoot == null && playerCamera != null)
        {
            cameraRoot = playerCamera.transform.parent;
        }

        if (characterController == null)
        {
            Debug.LogError("HO_PlayerController requires a CharacterController component.", this);
            enabled = false;
            return;
        }

        EnsureCinemachineRig();

        if (cameraRoot != null)
        {
            verticalLookAngle = NormalizeAngle(cameraRoot.localEulerAngles.x);
        }
    }

    /// <summary>
    /// Reads input in prototype order: detection, interaction, look, then movement.
    /// </summary>
    private void Update()
    {
        if (!enabled)
        {
            return;
        }

        DetectInteractTarget();
        HandleInteractInput();
        HandleDebugCycleInput();

        if (isControlLocked)
        {
            ApplyGravityOnly();
            return;
        }

        HandleLookInput();
        HandleMoveInput();
    }

    /// <summary>
    /// Locks or unlocks player-controlled movement and view rotation.
    /// </summary>
    public void SetControlLocked(bool isLocked)
    {
        isControlLocked = isLocked;
    }

    /// <summary>
    /// Returns whether gameplay input is currently locked.
    /// </summary>
    public bool IsControlLocked()
    {
        return isControlLocked;
    }

    /// <summary>
    /// Creates and configures a Cinemachine follow rig for third-person tracking.
    /// </summary>
    private void EnsureCinemachineRig()
    {
        if (!useCinemachineCamera)
        {
            return;
        }

        if (cameraRoot == null || playerCamera == null)
        {
            Debug.LogWarning("HO_PlayerController could not enable Cinemachine because the camera root or player camera reference is missing.", this);
            return;
        }

        EnsurePlayerTagForCameraCollision();
        DetachPlayerCameraFromPlayerRoot();
        EnsureCinemachineBrain();
        EnsureFollowCamera();
        ConfigureFollowCamera();
    }

    /// <summary>
    /// Ensures the player can be ignored by the third-person collision solver.
    /// </summary>
    private void EnsurePlayerTagForCameraCollision()
    {
        if (!string.Equals(gameObject.tag, "Untagged"))
        {
            return;
        }

        try
        {
            gameObject.tag = DefaultPlayerTag;
        }
        catch (UnityException)
        {
            Debug.LogWarning("HO_PlayerController could not assign the default Player tag for Cinemachine collision filtering.", this);
        }
    }

    /// <summary>
    /// Detaches the render camera so the Cinemachine brain can drive world-space motion.
    /// </summary>
    private void DetachPlayerCameraFromPlayerRoot()
    {
        Transform cameraTransform = playerCamera.transform;

        if (cameraTransform.parent != null)
        {
            cameraTransform.SetParent(null, true);
        }
    }

    /// <summary>
    /// Adds a Cinemachine brain to the render camera if it is missing.
    /// </summary>
    private void EnsureCinemachineBrain()
    {
        if (!playerCamera.TryGetComponent(out cinemachineBrain))
        {
            cinemachineBrain = playerCamera.gameObject.AddComponent<CinemachineBrain>();
        }
    }

    /// <summary>
    /// Reuses or creates the dedicated follow camera controller object.
    /// </summary>
    private void EnsureFollowCamera()
    {
        if (followCamera != null)
        {
            return;
        }

        GameObject followCameraObject = GameObject.Find(DefaultCinemachineCameraName);

        if (followCameraObject == null)
        {
            followCameraObject = new GameObject(DefaultCinemachineCameraName);
        }

        if (!followCameraObject.TryGetComponent(out followCamera))
        {
            followCamera = followCameraObject.AddComponent<CinemachineCamera>();
        }

        if (!followCameraObject.TryGetComponent(out thirdPersonFollow))
        {
            thirdPersonFollow = followCameraObject.AddComponent<CinemachineThirdPersonFollow>();
        }
    }

    /// <summary>
    /// Applies the current follow pivot and collision settings to Cinemachine.
    /// </summary>
    private void ConfigureFollowCamera()
    {
        LensSettings lensSettings = followCamera.Lens;
        lensSettings.FieldOfView = playerCamera.fieldOfView;
        lensSettings.NearClipPlane = playerCamera.nearClipPlane;
        lensSettings.FarClipPlane = playerCamera.farClipPlane;

        followCamera.Lens = lensSettings;
        followCamera.Follow = cameraRoot;
        followCamera.LookAt = cameraRoot;
        followCamera.Priority = 100;

        thirdPersonFollow.ShoulderOffset = followCameraShoulderOffset;
        thirdPersonFollow.VerticalArmLength = followCameraVerticalArmLength;
        thirdPersonFollow.CameraDistance = followCameraDistance;
        thirdPersonFollow.CameraSide = followCameraSide;
        thirdPersonFollow.Damping = followCameraDamping;

        CinemachineThirdPersonFollow.ObstacleSettings obstacleSettings = thirdPersonFollow.AvoidObstacles;
        obstacleSettings.Enabled = true;
        obstacleSettings.CollisionFilter = followCameraCollisionFilter;
        obstacleSettings.IgnoreTag = gameObject.tag;
        obstacleSettings.CameraRadius = followCameraRadius;
        obstacleSettings.DampingIntoCollision = followCameraDampingIntoCollision;
        obstacleSettings.DampingFromCollision = followCameraDampingFromCollision;
        thirdPersonFollow.AvoidObstacles = obstacleSettings;
    }

    /// <summary>
    /// Rotates yaw on the player root and pitch on the camera pivot.
    /// </summary>
    private void HandleLookInput()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX, Space.Self);

        if (cameraRoot == null)
        {
            return;
        }

        verticalLookAngle -= mouseY;
        verticalLookAngle = Mathf.Clamp(verticalLookAngle, -verticalLookLimit, verticalLookLimit);
        cameraRoot.localRotation = Quaternion.Euler(verticalLookAngle, 0f, 0f);
    }

    /// <summary>
    /// Moves the player with CharacterController and prototype gravity.
    /// </summary>
    private void HandleMoveInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveInput = (transform.right * horizontal) + (transform.forward * vertical);

        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 movement = (moveInput * moveSpeed) + (Vector3.up * verticalVelocity);
        characterController.Move(movement * Time.deltaTime);
    }

    /// <summary>
    /// Keeps gravity active while scripted presentation locks player control.
    /// </summary>
    private void ApplyGravityOnly()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    /// <summary>
    /// Tracks the current interactable directly in front of the active camera view.
    /// </summary>
    private void DetectInteractTarget()
    {
        Transform viewTransform = playerCamera != null ? playerCamera.transform : cameraRoot;

        if (viewTransform == null)
        {
            UpdateInteractTarget(null);
            return;
        }

        Ray ray = new Ray(viewTransform.position, viewTransform.forward);
        Collider nextTarget = null;

        if (interactLayerMask.value != 0 && Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayerMask))
        {
            nextTarget = hit.collider;
        }

        UpdateInteractTarget(nextTarget);
    }

    /// <summary>
    /// Routes the E key either to curator presentation flow or exhibit interaction.
    /// </summary>
    private void HandleInteractInput()
    {
        if (!Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        if (curatorPresenterReference != null && curatorPresenterReference.IsPresentationActive())
        {
            curatorPresenterReference.RequestNextPresentation();
            return;
        }

        if (isControlLocked || currentInteractTarget == null)
        {
            return;
        }

        RequestInteraction(currentInteractTarget);
    }

    /// <summary>
    /// Keeps the existing prototype debug cycle input on Space.
    /// </summary>
    private void HandleDebugCycleInput()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
        {
            return;
        }

        if (curatorPresenterReference == null)
        {
            Debug.LogWarning("HO_PlayerController is missing a HO_CuratorPresenter reference.", this);
            return;
        }

        curatorPresenterReference.RequestTestNextExhibit();
    }

    /// <summary>
    /// Swaps the tracked interact target and triggers prompt updates when it changes.
    /// </summary>
    private void UpdateInteractTarget(Collider nextTarget)
    {
        if (currentInteractTarget == nextTarget)
        {
            return;
        }

        Collider previousTarget = currentInteractTarget;
        currentInteractTarget = nextTarget;
        HandleInteractTargetChanged(previousTarget, currentInteractTarget);
    }

    /// <summary>
    /// Shows or hides the prompt according to the current interactable state.
    /// </summary>
    private void HandleInteractTargetChanged(Collider previousTarget, Collider nextTarget)
    {
        _ = previousTarget;

        if (uiManagerReference == null)
        {
            return;
        }

        if (TryGetExhibitInteraction(nextTarget, out _)
            && (curatorPresenterReference == null || !curatorPresenterReference.IsPresentationActive()))
        {
            uiManagerReference.ShowPrompt("Press E to interact.");
            return;
        }

        uiManagerReference.HidePrompt();
    }

    /// <summary>
    /// Resolves the exhibit interaction bridge and forwards the request.
    /// </summary>
    private void RequestInteraction(Collider interactTarget)
    {
        if (curatorPresenterReference == null)
        {
            Debug.LogWarning("HO_PlayerController is missing a HO_CuratorPresenter reference.", this);
            return;
        }

        if (!TryGetExhibitInteraction(interactTarget, out HO_ExhibitInteraction exhibitInteraction))
        {
            Debug.Log("HO_PlayerController did not find a HO_ExhibitInteraction on the current interact target.", this);
            return;
        }

        exhibitInteraction.TryInteract(curatorPresenterReference);
    }

    /// <summary>
    /// Resolves exhibit interaction from the collider hierarchy.
    /// </summary>
    private static bool TryGetExhibitInteraction(Collider interactTarget, out HO_ExhibitInteraction exhibitInteraction)
    {
        exhibitInteraction = null;

        if (interactTarget == null)
        {
            return false;
        }

        exhibitInteraction = interactTarget.GetComponentInParent<HO_ExhibitInteraction>();
        return exhibitInteraction != null;
    }

    /// <summary>
    /// Clamps serialized values so the prototype stays in a safe tuning range.
    /// </summary>
    private void OnValidate()
    {
        followCameraDistance = Mathf.Max(0.5f, followCameraDistance);
        followCameraRadius = Mathf.Clamp(followCameraRadius, 0f, 1f);
        followCameraSide = Mathf.Clamp01(followCameraSide);
        followCameraVerticalArmLength = Mathf.Max(0f, followCameraVerticalArmLength);
        followCameraDamping.x = Mathf.Max(0f, followCameraDamping.x);
        followCameraDamping.y = Mathf.Max(0f, followCameraDamping.y);
        followCameraDamping.z = Mathf.Max(0f, followCameraDamping.z);
        followCameraDampingIntoCollision = Mathf.Clamp(followCameraDampingIntoCollision, 0f, 10f);
        followCameraDampingFromCollision = Mathf.Clamp(followCameraDampingFromCollision, 0f, 10f);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        mouseSensitivity = Mathf.Max(0f, mouseSensitivity);
        gravity = Mathf.Min(gravity, 0f);
        verticalLookLimit = Mathf.Clamp(verticalLookLimit, 0f, 89f);
        interactDistance = Mathf.Max(0f, interactDistance);
    }

    /// <summary>
    /// Converts Euler X rotation into a signed range for pitch accumulation.
    /// </summary>
    private static float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}
