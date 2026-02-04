using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Controller gắn vào CELL prefab
/// </summary>
public class CellClearController : MonoBehaviour
{
    [Header("=== PREFAB ===")]
    [SerializeField] private GameObject cellCloseEndPrefab;

    [Header("=== SPAWN POSITION ===")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 0, -0.5f);

    [Header("=== TIMING ===")]
    [SerializeField] private float delayBeforeClose = 0.1f;
    [SerializeField] private float stayClosedDuration = 0.5f;
    [SerializeField] private float delayBeforeFly = 0.1f;

    [Header("=== AFTER CLEAR ===")]
    [SerializeField] private bool openDoorAfterClear = true;

    [Header("=== FLY ITEMS ===")]
    [SerializeField] private float flyZOffset = -2f;
    [SerializeField] private float flyDuration = 0.3f;
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private float slideDistance = 10f;
    [SerializeField] private Ease flyEase = Ease.OutQuad;
    [SerializeField] private Ease slideEase = Ease.InQuad;

    [Header("=== AUDIO ===")]
    [SerializeField] private AudioClip flyAwaySound;

    // References
    private Cell cell;
    private CellCloseEnd currentCloseEnd;
    private bool isProcessing = false;

    public System.Action<Cell> OnClearComplete;

    void Awake()
    {
        cell = GetComponent<Cell>();
        Debug.Log($"[CellClearController] Awake: cell = {(cell != null ? cell.name : "NULL")}");
    }

    void Start()
    {
        if (cell != null)
        {
            cell.OnCellSorted += HandleCellSorted;
            Debug.Log($"[CellClearController] {name}: Subscribed to OnCellSorted");
        }
        else
        {
            Debug.LogError($"[CellClearController] {name}: Cell component is NULL!");
        }

        // Check prefab
        if (cellCloseEndPrefab == null)
        {
            Debug.LogError($"[CellClearController] {name}: cellCloseEndPrefab is NULL! DRAG PREFAB TO INSPECTOR!");
        }
        else
        {
            Debug.Log($"[CellClearController] {name}: cellCloseEndPrefab = {cellCloseEndPrefab.name}");
        }
    }

    void OnDestroy()
    {
        if (cell != null)
        {
            cell.OnCellSorted -= HandleCellSorted;
        }

        if (currentCloseEnd != null)
        {
            Destroy(currentCloseEnd.gameObject);
        }
    }

    private void HandleCellSorted(Cell sortedCell)
    {
        Debug.Log($"[CellClearController] HandleCellSorted called! sortedCell={sortedCell.name}, this.cell={cell.name}");

        if (sortedCell != cell)
        {
            Debug.Log($"[CellClearController] Not my cell, ignoring");
            return;
        }

        if (isProcessing)
        {
            Debug.Log($"[CellClearController] Already processing, ignoring");
            return;
        }

        Debug.Log($"[CellClearController] {name}: *** STARTING CLEAR SEQUENCE ***");
        StartCoroutine(ClearSequence());
    }

    private IEnumerator ClearSequence()
    {
        isProcessing = true;

        Debug.Log($"[CellClearController] Step 1: Waiting {delayBeforeClose}s before spawn...");
        yield return new WaitForSeconds(delayBeforeClose);

        // ========== SPAWN ==========
        Debug.Log($"[CellClearController] Step 2: Spawning CellCloseEnd...");
        SpawnCloseEnd();

        if (currentCloseEnd == null)
        {
            Debug.LogError($"[CellClearController] FAILED to spawn! Aborting.");
            isProcessing = false;
            yield break;
        }

        // ========== CLOSE ==========
        Debug.Log($"[CellClearController] Step 3: Playing close animation...");
        bool closeComplete = false;
        currentCloseEnd.PlayClose(() => {
            closeComplete = true;
            Debug.Log($"[CellClearController] Close animation DONE!");
        });

        yield return new WaitUntil(() => closeComplete);

        // ========== STAY ==========
        Debug.Log($"[CellClearController] Step 4: Staying closed for {stayClosedDuration}s...");
        yield return new WaitForSeconds(stayClosedDuration);

        // ========== FLY ==========
        Debug.Log($"[CellClearController] Step 5: Flying items away...");
        yield return StartCoroutine(FlyItemsAway());

        // ========== OPEN/DESTROY ==========
        if (openDoorAfterClear)
        {
            Debug.Log($"[CellClearController] Step 6: Opening door...");
            bool openComplete = false;
            currentCloseEnd.PlayOpen(() => {
                openComplete = true;
                Debug.Log($"[CellClearController] Open animation DONE!");
            });
            yield return new WaitUntil(() => openComplete);

            Destroy(currentCloseEnd.gameObject);
        }
        else
        {
            Debug.Log($"[CellClearController] Step 6: Fading out...");
            currentCloseEnd.FadeOutAndDestroy(0.3f);
            yield return new WaitForSeconds(0.3f);
        }

        currentCloseEnd = null;
        isProcessing = false;

        OnClearComplete?.Invoke(cell);

        Debug.Log($"[CellClearController] *** SEQUENCE COMPLETE ***");
    }

