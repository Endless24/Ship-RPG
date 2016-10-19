using UnityEngine;
using System.Collections;

public class PercentageBar : MonoBehaviour {
    public float Maximum {
        get { return m_maximum; }
        set { m_maximum = value; UpdatePercentage(); }
    }

    public float Value {
        get { return m_value; }
        set { m_value = value; UpdatePercentage(); }
    }

    public int ForcedHeight {
        get { return m_forcedHeight; }
    }

    public int ForcedWidth {
        get { return m_forcedWidth; }
    }

    public int LastDerivedHeight {
        get { return m_derivedHeight; }
    }

    public int LastDerivedWidth {
        get { return m_derivedWidth; }
    }

    public Vector2 LastPositionDrawnAt {
        get { return m_lastPosition; }
    }

    public Color FilledColor {
        get { return m_filledColor; }
        set { m_filledColor = value; }
    }

    public Color EmptyColor {
        get { return m_emptyColor; }
        set { m_emptyColor = value; }
    }

    float m_percentage;
    float m_value;
    float m_maximum;

    int m_forcedWidth;
    int m_derivedWidth;
    int m_forcedHeight;
    int m_derivedHeight;

    Vector2 m_offset;
    Vector2 m_lastPosition;
    bool m_drawAbove;

    Color m_emptyColor;
    Color m_filledColor;
    Texture2D m_filledTexture;
    Mesh m_meshAttachedTo;
    PercentageBar m_barAttachedTo;

    bool hasInit = false;

    public void Start() {

    }

    public void Update() {

    }

    void UpdatePercentage() {
        if (m_maximum == 0) {
            m_percentage = 1.0f;
            return;
        }
        m_percentage = m_value / m_maximum;
    }

