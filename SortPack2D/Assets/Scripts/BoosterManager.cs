using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public enum BoosterType
{
    FreeTime,
    AutoMerge,
    DoubleStar,
    RandomSwap
}

public class BoosterManager : MonoBehaviour
{
    public static BoosterManager Instance { get; private set; }

    [Header("=== BOOSTER COUNTS ===")]
    [SerializeField] private int freeTimeCount = 3;
    [SerializeField] private int autoMergeCount = 3;
    [SerializeField] private int doubleStarCount = 3;
    [SerializeField] private int randomSwapCount = 3;

    [Header("=== FREE TIME CONFIG ===")]
    [SerializeField] private float freeTimeDuration = 5f;

    [Header("=== DOUBLE STAR CONFIG ===")]
    [SerializeField] private float doubleStarDuration = 10f;
    [SerializeField] private int starMultiplier = 2;

    [Header("=== AUTO MERGE CONFIG ===")]
    [SerializeField] private float mergeMoveDuration = 0.3f;
    [SerializeField] private float mergeScaleDuration = 0.2f;
    [SerializeField] private Ease mergeEase = Ease.InBack;
    [SerializeField] private int mergeBaseScore = 100;

    [Header("=== REFERENCES ===")]
    [SerializeField] private CountdownTimer countdownTimer;
    [SerializeField] private GridSpawner gridSpawner;

    // States
    private bool isFreeTimeActive = false;
    private bool isDoubleStarActive = false;
    private bool isAutoMerging = false;
    private Coroutine freeTimeCoroutine;
    private Coroutine doubleStarCoroutine;

    // Events
    public System.Action<BoosterType, int> OnBoosterCountChanged;
    public System.Action<BoosterType> OnBoosterUsed;
    public System.Action<BoosterType> OnBoosterFailed;

    public System.Action<float> OnFreeTimeStarted;
    public System.Action<float> OnFreeTimeUpdate;
    public System.Action OnFreeTimeEnded;

    public System.Action OnDoubleStarStarted;
    public System.Action<float> OnDoubleStarUpdate;
    public System.Action OnDoubleStarEnded;

    public System.Action<List<Item>> OnAutoMergeStarted;
    public System.Action<int> OnAutoMergeComplete;
    public System.Action OnAutoMergeFailed;

    public System.Action<Item, Item> OnRandomSwapComplete;
    public System.Action OnRandomSwapFailed;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (countdownTimer == null)
            countdownTimer = FindObjectOfType<CountdownTimer>();

