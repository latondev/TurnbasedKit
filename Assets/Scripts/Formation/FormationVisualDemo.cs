using System;
using System.Collections.Generic;
using UnityEngine;
using GameSystems.Formation;

[RequireComponent(typeof(FormationController))]
public class FormationVisualDemo : MonoBehaviour
{
    [System.Serializable]
    public class UnitVisualData
    {
        public string unitId;
        public GameObject prefab;
    }

    [Header("Visual Settings")]
    [SerializeField] private Transform gridCenter;
    [SerializeField] private float slotSpacing = 2.0f;
    [SerializeField] private float lerpSpeed = 10.0f;
    [SerializeField] private Material slotMaterialEmpty;
    [SerializeField] private Material slotMaterialOccupied;
    [SerializeField] private Material slotMaterialHover;

    [Header("References")]
    [SerializeField] private GameObject slotPrefab; // A flat cube/plane to represent a location
    [SerializeField] public List<UnitVisualData> unitVisuals;

    private FormationController controller;
    
    // Mapping from SlotId to the spawned Slot visual GameObjects
    private Dictionary<int, SlotVisual> slotVisuals = new Dictionary<int, SlotVisual>();
    
    // Mapping from SlotId to spawned Unit objects
    private Dictionary<int, UnitVisual> unitObjects = new Dictionary<int, UnitVisual>();

    private Camera mainCam;

    // Drag and Drop state
    private UnitVisual currentlyDraggingUnit;
    private SlotVisual hoverSlot;
    private Vector3 dragOffset;
    private float dragYHeight = 1f;

    void Start()
    {
        mainCam = Camera.main;
        controller = GetComponent<FormationController>();

        // Subscribe to controller events
        controller.OnFormationChanged += HandleFormationChanged;
        controller.OnUnitPlaced += HandleUnitPlaced;
        controller.OnUnitRemoved += HandleUnitRemoved;
        controller.OnUnitsSwapped += HandleUnitsSwapped;

        // Force a visual update if a formation is already selected
        if (controller.CurrentFormation != null)
        {
            HandleFormationChanged(controller.CurrentFormation);
        }
    }

    private void Update()
    {
        HandleMouseInteraction();

        // Animate units to their target positions
        foreach (var kvp in unitObjects)
        {
            if (kvp.Value == currentlyDraggingUnit) continue; // Dragged unit follows mouse
            
            if (slotVisuals.TryGetValue(kvp.Key, out SlotVisual targetSlot))
            {
                Transform unitT = kvp.Value.transform;
                unitT.position = Vector3.Lerp(unitT.position, targetSlot.transform.position, Time.deltaTime * lerpSpeed);
            }
        }
    }

    #region Drag and Drop Interaction

    private void HandleMouseInteraction()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        // Update hovering
        UpdateHoverState(ray);

        // Click to drag or spawn
        if (Input.GetMouseButtonDown(0))
        {
            if (hoverSlot != null)
            {
                if (unitObjects.ContainsKey(hoverSlot.SlotId))
                {
                    // Start dragging existing unit
                    currentlyDraggingUnit = unitObjects[hoverSlot.SlotId];
                    dragOffset = currentlyDraggingUnit.transform.position - GetMouseWorldPos(ray);
                }
                else
                {
                    // For demo purposes: Place a random unit into empty slot on click!
                    string randomUnitTarget = controller.AvailableUnits[UnityEngine.Random.Range(0, controller.AvailableUnits.Count)];
                    controller.PlaceUnit(hoverSlot.SlotId, randomUnitTarget);
                }
            }
        }

        // Dragging
        if (Input.GetMouseButton(0) && currentlyDraggingUnit != null)
        {
            Vector3 targetPos = GetMouseWorldPos(ray) + dragOffset;
            targetPos.y = gridCenter.position.y + dragYHeight; // Keep elevated
            currentlyDraggingUnit.transform.position = Vector3.Lerp(currentlyDraggingUnit.transform.position, targetPos, Time.deltaTime * lerpSpeed * 2f);
        }