    private void SpawnCloseEnd()
    {
        if (cellCloseEndPrefab == null)
        {
            Debug.LogError($"[CellClearController] cellCloseEndPrefab is NULL!");
            return;
        }

        Vector3 spawnPos = transform.position + spawnOffset;
        Debug.Log($"[CellClearController] Spawning at position: {spawnPos}");

        GameObject spawned = Instantiate(cellCloseEndPrefab, spawnPos, Quaternion.identity, transform);
        spawned.name = "CellCloseEnd_Active";

        Debug.Log($"[CellClearController] Instantiated: {spawned.name}, active={spawned.activeSelf}");

        // Đảm bảo active
        spawned.SetActive(true);

        // Lấy component
        currentCloseEnd = spawned.GetComponent<CellCloseEnd>();

        if (currentCloseEnd == null)
        {
            Debug.LogError($"[CellClearController] CellCloseEnd SCRIPT not found on prefab! Add CellCloseEnd.cs to prefab!");

            // Thử spawn trực tiếp không cần script
            Debug.Log($"[CellClearController] Trying direct spawn without script...");
            // Giữ spawned object để hiện lên
        }
        else
        {
            Debug.Log($"[CellClearController] CellCloseEnd component found!");
        }
    }

    private IEnumerator FlyItemsAway()
    {
        List<Item> items = cell.GetItems();
        Debug.Log($"[CellClearController] Flying {items.Count} items...");

        if (items.Count == 0) yield break;

        yield return new WaitForSeconds(delayBeforeFly);

        if (flyAwaySound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(flyAwaySound);
        else if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCellFlyAway();

        int dirSign = (transform.position.x <= 0) ? -1 : 1;
        Vector3 slideDir = Vector3.right * dirSign;

        Sequence flySeq = DOTween.Sequence();
        foreach (var item in items)
        {
            if (item == null) continue;
            var col = item.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            item.transform.SetParent(null);
            Vector3 flyPos = item.transform.position + new Vector3(0, 0, flyZOffset);
            flySeq.Join(item.transform.DOMove(flyPos, flyDuration).SetEase(flyEase));
        }
        yield return flySeq.WaitForCompletion();

        Sequence slideSeq = DOTween.Sequence();
        foreach (var item in items)
        {
            if (item == null) continue;
            Vector3 endPos = item.transform.position + slideDir * slideDistance;
            slideSeq.Join(item.transform.DOMove(endPos, slideDuration).SetEase(slideEase));
            slideSeq.Join(item.transform.DOScale(Vector3.zero, slideDuration).SetEase(slideEase));
        }
        yield return slideSeq.WaitForCompletion();

        foreach (var item in items)
        {
            if (item != null) Destroy(item.gameObject);
        }

        cell.ClearItemsWithoutDestroy();
        Debug.Log($"[CellClearController] Items cleared!");
    }

    public bool IsProcessing() => isProcessing;

    [ContextMenu("Force Test Clear")]
    public void ForceClear()
    {
        Debug.Log($"[CellClearController] Force Clear triggered!");
        if (!isProcessing) StartCoroutine(ClearSequence());
    }
}