        if (gridSpawner == null)
            gridSpawner = FindObjectOfType<GridSpawner>();
    }

    // ==================== PUBLIC API ====================

    public void UseBooster(BoosterType type)
    {
        switch (type)
        {
            case BoosterType.FreeTime: UseFreeTime(); break;
            case BoosterType.AutoMerge: UseAutoMerge(); break;
            case BoosterType.DoubleStar: UseDoubleStar(); break;
            case BoosterType.RandomSwap: UseRandomSwap(); break;
        }
    }

    public bool HasBooster(BoosterType type) => GetBoosterCount(type) > 0;

    public int GetBoosterCount(BoosterType type)
    {
        switch (type)
        {
            case BoosterType.FreeTime: return freeTimeCount;
            case BoosterType.AutoMerge: return autoMergeCount;
            case BoosterType.DoubleStar: return doubleStarCount;
            case BoosterType.RandomSwap: return randomSwapCount;
            default: return 0;
        }
    }

    public void AddBooster(BoosterType type, int amount)
    {
        if (amount <= 0) return;

        switch (type)
        {
            case BoosterType.FreeTime:
                freeTimeCount += amount;
                OnBoosterCountChanged?.Invoke(type, freeTimeCount);
                break;
            case BoosterType.AutoMerge:
                autoMergeCount += amount;
                OnBoosterCountChanged?.Invoke(type, autoMergeCount);
                break;
            case BoosterType.DoubleStar:
                doubleStarCount += amount;
                OnBoosterCountChanged?.Invoke(type, doubleStarCount);
                break;
            case BoosterType.RandomSwap:
                randomSwapCount += amount;
                OnBoosterCountChanged?.Invoke(type, randomSwapCount);
                break;
        }
    }

    // ==================== 1. FREE TIME ====================

    public void UseFreeTime()
    {
        if (freeTimeCount <= 0)
        {
            OnBoosterFailed?.Invoke(BoosterType.FreeTime);
            return;
        }

        if (isFreeTimeActive) return;

        freeTimeCount--;
        OnBoosterCountChanged?.Invoke(BoosterType.FreeTime, freeTimeCount);
        OnBoosterUsed?.Invoke(BoosterType.FreeTime);

        // Play booster sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPickUp();

        if (freeTimeCoroutine != null)
            StopCoroutine(freeTimeCoroutine);

        freeTimeCoroutine = StartCoroutine(FreeTimeCoroutine());
    }

    private IEnumerator FreeTimeCoroutine()
    {
        isFreeTimeActive = true;

        if (countdownTimer != null)
            countdownTimer.PauseTimer();

        OnFreeTimeStarted?.Invoke(freeTimeDuration);

        float remaining = freeTimeDuration;
        while (remaining > 0)
        {
            remaining -= Time.deltaTime;
            OnFreeTimeUpdate?.Invoke(remaining);
            yield return null;
        }

        if (countdownTimer != null)
            countdownTimer.ResumeTimer();

        isFreeTimeActive = false;
        OnFreeTimeEnded?.Invoke();
    }

    public bool IsFreeTimeActive() => isFreeTimeActive;

    // ==================== 2. AUTO MERGE ====================

    public void UseAutoMerge()
    {
        if (autoMergeCount <= 0)
        {
            OnAutoMergeFailed?.Invoke();
            return;
        }

        if (isAutoMerging) return;

        List<Item> itemsToMerge = FindThreeSameTypeItems();

        if (itemsToMerge == null || itemsToMerge.Count < 3)
        {
            OnAutoMergeFailed?.Invoke();
            return;
        }

        autoMergeCount--;
        OnBoosterCountChanged?.Invoke(BoosterType.AutoMerge, autoMergeCount);
        OnBoosterUsed?.Invoke(BoosterType.AutoMerge);

        StartCoroutine(PerformAutoMerge(itemsToMerge));
    }

    private List<Item> FindThreeSameTypeItems()
    {
        List<Item> allItems = GetAllItems();
        if (allItems.Count < 3) return null;

        Dictionary<string, List<Item>> itemsByType = new Dictionary<string, List<Item>>();

        foreach (var item in allItems)
        {
            if (string.IsNullOrEmpty(item.itemType)) continue;

            if (!itemsByType.ContainsKey(item.itemType))
                itemsByType[item.itemType] = new List<Item>();

            itemsByType[item.itemType].Add(item);
        }

        string bestType = null;
        int bestCount = 0;

        foreach (var kvp in itemsByType)
        {
            if (kvp.Value.Count >= 3)
            {
                if (kvp.Value.Count == 3)
                {
                    bestType = kvp.Key;
                    break;
                }

                if (kvp.Value.Count > bestCount)
                {
                    bestCount = kvp.Value.Count;
                    bestType = kvp.Key;
                }
            }
        }

        if (bestType == null) return null;

        return itemsByType[bestType].Take(3).ToList();
    }

    private IEnumerator PerformAutoMerge(List<Item> items)
    {
        isAutoMerging = true;
        OnAutoMergeStarted?.Invoke(items);

        // Tính center
        Vector3 centerPos = Vector3.zero;
        foreach (var item in items)
            centerPos += item.transform.position;
        centerPos /= items.Count;

        // Disable colliders
        foreach (var item in items)
        {
            var col = item.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }

        // Remove từ cells
        foreach (var item in items)
        {
            Cell cell = item.GetCurrentCell();
            if (cell != null)
                cell.RemoveItemWithoutNotify(item);
            item.transform.SetParent(null);
        }

        // Animation: Bay về center
        Sequence moveSeq = DOTween.Sequence();
        foreach (var item in items)
        {
            moveSeq.Join(item.transform.DOMove(centerPos, mergeMoveDuration).SetEase(Ease.InQuad));
            moveSeq.Join(item.transform.DORotate(new Vector3(0, 0, 360), mergeMoveDuration, RotateMode.FastBeyond360).SetEase(Ease.InQuad));
        }

        yield return moveSeq.WaitForCompletion();

        // *** PLAY MERGE SOUND ***
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMerge();

        // Animation: Scale về 0
        Sequence scaleSeq = DOTween.Sequence();
        foreach (var item in items)
        {
            scaleSeq.Join(item.transform.DOScale(Vector3.zero, mergeScaleDuration).SetEase(mergeEase));
        }

        yield return scaleSeq.WaitForCompletion();

        // Destroy
        foreach (var item in items)
        {
            if (item != null)
                Destroy(item.gameObject);
        }

        // *** TÍNH ĐIỂM VỚI X2 NẾU DOUBLE STAR ACTIVE ***
        int score = mergeBaseScore;
        score = ApplyStarMultiplier(score);

        // TODO: Gọi GameManager để cộng điểm
        // GameManager.Instance?.AddScore(score);

        Debug.Log($"[BoosterManager] AutoMerge complete! Score: {score} (x2: {isDoubleStarActive})");

        isAutoMerging = false;
        OnAutoMergeComplete?.Invoke(score);
    }

    // ==================== 3. DOUBLE STAR ====================

    public void UseDoubleStar()
    {
        if (doubleStarCount <= 0)
        {
            OnBoosterFailed?.Invoke(BoosterType.DoubleStar);
            return;
        }

        if (isDoubleStarActive) return;

        doubleStarCount--;
        OnBoosterCountChanged?.Invoke(BoosterType.DoubleStar, doubleStarCount);
        OnBoosterUsed?.Invoke(BoosterType.DoubleStar);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPickUp();

        if (doubleStarCoroutine != null)
            StopCoroutine(doubleStarCoroutine);

        doubleStarCoroutine = StartCoroutine(DoubleStarCoroutine());
    }

    private IEnumerator DoubleStarCoroutine()
    {
        isDoubleStarActive = true;
        OnDoubleStarStarted?.Invoke();

        Debug.Log("[BoosterManager] DoubleStar ACTIVATED!");

        float remaining = doubleStarDuration;
        while (remaining > 0)
        {
            remaining -= Time.deltaTime;
            OnDoubleStarUpdate?.Invoke(remaining);
            yield return null;
        }

        isDoubleStarActive = false;
        OnDoubleStarEnded?.Invoke();

        Debug.Log("[BoosterManager] DoubleStar ENDED!");
    }

    /// <summary>
    /// Gọi method này để tính điểm với x2 multiplier
    /// GameManager nên gọi method này khi tính score
    /// </summary>
    public int ApplyStarMultiplier(int baseScore)
    {
        if (isDoubleStarActive)
        {
            Debug.Log($"[BoosterManager] Applying x{starMultiplier}: {baseScore} -> {baseScore * starMultiplier}");
            return baseScore * starMultiplier;
        }
        return baseScore;
    }

    public bool IsDoubleStarActive() => isDoubleStarActive;

    // ==================== 4. RANDOM SWAP ====================

    public void UseRandomSwap()
    {
        if (randomSwapCount <= 0)
        {
            OnRandomSwapFailed?.Invoke();
            return;
        }

        List<Item> allItems = GetAllItems();

        if (allItems.Count < 2)
        {
            OnRandomSwapFailed?.Invoke();
            return;
        }

        randomSwapCount--;
        OnBoosterCountChanged?.Invoke(BoosterType.RandomSwap, randomSwapCount);
        OnBoosterUsed?.Invoke(BoosterType.RandomSwap);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPickUp();

        List<Item> shuffled = allItems.OrderBy(x => Random.value).ToList();
        Item item1 = shuffled[0];
        Item item2 = null;

        foreach (var item in shuffled)
        {
            if (item != item1 && item.itemType != item1.itemType)
            {
                item2 = item;
                break;
            }
        }

        if (item2 == null && shuffled.Count > 1)
            item2 = shuffled[1];

        if (item1 == null || item2 == null)
        {
            OnRandomSwapFailed?.Invoke();
            return;
        }

        StartCoroutine(PerformSwap(item1, item2));
    }

    private IEnumerator PerformSwap(Item item1, Item item2)
    {
        Cell cell1 = item1.GetCurrentCell();
        Cell cell2 = item2.GetCurrentCell();
        int spot1 = item1.GetSpotIndex();
        int spot2 = item2.GetSpotIndex();

        if (cell1 == null || cell2 == null) yield break;

        Vector3 pos1 = item1.transform.position;
        Vector3 pos2 = item2.transform.position;
        Vector3 scale1 = item1.transform.localScale;
        Vector3 scale2 = item2.transform.localScale;

        cell1.RemoveItem(item1);
        cell2.RemoveItem(item2);

        float duration = 0.3f;
        Vector3 midPoint = (pos1 + pos2) / 2f + Vector3.up * 0.5f;

        Sequence swapSeq = DOTween.Sequence();
        swapSeq.Join(item1.transform.DOPath(
            new Vector3[] { pos1, midPoint + Vector3.left * 0.3f, pos2 },
            duration, PathType.CatmullRom).SetEase(Ease.InOutQuad));
        swapSeq.Join(item2.transform.DOPath(
            new Vector3[] { pos2, midPoint + Vector3.right * 0.3f, pos1 },
            duration, PathType.CatmullRom).SetEase(Ease.InOutQuad));

        yield return swapSeq.WaitForCompletion();

        cell2.AddItemToSpot(item1, spot2);
        cell1.AddItemToSpot(item2, spot1);

        item1.transform.localScale = scale1;
        item2.transform.localScale = scale2;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayDrop();

        OnRandomSwapComplete?.Invoke(item1, item2);

        cell1.CheckSorted();
        cell2.CheckSorted();
    }

    // ==================== HELPERS ====================

    private List<Item> GetAllItems()
    {
        List<Item> items = new List<Item>();

        if (gridSpawner == null)
            items.AddRange(FindObjectsOfType<Item>());
        else
        {
            Cell[] allCells = gridSpawner.GetComponentsInChildren<Cell>();
            foreach (var cell in allCells)
                items.AddRange(cell.GetItems());
        }

        return items;
    }

    // ==================== SAVE/LOAD ====================

    public void SaveBoosterCounts()
    {
        PlayerPrefs.SetInt("Booster_FreeTime", freeTimeCount);
        PlayerPrefs.SetInt("Booster_AutoMerge", autoMergeCount);
        PlayerPrefs.SetInt("Booster_DoubleStar", doubleStarCount);
        PlayerPrefs.SetInt("Booster_RandomSwap", randomSwapCount);
        PlayerPrefs.Save();
    }

    public void LoadBoosterCounts()
    {
        freeTimeCount = PlayerPrefs.GetInt("Booster_FreeTime", 3);
        autoMergeCount = PlayerPrefs.GetInt("Booster_AutoMerge", 3);
        doubleStarCount = PlayerPrefs.GetInt("Booster_DoubleStar", 3);
        randomSwapCount = PlayerPrefs.GetInt("Booster_RandomSwap", 3);
    }
}