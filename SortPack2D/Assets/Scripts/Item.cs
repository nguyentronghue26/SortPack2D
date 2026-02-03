using UnityEngine;

public class Item : MonoBehaviour
{
    [Header("Item Settings")]
    public string itemType;
    public int itemID;
    private int spotIndex = -1;

    [Header("Drag Settings")]
    [SerializeField] private float dragSpeed = 50f;

    // State
    private bool isDragging = false;
    private Vector3 originalPosition;
    private float originalZ;
    private Vector3 baseScale;

    // References
    private Camera mainCamera;
    private Cell currentCell;
    private Cell hoveredCell;
    private Collider2D itemCollider;
    private ItemAnimator itemAnimator;
    private ItemOutline itemOutline;

    private Cell dragStartCell;
    private int dragStartSpotIndex;

    private bool scaleInitialized = false;

    public System.Action<Item> OnItemPickedUp;
    public System.Action<Item, Cell> OnItemDropped;

    void Start()
    {
        mainCamera = Camera.main;
        itemCollider = GetComponent<Collider2D>();
        itemAnimator = GetComponent<ItemAnimator>();
        itemOutline = GetComponent<ItemOutline>();

        originalZ = transform.position.z;

        // LƯU SCALE GỐC 1 LẦN DUY NHẤT
        if (!scaleInitialized)
        {
            baseScale = transform.localScale;
            scaleInitialized = true;
            Debug.Log($"[Item] {name} baseScale initialized: {baseScale}");
        }
    }

    public void InitializeScale(Vector3 scale)
    {
        baseScale = scale;
        transform.localScale = scale;
        scaleInitialized = true;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isDragging)
        {
            TryStartDrag();
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag();
        }
    }

    void LateUpdate()
    {
        if (isDragging)
        {
            Vector3 targetPos = GetInputPosition();
            targetPos.z = originalZ;

            transform.position = Vector3.Lerp(
                transform.position,
                targetPos,
                dragSpeed * Time.deltaTime
            );

            // KHÓA SCALE khi drag
            transform.localScale = baseScale;
        }

        CheckHoveredCell();
    }

    private void TryStartDrag()
    {
        if (mainCamera == null) return;

        Vector3 worldPos = GetInputPosition();
        Vector2 pos2D = worldPos;

        RaycastHit2D hit = Physics2D.Raycast(pos2D, Vector2.zero);
        if (hit.collider != null && hit.collider.gameObject == gameObject)
        {
            StartDrag();
        }
    }

    void OnMouseDown() { StartDrag(); }
    void OnMouseUp() { EndDrag(); }

    public void StartDrag()
    {
        if (isDragging) return;

        // KHÓA SCALE
        transform.localScale = baseScale;

        isDragging = true;
        originalPosition = transform.position;

        dragStartCell = currentCell;
        dragStartSpotIndex = spotIndex;

        if (currentCell != null)
            currentCell.RemoveItem(this);

        Vector3 mouseWorld = GetInputPosition();
        mouseWorld.z = originalZ;
        transform.position = mouseWorld;

        if (itemAnimator != null)
            itemAnimator.StopIdleAnimation();

        if (itemOutline != null)
            itemOutline.ShowOutline();

        SetSortingOrder(100);

        if (itemCollider != null)
            itemCollider.enabled = false;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPickUp();

        OnItemPickedUp?.Invoke(this);
    }

    public void EndDrag()
    {
        if (!isDragging) return;
        isDragging = false;

        Vector3 p = transform.position;
        p.z = originalZ;
        transform.position = p;

        // KHÓA SCALE
        transform.localScale = baseScale;

        if (itemCollider != null)
            itemCollider.enabled = true;

        bool dropSuccess = false;
        Cell targetCell = hoveredCell;

        if (targetCell != null && targetCell.CanAcceptItem(this))
        {
            targetCell.AddItemAtPosition(this, transform.position);
            currentCell = targetCell;
            dropSuccess = true;

            if (dragStartCell != null && dragStartCell != targetCell)
                dragStartCell.NotifyItemMovedToOtherCell();

            OnItemDropped?.Invoke(this, targetCell);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayDrop();
        }
        else
        {
            if (dragStartCell != null)
            {
                if (dragStartSpotIndex >= 0 && dragStartCell.IsSpotEmpty(dragStartSpotIndex))
                {
                    dragStartCell.AddItemToSpot(this, dragStartSpotIndex);
                }
                else
                {
                    dragStartCell.AddItemAtPosition(this, originalPosition);
                }
                currentCell = dragStartCell;
            }

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayInvalidDrop();
        }

        // KHÓA SCALE sau drop
        transform.localScale = baseScale;

        if (itemOutline != null)
            itemOutline.HideOutline();

        SetSortingOrder(0);

        if (hoveredCell != null)
        {
            hoveredCell.SetHighlight(false);
            hoveredCell = null;
        }
    }

    private bool IsFinite(float v)
    {
        return !float.IsNaN(v) && !float.IsInfinity(v);
    }

    private Vector3 GetInputPosition()
    {
        if (mainCamera == null) return transform.position;

        Vector3 mouse = Input.mousePosition;

        if (!IsFinite(mouse.x) || !IsFinite(mouse.y))
            return transform.position;

        float depth = mainCamera.WorldToScreenPoint(transform.position).z;
        if (!IsFinite(depth) || depth <= 0f)
            depth = 10f;

        mouse.z = depth;
        return mainCamera.ScreenToWorldPoint(mouse);
    }

    private void CheckHoveredCell()
    {
        if (mainCamera == null || !isDragging) return;

        Vector3 mouse = Input.mousePosition;

        if (!IsFinite(mouse.x) || !IsFinite(mouse.y))
            return;

        float depth = mainCamera.WorldToScreenPoint(transform.position).z;
        if (!IsFinite(depth) || depth <= 0f)
            depth = 10f;

        mouse.z = depth;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mouse);
        Vector2 pos2D = worldPos;

        RaycastHit2D[] hits = Physics2D.RaycastAll(pos2D, Vector2.zero);

        Cell foundCell = null;
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            Cell cell = hit.collider.GetComponent<Cell>();
            if (cell != null)
            {
                foundCell = cell;
                break;
            }
        }

        if (foundCell != null)
        {
            if (hoveredCell != null && hoveredCell != foundCell)
                hoveredCell.SetHighlight(false);

            hoveredCell = foundCell;
            hoveredCell.SetHighlight(foundCell.CanAcceptItem(this));
        }
        else
        {
            if (hoveredCell != null)
            {
                hoveredCell.SetHighlight(false);
                hoveredCell = null;
            }
        }
    }

    private void SetSortingOrder(int order)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = order;
    }

    public void SetCell(Cell cell) => currentCell = cell;
    public Cell GetCurrentCell() => currentCell;
    public void Initialize(string type, int id) { itemType = type; itemID = id; }
    public ItemAnimator GetAnimator() => itemAnimator;
    public void SetSpotIndex(int index) => spotIndex = index;
    public int GetSpotIndex() => spotIndex;
    public Vector3 GetBaseScale() => baseScale;

    public void ForceScale(Vector3 scale)
    {
        baseScale = scale;
        transform.localScale = scale;
    }
}