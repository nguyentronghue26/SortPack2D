using UnityEngine;

public class Item : MonoBehaviour
{
    [Header("Item Settings")]
    public string itemType;
    public int itemID;
    private int spotIndex = -1;

    [Header("Drag Settings")]
    [SerializeField] private float dragSpeed = 50f;
    [SerializeField] private float snapSpeed = 20f;

    // State
    private bool isDragging = false;
    private Vector3 originalPosition;
    private float originalZ;
    private Vector3 originalScale;  // LƯU SCALE GỐC

    // References
    private Camera mainCamera;
    private Cell currentCell;
    private Cell hoveredCell;
    private Collider2D itemCollider;
    private ItemAnimator itemAnimator;

    // Cache cell gốc khi bắt đầu drag
    private Cell dragStartCell;

    // Events
    public System.Action<Item> OnItemPickedUp;
    public System.Action<Item, Cell> OnItemDropped;

    void Start()
    {
        mainCamera = Camera.main;
        itemCollider = GetComponent<Collider2D>();
        itemAnimator = GetComponent<ItemAnimator>();
        originalZ = transform.position.z;

        // LƯU SCALE LÚC START
        originalScale = transform.lossyScale;  // World scale
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
            Vector3 targetPos = GetMouseWorldPosition();
            targetPos.z = originalZ;

            transform.position = Vector3.Lerp(
                transform.position,
                targetPos,
                dragSpeed * Time.deltaTime
            );

            // GIỮ SCALE KHI DRAG
            ApplyOriginalScale();

            CheckHoveredCell();
        }
    }

    private void TryStartDrag()
    {
        Vector3 mouseWorld = GetMouseWorldPosition();

        RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);

        if (hit.collider != null && hit.collider.gameObject == gameObject)
        {
            StartDrag();
        }
    }

    void OnMouseDown()
    {
        StartDrag();
    }

    void OnMouseUp()
    {
        EndDrag();
    }

    public void StartDrag()
    {
        if (isDragging) return;

        isDragging = true;
        originalPosition = transform.position;

        // LƯU SCALE TRƯỚC KHI TÁCH PARENT
        originalScale = transform.lossyScale;

        // Cache cell gốc trước khi drag
        dragStartCell = currentCell;

        // TÁCH KHỎI PARENT NHƯNG GIỮ SCALE
        transform.SetParent(null);
        ApplyOriginalScale();

        if (itemAnimator != null)
        {
            itemAnimator.StopIdleAnimation();
            // Không gọi PlayPickUp() để tránh scale animation
        }

        SetSortingOrder(100);

        if (itemCollider != null)
            itemCollider.enabled = false;

        OnItemPickedUp?.Invoke(this);
    }

    public void EndDrag()
    {
        if (!isDragging) return;

        isDragging = false;

        if (itemCollider != null)
            itemCollider.enabled = true;

        bool dropSuccess = false;
        Cell oldCell = dragStartCell;

        if (hoveredCell != null)
        {
            if (hoveredCell == currentCell)
            {
                // Cùng cell - đổi spot
                int newSpotIndex = hoveredCell.GetNearestSpotIndex(transform.position);
                if (newSpotIndex >= 0 && newSpotIndex != spotIndex)
                {
                    currentCell.RemoveItemKeepScale(this);
                    currentCell.AddItemToSpotKeepScale(this, newSpotIndex, originalScale);
                    dropSuccess = true;
                }
            }
            else if (hoveredCell.CanAcceptItem(this))
            {
                // Khác cell - di chuyển item sang cell mới
                if (currentCell != null)
                {
                    currentCell.RemoveItemKeepScale(this);
                }

                hoveredCell.AddItemAtPositionKeepScale(this, transform.position, originalScale);
                currentCell = hoveredCell;
                dropSuccess = true;

                if (oldCell != null && oldCell != hoveredCell)
                {
                    oldCell.NotifyItemMovedToOtherCell();
                }

                OnItemDropped?.Invoke(this, hoveredCell);
            }
        }

        if (dropSuccess)
        {
            if (itemAnimator != null)
            {
                itemAnimator.PlayDropBounce();
            }
        }
        else
        {
            // Trả về vị trí cũ
            if (dragStartCell != null)
            {
                dragStartCell.AddItemToSpotKeepScale(this, spotIndex >= 0 ? spotIndex : 0, originalScale);
            }
            SnapToPosition(originalPosition);
        }

        // ĐẢM BẢO SCALE ĐÚNG SAU KHI DROP
        ApplyOriginalScale();

        SetSortingOrder(0);

        if (hoveredCell != null)
        {
            hoveredCell.SetHighlight(false);
            hoveredCell = null;
        }
    }

    // ========== HELPER METHODS ==========

    private void ApplyOriginalScale()
    {
        // Nếu không có parent, localScale = lossyScale
        if (transform.parent == null)
        {
            transform.localScale = originalScale;
        }
        else
        {
            // Nếu có parent, cần tính toán localScale để lossyScale = originalScale
            Vector3 parentScale = transform.parent.lossyScale;
            transform.localScale = new Vector3(
                originalScale.x / parentScale.x,
                originalScale.y / parentScale.y,
                originalScale.z / parentScale.z
            );
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(mainCamera.transform.position.z - originalZ);
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    private void CheckHoveredCell()
    {
        Vector3 mouseWorld = GetMouseWorldPosition();

        RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorld, Vector2.zero);

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
            {
                hoveredCell.SetHighlight(false);
            }

            hoveredCell = foundCell;

            bool canAccept = false;
            if (foundCell == currentCell)
            {
                canAccept = foundCell.GetEmptySpotCount() > 0;
            }
            else
            {
                canAccept = foundCell.CanAcceptItem(this);
            }

            hoveredCell.SetHighlight(canAccept);
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

    private void SnapToPosition(Vector3 position)
    {
        StartCoroutine(SmoothSnapCoroutine(position));
    }

    private System.Collections.IEnumerator SmoothSnapCoroutine(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, target, snapSpeed * Time.deltaTime);
            ApplyOriginalScale();  // Giữ scale trong lúc snap
            yield return null;
        }
        transform.position = target;
        ApplyOriginalScale();
    }

    private void SetSortingOrder(int order)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = order;
        }
    }

    // ========== PUBLIC METHODS ==========

    public void SetCell(Cell cell)
    {
        currentCell = cell;
    }

    public Cell GetCurrentCell()
    {
        return currentCell;
    }

    public void Initialize(string type, int id)
    {
        itemType = type;
        itemID = id;
    }

    public ItemAnimator GetAnimator()
    {
        return itemAnimator;
    }

    public void SetSpotIndex(int index)
    {
        spotIndex = index;
    }

    public int GetSpotIndex()
    {
        return spotIndex;
    }

    /// <summary>
    /// Gọi sau khi spawn để lưu scale gốc
    /// </summary>
    public void SaveOriginalScale()
    {
        originalScale = transform.lossyScale;
    }

    public Vector3 GetOriginalScale()
    {
        return originalScale;
    }
}