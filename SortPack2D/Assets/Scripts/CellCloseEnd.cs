using UnityEngine;
using DG.Tweening;

/// <summary>
/// Script gắn vào CellCloseEnd PREFAB
/// Điều khiển animation cửa cuốn kéo xuống/lên
/// 
/// CÁCH DÙNG:
/// 1. Gắn script này vào CellCloseEnd prefab (parent object)
/// 2. Prefab sẽ được spawn bởi CellClearController
/// 3. Gọi PlayClose() để kéo cửa xuống
/// 4. Gọi PlayOpen() để kéo cửa lên
/// </summary>
public class CellCloseEnd : MonoBehaviour
{
    [Header("=== ANIMATION SETTINGS ===")]
    [SerializeField] private float closeDistance = 2.5f;      // Khoảng cách kéo xuống
    [SerializeField] private float closeDuration = 0.4f;      // Thời gian kéo xuống
    [SerializeField] private float openDuration = 0.3f;       // Thời gian kéo lên
    [SerializeField] private Ease closeEase = Ease.OutBounce; // Ease kéo xuống (nảy)
    [SerializeField] private Ease openEase = Ease.InQuad;     // Ease kéo lên

    [Header("=== AUDIO ===")]
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip openSound;

    // State
    private Vector3 openPosition;    // Vị trí mở (ẩn phía trên)
    private Vector3 closedPosition;  // Vị trí đóng (che cell)
    private bool isClosed = false;

    // Events
    public System.Action OnCloseComplete;
    public System.Action OnOpenComplete;

    void Awake()
    {
        // Lưu vị trí ban đầu là vị trí ĐÓNG
        closedPosition = transform.localPosition;

        // Vị trí MỞ là phía trên
        openPosition = closedPosition + Vector3.up * closeDistance;

        // Bắt đầu ở vị trí MỞ (ẩn phía trên)
        transform.localPosition = openPosition;
    }

    /// <summary>
    /// Kéo cửa XUỐNG (đóng)
    /// </summary>
    public void PlayClose(System.Action onComplete = null)
    {
        if (isClosed) return;

        Debug.Log($"[CellCloseEnd] Playing close animation...");

        // Sound
        if (closeSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(closeSound);
        }

        // Animation kéo xuống
        transform.DOKill();
        transform.DOLocalMove(closedPosition, closeDuration)
            .SetEase(closeEase)
            .OnComplete(() =>
            {
                isClosed = true;
                Debug.Log($"[CellCloseEnd] Close complete!");
                OnCloseComplete?.Invoke();
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// Kéo cửa LÊN (mở)
    /// </summary>
    public void PlayOpen(System.Action onComplete = null)
    {
        if (!isClosed) return;

        Debug.Log($"[CellCloseEnd] Playing open animation...");

        // Sound
        if (openSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(openSound);
        }

        // Animation kéo lên
        transform.DOKill();
        transform.DOLocalMove(openPosition, openDuration)
            .SetEase(openEase)
            .OnComplete(() =>
            {
                isClosed = false;
                Debug.Log($"[CellCloseEnd] Open complete!");
                OnOpenComplete?.Invoke();
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// Fade out và destroy
    /// </summary>
    public void FadeOutAndDestroy(float duration = 0.3f)
    {
        // Tìm tất cả renderers
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        Sequence fadeSeq = DOTween.Sequence();

        foreach (var r in renderers)
        {
            if (r is SpriteRenderer sr)
            {
                fadeSeq.Join(sr.DOFade(0f, duration));
            }
            else if (r is MeshRenderer mr)
            {
                foreach (var mat in mr.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        Color c = mat.color;
                        fadeSeq.Join(DOTween.To(
                            () => mat.color,
                            x => mat.color = x,
                            new Color(c.r, c.g, c.b, 0f),
                            duration
                        ));
                    }
                }
            }
        }

        fadeSeq.OnComplete(() => Destroy(gameObject));
    }

    /// <summary>
    /// Set vị trí đóng (gọi trước PlayClose nếu cần custom)
    /// </summary>
    public void SetClosedPosition(Vector3 localPos)
    {
        closedPosition = localPos;
        openPosition = closedPosition + Vector3.up * closeDistance;
        transform.localPosition = openPosition;
    }

    /// <summary>
    /// Set khoảng cách kéo
    /// </summary>
    public void SetCloseDistance(float distance)
    {
        closeDistance = distance;
        openPosition = closedPosition + Vector3.up * closeDistance;
    }

    void OnDestroy()
    {
        transform.DOKill();
    }

    // ==================== GETTERS ====================

    public bool IsClosed() => isClosed;
    public float GetCloseDuration() => closeDuration;
    public float GetOpenDuration() => openDuration;
}