using UnityEngine;
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

    [Header("Star UI")]
    [SerializeField] private Text starText;   // ⭐ kéo UI text của sao vào đây
    [SerializeField] private float starIncreaseSpeed = 0.2f;

    private int currentStar = 0;
    private Coroutine starRoutine;

    private GameObject settingsPanelInstance;
    private GameObject retryPanelInstance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddStar(int amount)
    {
        if (starRoutine != null)
            StopCoroutine(starRoutine);

        starRoutine = StartCoroutine(AnimateStarIncrease(amount));
    }

    private IEnumerator AnimateStarIncrease(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            currentStar++;
            if (starText != null)
                starText.text = currentStar.ToString();

            yield return new WaitForSeconds(starIncreaseSpeed);
        }
    }

    // ====================
    // CODE CŨ GIỮ NGUYÊN
    // ====================

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

    public void ShowRetryPanel()
    {
        if (retryPanelInstance == null)
        {
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
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
    }

    public void OnRetryHomeClicked()
    {
        SetPause(false);
        PlayClickSfx();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void OnRetryCloseClicked()
    {
        if (retryPanelInstance != null)
            retryPanelInstance.SetActive(false);

        SetPause(false);
        PlayClickSfx();
    }

    private void SetPause(bool pause)
    {
        Time.timeScale = pause ? 0f : 1f;
    }

    private void PlayClickSfx()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
    }
}