        // Release to drop/swap
        if (Input.GetMouseButtonUp(0))
        {
            if (currentlyDraggingUnit != null)
            {
                if (hoverSlot != null && hoverSlot.SlotId != currentlyDraggingUnit.CurrentSlotId)
                {
                    // Attempt swap (even if empty, FormationController handles it)
                    controller.SwapUnits(currentlyDraggingUnit.CurrentSlotId, hoverSlot.SlotId);
                }
                
                currentlyDraggingUnit = null;
            }
        }

        // Right click to remove
        if (Input.GetMouseButtonDown(1))
        {
            if (hoverSlot != null && unitObjects.ContainsKey(hoverSlot.SlotId))
            {
                controller.RemoveUnit(hoverSlot.SlotId);
            }
        }
    }

    private void UpdateHoverState(Ray ray)
    {
        SlotVisual newHoverSlot = null;
        
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            newHoverSlot = hit.collider.GetComponent<SlotVisual>();
        }

        if (newHoverSlot != hoverSlot)
        {
            // Reset old
            if (hoverSlot != null)
            {
                bool isOccupied = unitObjects.ContainsKey(hoverSlot.SlotId);
                hoverSlot.SetMaterial(isOccupied ? slotMaterialOccupied : slotMaterialEmpty);
            }

            hoverSlot = newHoverSlot;

            // Highlight new
            if (hoverSlot != null)
            {
                hoverSlot.SetMaterial(slotMaterialHover);
            }
        }
    }

    private Vector3 GetMouseWorldPos(Ray ray)
    {
        // Intersect ray with a flat plane at grid center Y
        Plane plane = new Plane(Vector3.up, gridCenter.position);
        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        return Vector3.zero;
    }

    #endregion

    #region Controller Event Handlers

    private void HandleFormationChanged(FormationType formation)
    {
        ClearVisuals();

        // Need to calculate bounding box to center the grid
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var slot in formation.Slots)
        {
            if (slot.GridPosition.x < minX) minX = slot.GridPosition.x;
            if (slot.GridPosition.x > maxX) maxX = slot.GridPosition.x;
            if (slot.GridPosition.y < minZ) minZ = slot.GridPosition.y;
            if (slot.GridPosition.y > maxZ) maxZ = slot.GridPosition.y;
        }

        float width = (maxX - minX) * slotSpacing;
        float depth = (maxZ - minZ) * slotSpacing;

        Vector3 offsetToCenter = new Vector3(-width / 2f, 0, -depth / 2f);

        // Spawn slots
        foreach (var slot in formation.Slots)
        {
            Vector3 pos = gridCenter.position + offsetToCenter + new Vector3(slot.GridPosition.x * slotSpacing, 0, slot.GridPosition.y * slotSpacing);
            
            GameObject slotObj = Instantiate(slotPrefab, pos, Quaternion.identity, transform);
            slotObj.name = $"Slot_{slot.SlotId} ({slot.GridPosition.x},{slot.GridPosition.y})";

            SlotVisual sv = slotObj.AddComponent<SlotVisual>();
            sv.SlotId = slot.SlotId;
            sv.VisualMesh = slotObj.GetComponentInChildren<MeshRenderer>();
            sv.SetMaterial(slotMaterialEmpty);
            
            // Need a collider for raycasting
            if (slotObj.GetComponent<Collider>() == null)
                slotObj.AddComponent<BoxCollider>();

            slotVisuals.Add(slot.SlotId, sv);

            // If unit already exists (e.g. from AutoArrange or persisting state), spawn it
            if (slot.IsOccupied)
            {
                SpawnUnitVisual(slot.SlotId, slot.OccupiedUnitId);
            }
        }
    }

    private void HandleUnitPlaced(FormationSlot slot, string unitId)
    {
        SpawnUnitVisual(slot.SlotId, unitId);
    }

    private void HandleUnitRemoved(FormationSlot slot)
    {
        if (unitObjects.TryGetValue(slot.SlotId, out UnitVisual uv))
        {
            Destroy(uv.gameObject);
            unitObjects.Remove(slot.SlotId);

            if (slotVisuals.TryGetValue(slot.SlotId, out SlotVisual sv))
            {
                sv.SetMaterial(slotMaterialEmpty);
            }
        }
    }

    private void HandleUnitsSwapped(FormationSlot slot1, FormationSlot slot2)
    {
        // Extract references temporarily
        unitObjects.TryGetValue(slot1.SlotId, out UnitVisual unit1);
        unitObjects.TryGetValue(slot2.SlotId, out UnitVisual unit2);

        // Swap in dictionary
        if (unit1 != null)
        {
            unitObjects.Remove(slot1.SlotId);
            unit1.CurrentSlotId = slot2.SlotId;
        }
        
        if (unit2 != null)
        {
            unitObjects.Remove(slot2.SlotId);
            unit2.CurrentSlotId = slot1.SlotId;
        }

        if (unit1 != null) unitObjects[slot2.SlotId] = unit1;
        if (unit2 != null) unitObjects[slot1.SlotId] = unit2;
        
        // Slot visuals update colors just in case
        UpdateSlotColor(slot1.SlotId);
        UpdateSlotColor(slot2.SlotId);
    }

    #endregion

    #region Helpers

    private void SpawnUnitVisual(int slotId, string unitId)
    {
        // Clean up existing if any
        if (unitObjects.ContainsKey(slotId))
        {
            Destroy(unitObjects[slotId].gameObject);
            unitObjects.Remove(slotId);
        }

        GameObject prefab = GetPrefabForUnit(unitId);
        if (prefab == null) return;
        
        if (slotVisuals.TryGetValue(slotId, out SlotVisual sv))
        {
            GameObject inst = Instantiate(prefab, sv.transform.position + Vector3.up * 5f, Quaternion.identity, transform); // Spawn high up and drop down
            inst.name = $"Visual_{unitId}";

            UnitVisual uv = inst.AddComponent<UnitVisual>();
            uv.CurrentSlotId = slotId;

            unitObjects.Add(slotId, uv);
            sv.SetMaterial(slotMaterialOccupied);
        }
    }

    private void ClearVisuals()
    {
        foreach (var sv in slotVisuals.Values)
        {
            if (sv != null) Destroy(sv.gameObject);
        }
        slotVisuals.Clear();

        foreach (var uv in unitObjects.Values)
        {
            if (uv != null) Destroy(uv.gameObject);
        }
        unitObjects.Clear();
    }

    private GameObject GetPrefabForUnit(string unitId)
    {
        foreach (var data in unitVisuals)
        {
            if (data.unitId == unitId) return data.prefab;
        }
        
        // Fallback: create primitive
        GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        primitive.SetActive(false); // So it doesn't spawn at 0,0 right away
        return primitive;
    }

    private void UpdateSlotColor(int slotId)
    {
        if (slotVisuals.TryGetValue(slotId, out SlotVisual sv))
        {
            bool isOccupied = unitObjects.ContainsKey(slotId);
            sv.SetMaterial(isOccupied ? slotMaterialOccupied : slotMaterialEmpty);
        }
    }

    #endregion

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200), GUI.skin.box);
        GUILayout.Label($"<b><size=16>Formation System Demo</size></b>");
        GUILayout.Space(10);
        
        if (controller.CurrentFormation != null)
        {
            GUILayout.Label($"<b>Current:</b> <color=#00ffff>{controller.CurrentFormation.FormationName}</color>");
            GUILayout.Label($"<b>Slots:</b> {controller.UnitsPlaced}/{controller.TotalSlots}");
        }

        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<< Prev")) controller.PreviousFormation();
        if (GUILayout.Button("Next >>")) controller.NextFormation();
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        if (GUILayout.Button("Auto Arrange Randomly")) controller.AutoArrangeUnits();
        if (GUILayout.Button("Clear All")) controller.ClearFormation();

        GUILayout.Space(10);
        GUILayout.Label("<color=#aaaaaa>- Left Click: Spawn Random Unit</color>");
        GUILayout.Label("<color=#aaaaaa>- Drag: Swap/Move Unit</color>");
        GUILayout.Label("<color=#aaaaaa>- Right Click: Remove Unit</color>");
        
        GUILayout.EndArea();
    }
}

// Simple helper components
public class SlotVisual : MonoBehaviour
{
    public int SlotId;
    public MeshRenderer VisualMesh;
    
    public void SetMaterial(Material mat)
    {
        if (VisualMesh != null && mat != null)
            VisualMesh.material = mat;
    }
}

public class UnitVisual : MonoBehaviour
{
    public int CurrentSlotId;
}
