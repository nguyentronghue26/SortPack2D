using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Canvas")]
    [SerializeField] private Transform canvasTransform;

    [Header("Prefabs")]
    [SerializeField] private GameObject settingsPanelPrefab;
    [SerializeField] private GameObject retryPanelPrefab;

    [Header("Options")]
    [SerializeField] private bool useTimeScalePause = true;

    [Header("Star UI")]
    [SerializeField] private Text starText;               // Text thường
    [SerializeField] private float starAnimDuration = 0.25f;
    [SerializeField] private AudioClip starGainClip;      // optional – có thì kéo vào

    private GameObject settingsPanelInstance;
    private GameObject retryPanelInstance;

    int starCount = 0;
    int displayedStar = 0;
    Coroutine starAnimRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Nếu muốn UIManager sống qua nhiều scene:
        // DontDestroyOnLoad(gameObject);

        // Init star UI
        if (starText != null)
            starText.text = "0";
    }

    // ==================== STAR ====================

    public void AddStars(int baseAmount)
    {
        if (baseAmount <= 0) return;

        int finalAmount = baseAmount;

        // Nếu có Double Star booster thì nhân lên
        if (BoosterManager.Instance != null)
            finalAmount = BoosterManager.Instance.ApplyStarMultiplier(baseAmount);

        int from = starCount;
        int to = starCount + finalAmount;
        starCount = to;

        if (starAnimRoutine != null)
            StopCoroutine(starAnimRoutine);

        starAnimRoutine = StartCoroutine(AnimateStarCount(from, to));

        // Play sound sao (nếu có clip)
        if (AudioManager.Instance != null && starGainClip != null)
            AudioManager.Instance.PlaySFX(starGainClip, 1f, false);
    }

    IEnumerator AnimateStarCount(int from, int to)
    {
        if (starText == null)
            yield break;

        float t = 0f;
        displayedStar = from;

        while (t < starAnimDuration)
        {
            // dùng unscaled để không phụ thuộc Time.timeScale
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / starAnimDuration);
            int current = Mathf.RoundToInt(Mathf.Lerp(from, to, lerp));

            if (current != displayedStar)
            {
                displayedStar = current;
                starText.text = displayedStar.ToString();
            }

            yield return null;
        }

        displayedStar = to;
        starText.text = displayedStar.ToString();
    }

    public int GetStarCount() => starCount;

    // ==================== SETTINGS ====================

    public void OnSettingsButtonClicked()
    {
        if (settingsPanelInstance == null)
        {
            if (settingsPanelPrefab == null || canvasTransform == null)
            {
                Debug.LogWarning("[GameUIManager] Chưa gán SettingsPanelPrefab hoặc CanvasTransform");
                return;
            }

            settingsPanelInstance = Instantiate(settingsPanelPrefab, canvasTransform);
        }

        settingsPanelInstance.SetActive(true);
        SetPause(true);
        PlayClickSfx();
    }

    public void OnSettingsCloseClicked()
    {
        if (settingsPanelInstance == null) return;

        settingsPanelInstance.SetActive(false);
        SetPause(false);
        PlayClickSfx();
    }

    // ==================== RETRY PANEL ====================

    public void ShowRetryPanel()
    {
        if (retryPanelInstance == null)
        {
            if (retryPanelPrefab == null || canvasTransform == null)
            {
                Debug.LogWarning("[GameUIManager] Chưa gán RetryPanelPrefab hoặc CanvasTransform");
                return;
            }

            retryPanelInstance = Instantiate(retryPanelPrefab, canvasTransform);
        }

        retryPanelInstance.SetActive(true);
        SetPause(true);
        PlayClickSfx();
    }

    public void OnRetryConfirmClicked()
    {
        SetPause(false);
        PlayClickSfx();

        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    public void OnRetryHomeClicked()
    {
        SetPause(false);
        PlayClickSfx();
        SceneManager.LoadScene("MainMenu");
    }

    public void OnRetryCloseClicked()
    {
        if (retryPanelInstance != null)
            retryPanelInstance.SetActive(false);

        SetPause(false);
        PlayClickSfx();
    }

    // ==================== HELPER ====================

    private void SetPause(bool pause)
    {
        if (!useTimeScalePause) return;
        Time.timeScale = pause ? 0f : 1f;
    }

    private void PlayClickSfx()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
    }
}
