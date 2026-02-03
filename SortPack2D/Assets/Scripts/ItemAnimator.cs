using UnityEngine;
using DG.Tweening;

public class ItemAnimator : MonoBehaviour
{
    [Header("Pick Up Animation")]
    [SerializeField] private float pickUpScale = 1.15f;
    [SerializeField] private float pickUpDuration = 0.15f;
    [SerializeField] private Ease pickUpEase = Ease.OutBack;

    [Header("Drop Animation")]
    [SerializeField] private float dropDuration = 0.3f;
    [SerializeField] private float squashScaleY = 0.7f;
    [SerializeField] private float stretchScaleY = 1.1f;
    [SerializeField] private Ease dropEase = Ease.OutBounce;

    [Header("Merge Animation")]
    [SerializeField] private float mergeDuration = 0.4f;
    [SerializeField] private float mergeScaleUp = 1.3f;
    [SerializeField] private float mergeRotation = 360f;
    [SerializeField] private Ease mergeEase = Ease.InBack;

    [Header("Idle Animation")]
    [SerializeField] private bool enableIdleAnimation = false;  // Tắt mặc định
    [SerializeField] private float idleBounceDuration = 2f;
    [SerializeField] private float idleBounceAmount = 0.05f;

    [Header("Hover Animation")]
    [SerializeField] private float hoverFloatAmount = 0.1f;
    [SerializeField] private float hoverFloatDuration = 0.5f;

    [Header("Shake Animation")]
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeStrength = 0.1f;

    // Cache
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Tweener idleTween;
    private Tweener hoverTween;

    void Start()
    {
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
        // Không auto idle
    }

    void OnDestroy()
    {
        transform.DOKill();
    }

    // ========== PICK UP ==========
    public void PlayPickUp()
    {
        // Nếu sau này muốn có hiệu ứng nhấc lên, nhớ dùng scale hiện tại làm gốc:
        // transform.DOKill();
        // Vector3 baseScale = transform.localScale;
        // transform.DOScale(baseScale * pickUpScale, pickUpDuration)
        //          .SetEase(pickUpEase);
    }

    // ========== DROP (CÓ DI CHUYỂN) ==========
    public void PlayDrop(Vector3 targetPosition, System.Action onComplete = null)
    {
        transform.DOKill();

        // *** LẤY SCALE HIỆN TẠI LÀM GỐC ***
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;

        Sequence dropSequence = DOTween.Sequence();

        // Di chuyển đến vị trí
        dropSequence.Append(
            transform.DOLocalMove(targetPosition, dropDuration * 0.5f)
                .SetEase(Ease.OutQuad)
        );

        // Squash khi chạm
        dropSequence.Append(
            transform.DOScale(
                new Vector3(
                    originalScale.x * 1.2f,
                    originalScale.y * squashScaleY,
                    originalScale.z),
                dropDuration * 0.15f
            ).SetEase(Ease.OutQuad)
        );

        // Stretch lên
        dropSequence.Append(
            transform.DOScale(
                new Vector3(
                    originalScale.x * 0.9f,
                    originalScale.y * stretchScaleY,
                    originalScale.z),
                dropDuration * 0.15f
            ).SetEase(Ease.OutQuad)
        );

        // Về scale gốc với bounce
        dropSequence.Append(
            transform.DOScale(originalScale, dropDuration * 0.2f)
                .SetEase(Ease.OutBounce)
        );

        dropSequence.OnComplete(() =>
        {
            onComplete?.Invoke();
            if (enableIdleAnimation) StartIdleAnimation();
        });
    }

    // ========== DROP SIMPLE (KHÔNG DI CHUYỂN) ==========
    public void PlayDropBounce()
    {
        transform.DOKill();

        // *** LẤY SCALE HIỆN TẠI LÀM GỐC ***
        originalScale = transform.localScale;

        Sequence bounceSequence = DOTween.Sequence();

        // Squash
        bounceSequence.Append(
            transform.DOScale(
                new Vector3(
                    originalScale.x * 1.15f,
                    originalScale.y * squashScaleY,
                    originalScale.z),
                0.1f
            ).SetEase(Ease.OutQuad)
        );

        // Stretch
        bounceSequence.Append(
            transform.DOScale(
                new Vector3(
                    originalScale.x * 0.92f,
                    originalScale.y * stretchScaleY,
                    originalScale.z),
                0.1f
            ).SetEase(Ease.OutQuad)
        );

        // Back to normal
        bounceSequence.Append(
            transform.DOScale(originalScale, 0.15f)
                .SetEase(Ease.OutBounce)
        );
    }

