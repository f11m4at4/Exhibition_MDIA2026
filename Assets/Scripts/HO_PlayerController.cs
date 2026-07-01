using UnityEngine;

/// <summary>
/// 플레이어 이동과 작품 상호작용 입력을 처리한다.
/// </summary>
public sealed class HO_PlayerController : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float verticalLookLimit = 75f;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayerMask;
    [SerializeField] private HO_UIManager uiManagerReference;
    [SerializeField] private HO_CuratorPresenter curatorPresenterReference;

    private float verticalLookAngle;
    private float verticalVelocity;
    private bool isControlLocked;
    private Collider currentInteractTarget;

    /// <summary>
    /// 필수 컴포넌트와 기본 참조를 확인하고 누락 시 안전하게 중단한다.
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

        if (cameraRoot != null)
        {
            verticalLookAngle = NormalizeAngle(cameraRoot.localEulerAngles.x);
        }
    }

    /// <summary>
    /// 입력 감지, 시점 회전, 이동 처리, 상호작용 입력을 단계별로 실행한다.
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
    /// 플레이어 조작 잠금 상태를 외부 시스템이 제어할 수 있게 한다.
    /// </summary>
    public void SetControlLocked(bool isLocked)
    {
        isControlLocked = isLocked;
    }

    /// <summary>
    /// 현재 조작 잠금 상태를 확인할 수 있게 한다.
    /// </summary>
    public bool IsControlLocked()
    {
        return isControlLocked;
    }

    /// <summary>
    /// 마우스 입력으로 플레이어 좌우 회전과 카메라 상하 회전을 분리해 처리한다.
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
    /// 키보드 축 입력과 중력을 적용해 CharacterController 이동을 처리한다.
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
    /// 조작이 잠겨도 바닥 판정이 유지되도록 중력만 별도로 적용한다.
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
    /// 전방 Raycast로 현재 상호작용 가능한 대상 1개만 유지한다.
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
    /// E 입력을 분리해 두고 이후 해설/UI Task와 연결할 호출 자리를 마련한다.
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
    /// 테스트용 Space 입력으로 작품 해설을 독립적으로 순환 호출한다.
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
    /// 상호작용 대상이 바뀔 때 이전 안내 정리와 새 안내 요청 지점을 분리한다.
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
    /// UI 매니저 연결 전까지는 대상 변경 분기 자리만 유지한다.
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
    /// 큐레이터/데이터 Task 연결 전까지는 상호작용 요청 자리만 유지한다.
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
    /// Resolves the exhibit interaction component from the hit collider so child colliders can still open the exhibit.
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
    /// 인스펙터 값이 과도하지 않도록 기본 범위를 보정한다.
    /// </summary>
    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        mouseSensitivity = Mathf.Max(0f, mouseSensitivity);
        gravity = Mathf.Min(gravity, 0f);
        verticalLookLimit = Mathf.Clamp(verticalLookLimit, 0f, 89f);
        interactDistance = Mathf.Max(0f, interactDistance);
    }

    /// <summary>
    /// Unity 각도를 상하 회전 누적에 쓰기 좋은 범위로 변환한다.
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
