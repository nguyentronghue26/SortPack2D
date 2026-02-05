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
    private Vector3 originalScale;

    // References
    private Camera mainCamera;
    private Cell currentCell;
    private Cell hoveredCell;
    private Collider2D itemCollider;
    private ItemAnimator itemAnimator;

    private Cell dragStartCell;

    public System.Action<Item> OnItemPickedUp;
    public System.Action<Item, Cell> OnItemDropped;

    void Start()
    {
        mainCamera = Camera.main;
        itemCollider = GetComponent<Collider2D>();
        itemAnimator = GetComponent<ItemAnimator>();
        originalZ = transform.position.z;
        originalScale = transform.lossyScale;
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

            ApplyOriginalScale();
            CheckHoveredCell();
        }
    }

    private void TryStartDrag()
    {
        Vector3 mouseWorld = GetMouseWorldPosition();

        // LẤY TẤT CẢ COLLIDER TẠI VỊ TRÍ CHUỘT
        RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorld, Vector2.zero);

        // TÌM ITEM TRƯỚC (ưu tiên Item hơn Cell)
        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                StartDrag();
                return;
            }
        }
    }

    void OnMouseDown()
    {
        // OnMouseDown vẫn hoạt động nếu Item ở trên Cell
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
        originalScale = transform.lossyScale;
        dragStartCell = currentCell;

        // Tách khỏi parent
        transform.SetParent(null);
        ApplyOriginalScale();

        if (itemAnimator != null)
        {
            itemAnimator.StopIdleAnimation();
        }

        SetSortingOrder(100);

        // TẮT COLLIDER KHI DRAG để không chặn raycast tìm Cell
        if (itemCollider != null)
            itemCollider.enabled = false;

        OnItemPickedUp?.Invoke(this);
    }

    public void EndDrag()
    {
        if (!isDragging) return;

        isDragging = false;

        // BẬT LẠI COLLIDER
        if (itemCollider != null)
            itemCollider.enabled = true;

        bool dropSuccess = false;
        bool invalidTarget = false;   
        Cell oldCell = dragStartCell;

        if (hoveredCell != null)
        {
            var locked = hoveredCell.GetComponent<LockedCell>();
            if (locked != null && !locked.CanAcceptItem())
            {

                invalidTarget = true;
            }
            else if (hoveredCell == currentCell)
            {
                int newSpotIndex = hoveredCell.GetNearestSpotIndex(transform.position);
                if (newSpotIndex >= 0 && newSpotIndex != spotIndex)
                {
                    currentCell.RemoveItemKeepScale(this);
                    currentCell.AddItemToSpotKeepScale(this, newSpotIndex, originalScale);
                    dropSuccess = true;
                }
                else
                {
                
                    invalidTarget = true;  
                }
            }
            else
            {
                if (hoveredCell.CanAcceptItem(this))
                {
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
                else
                {
                 
                    invalidTarget = true;  
                }
            }

            if (!dropSuccess && invalidTarget && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayInvalidDrop();
            }
        }
        else
        {
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
            // Trả về cell cũ
            if (dragStartCell != null)
            {
                int returnSpot = spotIndex >= 0 ? spotIndex : dragStartCell.GetNearestSpotIndex(originalPosition);
                dragStartCell.AddItemToSpotKeepScale(this, returnSpot, originalScale);
            }
            SnapToPosition(originalPosition);
        }

        ApplyOriginalScale();
        SetSortingOrder(0);

        if (hoveredCell != null)
        {
            hoveredCell.SetHighlight(false);
            hoveredCell = null;
        }
    }

    private void ApplyOriginalScale()
    {
        if (transform.parent == null)
        {
            transform.localScale = originalScale;
        }
        else
        {
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

        // Raycast tìm Cell
        RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorld, Vector2.zero);

        Cell foundCell = null;

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            // Tìm Cell
            Cell cell = hit.collider.GetComponent<Cell>();
            if (cell != null)
            {
                foundCell = cell;
                break;
            }
        }

        if (foundCell != hoveredCell)
        {
            if (hoveredCell != null)
                hoveredCell.SetHighlight(false);

            hoveredCell = foundCell;

            if (hoveredCell != null)
            {
                bool canAccept = (hoveredCell == currentCell)
                    ? hoveredCell.GetEmptySpotCount() > 0
                    : hoveredCell.CanAcceptItem(this);
                hoveredCell.SetHighlight(canAccept);
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
            ApplyOriginalScale();
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

    public void SetCell(Cell cell) => currentCell = cell;
    public Cell GetCurrentCell() => currentCell;
    public void Initialize(string type, int id) { itemType = type; itemID = id; }
    public ItemAnimator GetAnimator() => itemAnimator;
    public void SetSpotIndex(int index) => spotIndex = index;
    public int GetSpotIndex() => spotIndex;
    public void SaveOriginalScale() => originalScale = transform.lossyScale;
    public Vector3 GetOriginalScale() => originalScale;
}