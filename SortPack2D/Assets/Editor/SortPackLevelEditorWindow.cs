#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class SortPackLevelEditorWindow : EditorWindow
{
    private const float UI_SCALE = 2.5f;
    private const float CELL_BASE_WIDTH = 90f;
    private const float CELL_BASE_HEIGHT = 90f;
    private const float SLOT_BASE_WIDTH = 70f;
    private const float SLOT_BASE_HEIGHT = 24f;
    private const float GRID_SCROLL_BASE_HEIGHT = 250f;

    private float CELL_WIDTH => CELL_BASE_WIDTH * UI_SCALE;
    private float CELL_HEIGHT => CELL_BASE_HEIGHT * UI_SCALE;
    private float SLOT_WIDTH => SLOT_BASE_WIDTH * UI_SCALE;
    private float SLOT_HEIGHT => SLOT_BASE_HEIGHT * UI_SCALE;
    private float GRID_SCROLL_HEIGHT => GRID_SCROLL_BASE_HEIGHT * UI_SCALE;

    private SortPackLevelData currentLevel;
    private Vector2 scrollPos;
    private Vector2 statsScrollPos;
    private Vector2 quickButtonsScrollPos;

    // CHANGED: Dùng SpriteItemList thay vì ItemList
    [SerializeField] private SpriteItemList spriteItemList;

    // Cache
    private Dictionary<int, Sprite> idToSprite = new Dictionary<int, Sprite>();
    private Dictionary<int, string> idToName = new Dictionary<int, string>();
    private Dictionary<int, Texture2D> idToPreview = new Dictionary<int, Texture2D>();
    private List<int> availableItemIDs = new List<int>();

    private int selectedLayer = 0;
    private bool hasSelection = false;
    private int selX, selY, selZ, selSlot;
    private int editItemID = -1;

    private Dictionary<int, int> itemCounts = new Dictionary<int, int>();
    private List<int> invalidItemIDs = new List<int>();

    private readonly Color validColor = new Color(0.3f, 0.8f, 0.3f);
    private readonly Color invalidColor = new Color(1f, 0.3f, 0.3f);
    private readonly Color selectedColor = new Color(0.3f, 0.7f, 1f);

    [MenuItem("SortPack/Level Editor")]
    public static void OpenWindow()
    {
        var window = GetWindow<SortPackLevelEditorWindow>("SortPack Level Editor");
        window.minSize = new Vector2(800, 600);
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField("SortPack Level Editor", EditorStyles.boldLabel, GUILayout.Width(150));

        EditorGUI.BeginChangeCheck();
        currentLevel = (SortPackLevelData)EditorGUILayout.ObjectField(currentLevel, typeof(SortPackLevelData), false);
        if (EditorGUI.EndChangeCheck())
        {
            hasSelection = false;
            RefreshItemCounts();
        }

        EditorGUI.BeginChangeCheck();
        spriteItemList = (SpriteItemList)EditorGUILayout.ObjectField(spriteItemList, typeof(SpriteItemList), false);
        if (EditorGUI.EndChangeCheck())
        {
            BuildSpriteCache();
            Repaint();
        }

        EditorGUILayout.EndHorizontal();

        if (currentLevel == null)
        {
            EditorGUILayout.HelpBox("Kéo 1 SortPackLevelData vào để bắt đầu edit.", MessageType.Info);
            return;
        }

        RefreshItemCounts();
        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        // LEFT COLUMN
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.65f - 10));
        DrawLevelSettings();
        EditorGUILayout.Space(5);
        DrawLayerSelector();
        EditorGUILayout.Space(5);
        DrawGrid();
        EditorGUILayout.Space(5);
        DrawSelectionEditor();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // RIGHT COLUMN
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.35f - 10));
        DrawItemStatistics();
        EditorGUILayout.Space(5);
        DrawValidationStatus();
        EditorGUILayout.Space(5);
        DrawTools();
        EditorGUILayout.Space(5);
        DrawQuickButtons();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawLevelSettings()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Level Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        Undo.RecordObject(currentLevel, "Edit Level");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Level:", GUILayout.Width(50));
        currentLevel.levelIndex = EditorGUILayout.IntField(currentLevel.levelIndex, GUILayout.Width(50));
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Items/Match:", GUILayout.Width(80));
        currentLevel.itemsPerMatch = EditorGUILayout.IntField(currentLevel.itemsPerMatch, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Desc:", GUILayout.Width(50));
        currentLevel.description = EditorGUILayout.TextField(currentLevel.description);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Rows(X):", GUILayout.Width(55));
        currentLevel.sizeX = Mathf.Max(1, EditorGUILayout.IntField(currentLevel.sizeX, GUILayout.Width(40)));
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Cols(Y):", GUILayout.Width(50));
        currentLevel.sizeY = Mathf.Max(1, EditorGUILayout.IntField(currentLevel.sizeY, GUILayout.Width(40)));
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Layers(Z):", GUILayout.Width(60));
        currentLevel.sizeZ = Mathf.Max(1, EditorGUILayout.IntField(currentLevel.sizeZ, GUILayout.Width(40)));
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Slots:", GUILayout.Width(40));
        currentLevel.slotsPerCell = Mathf.Clamp(EditorGUILayout.IntField(currentLevel.slotsPerCell, GUILayout.Width(40)), 1, 5);
        EditorGUILayout.EndHorizontal();

        selectedLayer = Mathf.Clamp(selectedLayer, 0, currentLevel.sizeZ - 1);
        EditorGUILayout.EndVertical();

        if (GUI.changed) EditorUtility.SetDirty(currentLevel);
    }

    private void DrawLayerSelector()
    {
        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField("Layer:", GUILayout.Width(45));

        for (int z = 0; z < currentLevel.sizeZ; z++)
        {
            GUI.backgroundColor = (z == selectedLayer) ? selectedColor : Color.white;
            if (GUILayout.Button("Z=" + z, GUILayout.Width(45), GUILayout.Height(22)))
                selectedLayer = z;
        }

        GUI.backgroundColor = Color.white;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawGrid()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Grid - Layer " + selectedLayer, EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(GRID_SCROLL_HEIGHT));

        for (int x = 0; x < currentLevel.sizeX; x++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int y = 0; y < currentLevel.sizeY; y++)
                DrawCell(x, y, selectedLayer);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4 * UI_SCALE);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawCell(int x, int y, int z)
    {
        bool isSelected = hasSelection && selX == x && selY == y && selZ == z;

        GUIStyle cellStyle = new GUIStyle("box");
        if (isSelected) GUI.backgroundColor = new Color(0.3f, 0.5f, 0.7f, 0.8f);

        EditorGUILayout.BeginVertical(cellStyle, GUILayout.Width(CELL_WIDTH), GUILayout.Height(CELL_HEIGHT));
        GUI.backgroundColor = Color.white;

        EditorGUILayout.LabelField("(" + x + "," + y + ")", EditorStyles.miniLabel);

        for (int slot = 0; slot < currentLevel.slotsPerCell; slot++)
        {
            var placement = FindPlacement(x, y, z, slot);

            string label = "-";
            Color btnColor = Color.gray;
            Texture2D preview = null;
            string tooltip = "";

            if (placement != null && placement.itemID >= 0)
            {
                label = placement.itemID.ToString();
                btnColor = invalidItemIDs.Contains(placement.itemID) ? invalidColor : validColor;

                if (idToSprite.TryGetValue(placement.itemID, out var sprite) && sprite != null)
                {
                    if (idToName.TryGetValue(placement.itemID, out var name))
                        tooltip = name;

                    if (!idToPreview.TryGetValue(placement.itemID, out preview) || preview == null)
                    {
                        preview = AssetPreview.GetAssetPreview(sprite);
                        if (preview == null) preview = sprite.texture;
                        idToPreview[placement.itemID] = preview;
                    }
                }
            }

            if (hasSelection && selX == x && selY == y && selZ == z && selSlot == slot)
                btnColor = selectedColor;

            GUI.backgroundColor = btnColor;

            GUIContent content = (preview != null)
                ? new GUIContent(label, preview, tooltip)
                : new GUIContent(label, tooltip);

            if (GUILayout.Button(content, GUILayout.Width(SLOT_WIDTH), GUILayout.Height(SLOT_HEIGHT)))
                SelectSlot(x, y, z, slot, placement);
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();
    }

    private void SelectSlot(int x, int y, int z, int slot, CellSlotItemData placement)
    {
        hasSelection = true;
        selX = x; selY = y; selZ = z; selSlot = slot;
        editItemID = (placement != null) ? placement.itemID : -1;
        Repaint();
    }

    private CellSlotItemData FindPlacement(int x, int y, int z, int slotIndex)
    {
        if (currentLevel == null || currentLevel.placements == null) return null;
        foreach (var p in currentLevel.placements)
            if (p != null && p.x == x && p.y == y && p.z == z && p.slotIndex == slotIndex)
                return p;
        return null;
    }

    private void DrawSelectionEditor()
    {
        EditorGUILayout.BeginVertical("box");

        if (!hasSelection)
        {
            EditorGUILayout.LabelField("Click a slot to edit", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.LabelField("Selected: Cell(" + selX + "," + selY + ") Layer=" + selZ + " Slot=" + selSlot, EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Item ID:", GUILayout.Width(55));
        editItemID = EditorGUILayout.IntField(editItemID, GUILayout.Width(50));
        EditorGUILayout.Space(10);
        if (GUILayout.Button("Set", GUILayout.Width(45))) ApplySelection();
        if (GUILayout.Button("Clear", GUILayout.Width(50))) ClearSelectedSlot();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3);
        EditorGUILayout.EndVertical();
    }

    private void DrawQuickButtons()
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Quick Select", EditorStyles.boldLabel);
        if (spriteItemList != null && availableItemIDs.Count > 0)
            EditorGUILayout.LabelField("(" + availableItemIDs.Count + " items)", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3);

        if (spriteItemList == null)
        {
            EditorGUILayout.HelpBox("Assign SpriteItemList to see items", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        if (availableItemIDs.Count == 0)
        {
            EditorGUILayout.LabelField("No items in SpriteItemList", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            return;
        }

        if (!hasSelection)
        {
            EditorGUILayout.HelpBox("Select a slot first", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        float buttonWidth = 55f;
        float buttonHeight = 55f;
        float spacing = 4f;
        float availableWidth = position.width * 0.35f - 30;
        int buttonsPerRow = Mathf.Max(1, (int)(availableWidth / (buttonWidth + spacing)));

        quickButtonsScrollPos = EditorGUILayout.BeginScrollView(quickButtonsScrollPos, GUILayout.Height(280f));

        int index = 0;
        while (index < availableItemIDs.Count)
        {
            EditorGUILayout.BeginHorizontal();

            for (int col = 0; col < buttonsPerRow && index < availableItemIDs.Count; col++)
            {
                int itemID = availableItemIDs[index];

                Texture2D preview = null;
                string itemName = "ID: " + itemID;

                if (idToSprite.TryGetValue(itemID, out var sprite) && sprite != null)
                {
                    if (idToName.TryGetValue(itemID, out var name))
                        itemName = itemID + ": " + name;

                    if (!idToPreview.TryGetValue(itemID, out preview) || preview == null)
                    {
                        preview = AssetPreview.GetAssetPreview(sprite);
                        if (preview == null) preview = sprite.texture;
                        idToPreview[itemID] = preview;
                    }
                }

                GUI.backgroundColor = (editItemID == itemID) ? selectedColor : Color.white;

                EditorGUILayout.BeginVertical(GUILayout.Width(buttonWidth));

                GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
                btnStyle.padding = new RectOffset(2, 2, 2, 2);

                GUIContent btnContent = preview != null
                    ? new GUIContent(preview, itemName)
                    : new GUIContent("?", itemName);

                if (GUILayout.Button(btnContent, btnStyle, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight - 18)))
                {
                    editItemID = itemID;
                    ApplySelection();
                }

                GUIStyle idStyle = new GUIStyle(EditorStyles.miniLabel);
                idStyle.alignment = TextAnchor.MiddleCenter;
                idStyle.fontStyle = FontStyle.Bold;
                idStyle.normal.textColor = editItemID == itemID ? Color.white : Color.gray;

                EditorGUILayout.LabelField(itemID.ToString(), idStyle, GUILayout.Width(buttonWidth), GUILayout.Height(16));

                EditorGUILayout.EndVertical();
                index++;
            }

            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void ApplySelection()
    {
        if (currentLevel == null || !hasSelection) return;

        Undo.RecordObject(currentLevel, "Set Item");
        var placement = FindPlacement(selX, selY, selZ, selSlot);

        if (editItemID < 0)
        {
            if (placement != null) currentLevel.placements.Remove(placement);
        }
        else
        {
            if (placement == null)
            {
                placement = new CellSlotItemData { x = selX, y = selY, z = selZ, slotIndex = selSlot, itemID = editItemID };
                currentLevel.placements.Add(placement);
            }
            else
            {
                placement.itemID = editItemID;
            }
        }

        EditorUtility.SetDirty(currentLevel);
        RefreshItemCounts();
    }

    private void ClearSelectedSlot()
    {
        editItemID = -1;
        ApplySelection();
    }

    private void RefreshItemCounts()
    {
        if (currentLevel == null) return;
        itemCounts = currentLevel.GetItemCountsByID();
        invalidItemIDs = currentLevel.GetInvalidItemIDs();
        BuildSpriteCache();
    }

    private void BuildSpriteCache()
    {
        idToSprite.Clear();
        idToName.Clear();
        idToPreview.Clear();
        availableItemIDs.Clear();

        if (spriteItemList == null || spriteItemList.items == null) return;

        for (int i = 0; i < spriteItemList.items.Count; i++)
        {
            var item = spriteItemList.items[i];
            if (item == null || item.sprite == null) continue;

            int id = i;
            if (!idToSprite.ContainsKey(id))
            {
                idToSprite.Add(id, item.sprite);
                idToName.Add(id, string.IsNullOrEmpty(item.itemName) ? item.sprite.name : item.itemName);
                availableItemIDs.Add(id);
            }
        }
    }

    private void DrawItemStatistics()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Item Statistics", EditorStyles.boldLabel);

        if (itemCounts.Count == 0)
        {
            EditorGUILayout.LabelField("No items placed.", EditorStyles.miniLabel);
        }
        else
        {
            statsScrollPos = EditorGUILayout.BeginScrollView(statsScrollPos, GUILayout.Height(120));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ID", EditorStyles.boldLabel, GUILayout.Width(30));
            EditorGUILayout.LabelField("Count", EditorStyles.boldLabel, GUILayout.Width(45));
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("Name", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            foreach (var kvp in itemCounts.OrderBy(k => k.Key))
            {
                int itemID = kvp.Key;
                int count = kvp.Value;
                bool isValid = count % currentLevel.itemsPerMatch == 0;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(itemID.ToString(), GUILayout.Width(30));

                GUIStyle countStyle = new GUIStyle(EditorStyles.label);
                countStyle.normal.textColor = isValid ? validColor : invalidColor;
                countStyle.fontStyle = FontStyle.Bold;
                EditorGUILayout.LabelField(count.ToString(), countStyle, GUILayout.Width(45));

                if (isValid)
                {
                    int sets = count / currentLevel.itemsPerMatch;
                    EditorGUILayout.LabelField("✓ " + sets + " set(s)", GUILayout.Width(80));
                }
                else
                {
                    int need = currentLevel.itemsPerMatch - (count % currentLevel.itemsPerMatch);
                    GUIStyle errStyle = new GUIStyle(EditorStyles.label);
                    errStyle.normal.textColor = invalidColor;
                    EditorGUILayout.LabelField("✗ +" + need, errStyle, GUILayout.Width(80));
                }

                string itemName = "";
                if (idToName.TryGetValue(itemID, out var name)) itemName = name;
                EditorGUILayout.LabelField(itemName);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        int total = itemCounts.Values.Sum();
        EditorGUILayout.LabelField("Total Items: " + total, EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    private void DrawValidationStatus()
    {
        EditorGUILayout.BeginVertical("box");

        if (itemCounts.Count == 0)
        {
            EditorGUILayout.LabelField("No items placed.", EditorStyles.centeredGreyMiniLabel);
        }
        else if (invalidItemIDs.Count == 0)
        {
            GUIStyle validStyle = new GUIStyle(EditorStyles.boldLabel);
            validStyle.normal.textColor = validColor;
            validStyle.fontSize = 14;
            EditorGUILayout.LabelField("✓ LEVEL VALID", validStyle);
            EditorGUILayout.LabelField("All items have correct counts.", EditorStyles.miniLabel);
        }
        else
        {
            GUIStyle invalidStyle = new GUIStyle(EditorStyles.boldLabel);
            invalidStyle.normal.textColor = invalidColor;
            invalidStyle.fontSize = 14;
            EditorGUILayout.LabelField("✗ LEVEL INVALID", invalidStyle);
            EditorGUILayout.LabelField("Fix IDs: [" + string.Join(", ", invalidItemIDs) + "]", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawTools()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        if (GUILayout.Button("Clear All Items", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Clear All", "Xóa toàn bộ items?", "Yes", "No"))
            {
                Undo.RecordObject(currentLevel, "Clear All");
                currentLevel.placements.Clear();
                hasSelection = false;
                EditorUtility.SetDirty(currentLevel);
                RefreshItemCounts();
            }
        }

        if (GUILayout.Button("Recalculate Statistics", GUILayout.Height(25)))
            RecalculateStats();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Quick Fill:", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fill Layer Random", GUILayout.Height(22)))
            FillLayerRandom(selectedLayer);
        if (GUILayout.Button("Clear Layer", GUILayout.Height(22)))
            ClearLayer(selectedLayer);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void RecalculateStats()
    {
        if (currentLevel == null) return;
        Undo.RecordObject(currentLevel, "Recalculate");

        currentLevel.itemCounts.Clear();
        var counts = currentLevel.GetItemCountsByID();

        foreach (var kvp in counts)
        {
            currentLevel.itemCounts.Add(new ItemCountInfo
            {
                itemID = kvp.Key,
                totalQuantity = kvp.Value,
                isValid = kvp.Value % currentLevel.itemsPerMatch == 0
            });
        }

        EditorUtility.SetDirty(currentLevel);
        RefreshItemCounts();
    }

    private void FillLayerRandom(int layer)
    {
        if (currentLevel == null) return;
        Undo.RecordObject(currentLevel, "Fill Random");

        List<int> idsToUse = availableItemIDs.Count > 0 ? availableItemIDs : Enumerable.Range(0, 6).ToList();
        int numTypes = Mathf.Min(Random.Range(3, 6), idsToUse.Count);
        var shuffledIDs = idsToUse.OrderBy(x => Random.value).Take(numTypes).ToList();

        for (int x = 0; x < currentLevel.sizeX; x++)
        {
            for (int y = 0; y < currentLevel.sizeY; y++)
            {
                for (int slot = 0; slot < currentLevel.slotsPerCell; slot++)
                {
                    var existing = FindPlacement(x, y, layer, slot);
                    if (existing != null) currentLevel.placements.Remove(existing);

                    currentLevel.placements.Add(new CellSlotItemData
                    {
                        x = x,
                        y = y,
                        z = layer,
                        slotIndex = slot,
                        itemID = shuffledIDs[Random.Range(0, shuffledIDs.Count)]
                    });
                }
            }
        }

        EditorUtility.SetDirty(currentLevel);
        RefreshItemCounts();
    }

    private void ClearLayer(int layer)
    {
        if (currentLevel == null) return;
        Undo.RecordObject(currentLevel, "Clear Layer");
        currentLevel.placements.RemoveAll(p => p.z == layer);
        EditorUtility.SetDirty(currentLevel);
        RefreshItemCounts();
    }
}
#endif