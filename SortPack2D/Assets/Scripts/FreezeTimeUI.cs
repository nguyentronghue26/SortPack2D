using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Điều khiển animation FreezeTime khi dùng booster
/// - Image background luôn hiện
/// - Chỉ bật/tắt animation
/// </summary>
public class FreezeTimeUI : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    [SerializeField] private Animator animator;
    
    [Header("=== ANIMATION NAMES ===")]
    [SerializeField] private string freezeAnimName = "FreezeTime";

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        
        // Tắt animator ban đầu (giữ nguyên image)
        if (animator != null)
            animator.enabled = false;
        
        // Subscribe events
        if (BoosterManager.Instance != null)
        {
            BoosterManager.Instance.OnFreeTimeStarted += OnFreeTimeStarted;
            BoosterManager.Instance.OnFreeTimeEnded += OnFreeTimeEnded;
        }
    }

    void OnDestroy()
    {
        if (BoosterManager.Instance != null)
        {
            BoosterManager.Instance.OnFreeTimeStarted -= OnFreeTimeStarted;
            BoosterManager.Instance.OnFreeTimeEnded -= OnFreeTimeEnded;
        }
    }

    private void OnFreeTimeStarted(float duration)
    {
        Debug.Log($"[FreezeTimeUI] Starting animation");
        
        // Bật animator và chạy animation
        if (animator != null)
        {
            animator.enabled = true;
            animator.Play(freezeAnimName, 0, 0f);
        }
    }

    private void OnFreeTimeEnded()
    {
        Debug.Log("[FreezeTimeUI] Stopping animation");
        
        // Tắt animator (image vẫn hiện)
        if (animator != null)
        {
            animator.enabled = false;
        }
    }
}