using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI cho 1 booster button
/// Gắn vào mỗi Booster Button
/// </summary>
public class BoosterButton : MonoBehaviour
{
    [Header("=== CONFIG ===")]
    [SerializeField] private BoosterType boosterType;

    [Header("=== UI REFERENCES ===")]
    [SerializeField] private Button button;
    [SerializeField] private Text countText;  // Text thường
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private GameObject activeIndicator;

    [Header("=== VISUALS ===")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color disabledColor = Color.gray;
    [SerializeField] private Color activeColor = Color.yellow;

    void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        button?.onClick.AddListener(OnClick);

        if (BoosterManager.Instance != null)
        {
            BoosterManager.Instance.OnBoosterCountChanged += OnBoosterCountChanged;
            BoosterManager.Instance.OnFreeTimeStarted += OnFreeTimeStarted;
            BoosterManager.Instance.OnFreeTimeUpdate += OnFreeTimeUpdate;
            BoosterManager.Instance.OnFreeTimeEnded += OnFreeTimeEnded;
            BoosterManager.Instance.OnDoubleStarStarted += OnDoubleStarStarted;
            BoosterManager.Instance.OnDoubleStarUpdate += OnDoubleStarUpdate;
            BoosterManager.Instance.OnDoubleStarEnded += OnDoubleStarEnded;
        }

        UpdateUI();
    }

    void OnDestroy()
    {
        if (BoosterManager.Instance != null)
        {
            BoosterManager.Instance.OnBoosterCountChanged -= OnBoosterCountChanged;
            BoosterManager.Instance.OnFreeTimeStarted -= OnFreeTimeStarted;
            BoosterManager.Instance.OnFreeTimeUpdate -= OnFreeTimeUpdate;
            BoosterManager.Instance.OnFreeTimeEnded -= OnFreeTimeEnded;
            BoosterManager.Instance.OnDoubleStarStarted -= OnDoubleStarStarted;
            BoosterManager.Instance.OnDoubleStarUpdate -= OnDoubleStarUpdate;
            BoosterManager.Instance.OnDoubleStarEnded -= OnDoubleStarEnded;
        }
    }

    private void OnClick()
    {
        if (BoosterManager.Instance == null) return;

        // Play sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPickUp();

        // Use booster
        BoosterManager.Instance.UseBooster(boosterType);
    }

    private void OnBoosterCountChanged(BoosterType type, int newCount)
    {
        if (type == boosterType)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (BoosterManager.Instance == null) return;

        int count = BoosterManager.Instance.GetBoosterCount(boosterType);

        if (countText != null)
            countText.text = count.ToString();

        bool canUse = count > 0;

        if (boosterType == BoosterType.FreeTime && BoosterManager.Instance.IsFreeTimeActive())
            canUse = false;
        if (boosterType == BoosterType.DoubleStar && BoosterManager.Instance.IsDoubleStarActive())
            canUse = false;

        if (button != null)
            button.interactable = canUse;

        if (iconImage != null)
            iconImage.color = canUse ? normalColor : disabledColor;
    }

    private void OnFreeTimeStarted(float duration)
    {
        if (boosterType != BoosterType.FreeTime) return;

        if (activeIndicator != null)
            activeIndicator.SetActive(true);

        if (iconImage != null)
            iconImage.color = activeColor;

        if (button != null)
            button.interactable = false;
    }

    private void OnFreeTimeUpdate(float remaining)
    {
        if (boosterType != BoosterType.FreeTime) return;
    }

    private void OnFreeTimeEnded()
    {
        if (boosterType != BoosterType.FreeTime) return;

        if (activeIndicator != null)
            activeIndicator.SetActive(false);

        UpdateUI();
    }

    private void OnDoubleStarStarted()
    {
        if (boosterType != BoosterType.DoubleStar) return;

        if (activeIndicator != null)
            activeIndicator.SetActive(true);

        if (iconImage != null)
            iconImage.color = activeColor;

        if (button != null)
            button.interactable = false;
    }

    private void OnDoubleStarUpdate(float remaining)
    {
        if (boosterType != BoosterType.DoubleStar) return;
    }

    private void OnDoubleStarEnded()
    {
        if (boosterType != BoosterType.DoubleStar) return;

        if (activeIndicator != null)
            activeIndicator.SetActive(false);

        UpdateUI();
    }
}