    void OnGUI() {
        if (!hasInit) {
            Debug.LogWarning("OnGUI called on uninitialized PercentageBar" + this);
            return;
        }
        Vector2 pos;
        int width;
        int height;

        if (m_barAttachedTo == null) {
            Camera camera = Camera.main;
            float minX = GetLowestXVertex(gameObject.transform, gameObject.transform.localScale, m_meshAttachedTo.vertices, camera).x;
            float maxX = GetHighestXVertex(gameObject.transform, gameObject.transform.localScale, m_meshAttachedTo.vertices, camera).x;
            float minY = GetLowestYVertex(gameObject.transform, gameObject.transform.localScale, m_meshAttachedTo.vertices, camera).y;
            float maxY = GetHighestYVertex(gameObject.transform, gameObject.transform.localScale, m_meshAttachedTo.vertices, camera).y;

            width = (int)(maxX - minX);
            int baseWidth = width;
            width = Mathf.Max((int)(maxX - minX), 60);
            minX -= (width - baseWidth) / 2;
            maxX += (width - baseWidth) / 2;
            height = Mathf.Max(width / 10, 8);

            pos = new Vector2(minX + m_offset.x, m_drawAbove ? maxY - m_offset.y : minY - m_offset.y);
        }
        else {
            pos = m_barAttachedTo.LastPositionDrawnAt;
            width = m_barAttachedTo.LastDerivedWidth;
            height = m_barAttachedTo.LastDerivedHeight;
        }

        if (m_forcedWidth > 0 || m_forcedHeight > 0) {
            width = m_forcedWidth;
            height = m_forcedHeight;
        }

        m_derivedWidth = width;
        m_derivedHeight = height;
        if (m_drawAbove) {
            pos.y -= m_offset.y;
            pos.y -= height;
        }
        else {
            pos.y += m_offset.y;
            if(m_barAttachedTo)
                pos.y += m_barAttachedTo.LastDerivedHeight;
        }
        pos.x += m_offset.x;

        m_lastPosition = pos;

        
        // We want to leave a one pixel wide margin on every side for the black border.
        float emptyX = pos.x + (width * m_percentage)   + 0;
        float emptyY = pos.y                            + 1;
        float emptyWidth = width * (1 - m_percentage)   - 1; emptyWidth = Mathf.Max(emptyWidth, 0);
        float emptyHeight = height                      - 2; emptyHeight = Mathf.Max(emptyHeight, 0);

        float filledX = pos.x                           + 1;
        float filledY = pos.y                           + 1;
        float filledWidth = width * m_percentage        - 1; filledWidth = Mathf.Max(filledWidth, 0);
        float filledHeight = height                     - 2; filledHeight = Mathf.Max(filledHeight, 0);

        // Draw the black border.
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(pos.x, pos.y, width, height), m_filledTexture, ScaleMode.StretchToFill, false);
        // Draw the empty portion.
        GUI.color = m_emptyColor;
        GUI.DrawTexture(new Rect(emptyX, emptyY, emptyWidth, emptyHeight), m_filledTexture, ScaleMode.StretchToFill, false);
        // Draw the filled portion.
        GUI.color = m_filledColor;
        GUI.DrawTexture(new Rect(filledX, filledY, filledWidth, filledHeight), m_filledTexture, ScaleMode.StretchToFill, false);
    }

    public void Init(PercentageBar attachTo, float max = 0, float current = 0, bool drawAbove = true, Color emptyColor = new Color(), Color filledColor = new Color(), Vector2 offset = new Vector2(), Vector2 forceDimensions = new Vector2()) {
        m_maximum = max;
        m_value = current;
        m_emptyColor = emptyColor;
        m_filledColor = filledColor;
        m_barAttachedTo = attachTo;
        m_drawAbove = drawAbove;
        m_offset = offset;

        if (forceDimensions != Vector2.zero) {
            m_forcedWidth = (int)forceDimensions.x;
            m_forcedHeight = (int)forceDimensions.y;
        }

        Setup();
    }

    public void Init(float max = 0, float current = 0, bool drawAbove = true, Color emptyColor = new Color(), Color filledColor = new Color(), Vector2 offset = new Vector2(), Vector2 forceDimensions = new Vector2()) {
        m_maximum = max;
        m_value = current;
        m_emptyColor = emptyColor;
        m_filledColor = filledColor;
        m_drawAbove = drawAbove;
        m_offset = offset;

        if (forceDimensions != Vector2.zero) {
            m_forcedWidth = (int)forceDimensions.x;
            m_forcedHeight = (int)forceDimensions.y;
        }

        Setup();
    }

    private void Setup() {
        m_meshAttachedTo = GetComponent<MeshFilter>().mesh;
        m_filledTexture = new Texture2D(1, 1);
        m_filledTexture.SetPixel(1, 1, Color.gray);

        UpdatePercentage();
        hasInit = true;
    }

    private Vector2 GetLowestXVertex(Transform origin, Vector3 scale, Vector3[] vertices, Camera camera) {
        Vector3 most = new Vector3();
        foreach (Vector3 vertex in vertices) {
            if (most == new Vector3()) {
                most = vertex;
                most = camera.WorldToScreenPoint(origin.TransformPoint(most));
            }
            Vector3 prospectiveMost = vertex;
            prospectiveMost = camera.WorldToScreenPoint(origin.TransformPoint(prospectiveMost));
            if (prospectiveMost.x < most.x)
                most = prospectiveMost;
        }
        return most;
    }

    private Vector2 GetHighestXVertex(Transform origin, Vector3 scale, Vector3[] vertices, Camera camera) {
        Vector3 most = new Vector3();
        foreach (Vector3 vertex in vertices) {
            if (most == new Vector3()) {
                most = vertex;
                most = camera.WorldToScreenPoint(origin.TransformPoint(most));
            }
            Vector3 prospectiveMost = vertex;
            prospectiveMost = camera.WorldToScreenPoint(origin.TransformPoint(prospectiveMost));
            if (prospectiveMost.x > most.x)
                most = prospectiveMost;
        }
        return most;
    }

    private Vector2 GetLowestYVertex(Transform origin, Vector3 scale, Vector3[] vertices, Camera camera) {
        Vector3 most = new Vector3();
        foreach (Vector3 vertex in vertices) {
            if (most == Vector3.zero) {
                most = vertex;
                most = camera.WorldToScreenPoint(origin.TransformPoint(most));
                most.y = camera.pixelHeight - most.y;
            }
            Vector3 prospectiveMost = vertex;
            prospectiveMost = camera.WorldToScreenPoint(origin.TransformPoint(prospectiveMost));
            prospectiveMost.y = camera.pixelHeight - prospectiveMost.y;
            if (prospectiveMost.y > most.y)
                most = prospectiveMost;
        }
        return most;
    }

    private Vector2 GetHighestYVertex(Transform origin, Vector3 scale, Vector3[] vertices, Camera camera) {
        Vector3 most = new Vector3();
        foreach (Vector3 vertex in vertices) {
            if (most == new Vector3()) {
                most = vertex;
                most = camera.WorldToScreenPoint(origin.TransformPoint(most));
                most.y = camera.pixelHeight - most.y;
            }
            Vector3 prospectiveMost = vertex;
            prospectiveMost = camera.WorldToScreenPoint(origin.TransformPoint(prospectiveMost));
            prospectiveMost.y = camera.pixelHeight - prospectiveMost.y;
            if (prospectiveMost.y < most.y)
                most = prospectiveMost;
        }
        return most;
    }
}