    // ========== MERGE ==========
    public void PlayMerge(Vector3 mergeCenter, System.Action onComplete = null)
    {
        transform.DOKill();

        // *** BASE SCALE HIỆN TẠI ***
        originalScale = transform.localScale;

        Sequence mergeSequence = DOTween.Sequence();

        // Bay về trung tâm + xoay + scale up
        mergeSequence.Append(
            transform.DOMove(mergeCenter, mergeDuration * 0.6f)
                .SetEase(Ease.InQuad)
        );

        mergeSequence.Join(
            transform.DORotate(
                new Vector3(0, 0, mergeRotation),
                mergeDuration * 0.6f,
                RotateMode.FastBeyond360)
                .SetEase(Ease.InQuad)
        );

        mergeSequence.Join(
            transform.DOScale(originalScale * mergeScaleUp, mergeDuration * 0.6f)
                .SetEase(Ease.OutQuad)
        );

        // Scale về 0 và biến mất
        mergeSequence.Append(
            transform.DOScale(Vector3.zero, mergeDuration * 0.4f)
                .SetEase(mergeEase)
        );

        mergeSequence.OnComplete(() => { onComplete?.Invoke(); });
    }

    // ========== MERGE SIMPLE ==========
    public void PlayMergeSimple(System.Action onComplete = null)
    {
        transform.DOKill();

        originalScale = transform.localScale;

        Sequence mergeSequence = DOTween.Sequence();

        // Scale up
        mergeSequence.Append(
            transform.DOScale(originalScale * mergeScaleUp, mergeDuration * 0.4f)
                .SetEase(Ease.OutQuad)
        );

        // Xoay nhẹ
        mergeSequence.Join(
            transform.DORotate(new Vector3(0, 0, 15f), mergeDuration * 0.4f)
                .SetEase(Ease.OutQuad)
        );

        // Scale về 0
        mergeSequence.Append(
            transform.DOScale(Vector3.zero, mergeDuration * 0.6f)
                .SetEase(mergeEase)
        );

        mergeSequence.OnComplete(() => { onComplete?.Invoke(); });
    }

    // ========== IDLE ANIMATION ==========
    public void StartIdleAnimation()
    {
        if (!enableIdleAnimation) return;

        idleTween?.Kill();

        originalPosition = transform.localPosition;

        idleTween = transform.DOLocalMoveY(
                originalPosition.y + idleBounceAmount,
                idleBounceDuration
            )
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void StopIdleAnimation()
    {
        idleTween?.Kill();
        transform.localPosition = originalPosition;
    }

    // ========== HOVER ==========
    public void PlayHoverEnter()
    {
        hoverTween?.Kill();

        originalPosition = transform.localPosition;

        hoverTween = transform.DOLocalMoveY(
                transform.localPosition.y + hoverFloatAmount,
                hoverFloatDuration
            )
            .SetEase(Ease.OutQuad)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void PlayHoverExit()
    {
        hoverTween?.Kill();
        transform.DOLocalMoveY(originalPosition.y, hoverFloatDuration * 0.5f)
            .SetEase(Ease.OutQuad);
    }

    // ========== SHAKE ==========
    public void PlayShake()
    {
        transform.DOKill();

        transform.DOShakePosition(
                shakeDuration,
                shakeStrength,
                20,
                90,
                false,
                true
            )
            .OnComplete(() =>
            {
                if (enableIdleAnimation) StartIdleAnimation();
            });
    }

    // ========== SPAWN ==========
    public void PlaySpawn()
    {
        transform.DOKill();

        // *** DÙNG SCALE HIỆN TẠI LÀ TARGET ***
        Vector3 targetScale = transform.localScale;
        originalScale = targetScale;

        transform.localScale = Vector3.zero;

        transform.DOScale(targetScale, 0.4f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                if (enableIdleAnimation) StartIdleAnimation();
            });
    }

    // ========== WOBBLE ==========
    public void PlayWobble()
    {
        transform.DOKill();

        Sequence wobbleSequence = DOTween.Sequence();

        wobbleSequence.Append(
            transform.DORotate(new Vector3(0, 0, 5f), 0.1f)
                .SetEase(Ease.OutQuad)
        );

        wobbleSequence.Append(
            transform.DORotate(new Vector3(0, 0, -5f), 0.1f)
                .SetEase(Ease.OutQuad)
        );

        wobbleSequence.Append(
            transform.DORotate(new Vector3(0, 0, 3f), 0.08f)
                .SetEase(Ease.OutQuad)
        );

        wobbleSequence.Append(
            transform.DORotate(new Vector3(0, 0, 0f), 0.08f)
                .SetEase(Ease.OutQuad)
        );

        wobbleSequence.OnComplete(() =>
        {
            if (enableIdleAnimation) StartIdleAnimation();
        });
    }

    // ========== UTILITIES ==========
    public void ResetToOriginal()
    {
        transform.DOKill();
        transform.localScale = originalScale;
        transform.localPosition = originalPosition;
        transform.localRotation = Quaternion.identity;
    }

    public void SetOriginalScale(Vector3 scale) { originalScale = scale; }
    public void SetOriginalPosition(Vector3 pos) { originalPosition = pos; }
}
