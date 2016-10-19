using UnityEngine;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;

// Player.cs

//
// Represents a single connected player or non-player force. Each connected player will have exactly one
// Player object associated with them.
//

public class Player : MonoBehaviour {
    // Is this Player the local Player?
    public bool IsMe { get { return m_isMe; } }
    public ReadOnlyCollection<Unit> SelectedUnits { get { return m_selectedUnits.AsReadOnly(); } }

    private bool m_isMe = true;
    // Currently dragging mouse (holding left mouse button and moving)?
    private bool m_dragging = false;
    private Vector2 m_dragStartPosition;
    private List<Unit> m_selectedUnits = new List<Unit>();
    private Vector3 start, end;
    private float m_cameraPanSpeed = 10.0f;

    void Start() {
        PlayerManager.Manager.RegisterPlayer(this);
    }

    void Update() {
        if (!m_isMe)
            return;
        EvaluatePlayerInput();
        Debug.DrawRay(start, end);
    }

    void EvaluatePlayerInput() {
        EvaluateMouseInput();
    }

    void EvaluateMouseInput() {
        EvaluateMouseCameraPan();
        EvaluateMouseDrag();

        // If the right mouse button was just clicked...
        if(Input.GetMouseButtonDown(1)) {
            //...issue a move order to our currently selected Units.
            CalculateAndIssueMoveOrders();
        }
    }

    void EvaluateMouseDrag() {
        // If the left mouse button is currently being held down...
        if (Input.GetMouseButton(0)) {
            // ...and we're not dragging, then start dragging.
            if (!m_dragging) {
                StartDragging();
            } // Otherwise, draw the drag box.
            else {
                // draw box
            }
        }
        else { // LMB is not being held down.
            if (m_dragging) {
                StopDragging();
            }
        }
    }

    void EvaluateMouseCameraPan() {
        Vector3 mousePos = Input.mousePosition;
        float x = mousePos.x;
        float y = mousePos.y;

        float cameraPanMagnitude = m_cameraPanSpeed * Time.deltaTime;
        if (x < 10)
            MoveCamera(new Vector3(-cameraPanMagnitude, 0, 0));
        if (x > Screen.width - 10)
            MoveCamera(new Vector3(cameraPanMagnitude, 0, 0));
        if (y < 10)
            MoveCamera(new Vector3(0, 0, -cameraPanMagnitude));
        if (y > Screen.height - 10)
            MoveCamera(new Vector3(0, 0, cameraPanMagnitude));
    }

    void MoveCamera(Vector3 vector) {
        Camera.main.transform.position += vector;
    }

    void CalculateAndIssueMoveOrders() {
        if (m_selectedUnits.Count == 0)
            return;
        List<float> extents = new List<float>();
        foreach(Unit unit in m_selectedUnits) {
            extents.Add(unit.GreatestExtent);
        }

        float greatestExtent = 0;
        foreach(float extent in extents) {
            if (extent > greatestExtent)
                greatestExtent = extent;
        }

        int unitCount = m_selectedUnits.Count;
        int numberOfRows = (int)Mathf.Floor(Mathf.Sqrt(unitCount));
        int unitsPerRow = unitCount / numberOfRows;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10000;
        Vector3 basePosition = Camera.main.ScreenToWorldPoint(mousePos);
        RaycastHit hit;
        start = Camera.main.transform.position;
        end = basePosition;
        Physics.Raycast(Camera.main.transform.position, basePosition.normalized, out hit);
        mousePos.z = hit.distance;
        basePosition = Camera.main.ScreenToWorldPoint(mousePos);

        float offsetPerUnit = greatestExtent * 1.5f * 4;
        float formationWidth = (unitsPerRow - 1) * offsetPerUnit;
        float formationHeight = (numberOfRows - 1) * offsetPerUnit;

        float minX = basePosition.x - (formationWidth / 2);
        float minZ = basePosition.z - (formationHeight / 2);

        int i = 0;

        m_selectedUnits.Sort(SelectionComparisonDelegate);

        foreach(Unit unit in m_selectedUnits) {
            int column = i % unitsPerRow;
            int row = (int)Mathf.Floor((float)i / unitsPerRow);
            float x = minX + (column * offsetPerUnit);
            float z = minZ + (row * offsetPerUnit);
            Vector3 targetPos = new Vector3(x, 0, z);
            unit.IssueOrder(new MoveOrder(targetPos));
            i++;
        }
    }

    void StartDragging() {
        m_dragging = true;
        m_dragStartPosition = Input.mousePosition;
    }

    void StopDragging() {
        Vector2 dragStopPosition = Input.mousePosition;
        float width = dragStopPosition.x - m_dragStartPosition.x;
        float height = dragStopPosition.y - m_dragStartPosition.y;
        SelectUnitsWithinRectangle(new Rect(m_dragStartPosition.x, m_dragStartPosition.y, width, height));
        m_dragging = false;
    }

    void SelectUnitsWithinRectangle(Rect rectangle) {
        List<Unit> newSelection = new List<Unit>();
        foreach(Unit unit in Unit.Units) {
            if(unit.VertexWithinRectangle(rectangle)) {
                newSelection.Add(unit);
            }
        }

        if (newSelection.Count > 0) {
            m_selectedUnits = newSelection;
            foreach (Unit unit in Unit.Units) {
                unit.SelectionChanged();
            }
        }
    }

    int SelectionComparisonDelegate(Unit a, Unit b) {
        Vector3 arbitrarilyLargeVector = new Vector3(-1000000, 0, 1000000);
        if (a.GetDistance(arbitrarilyLargeVector) < b.GetDistance(arbitrarilyLargeVector))
            return 1;
        return -1;
    }
}
