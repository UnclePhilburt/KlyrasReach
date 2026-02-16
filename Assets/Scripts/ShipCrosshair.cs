using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Draws Star Citizen style crosshair:
/// - Small dot = where mouse is aiming (lead indicator)
/// - Large circle = where ship is pointing
/// </summary>
public class ShipCrosshair : MonoBehaviour
{
    [Header("Crosshair Settings")]
    [Tooltip("Size of the small center dot")]
    [SerializeField] private float _dotSize = 4f;

    [Tooltip("Size of the large circle (ship direction)")]
    [SerializeField] private float _circleRadius = 30f;

    [Tooltip("Thickness of the circle line")]
    [SerializeField] private float _circleThickness = 2f;

    [Tooltip("Color of the crosshair")]
    [SerializeField] private Color _crosshairColor = Color.cyan;

    [Tooltip("How far the mouse can move from center (screen percentage)")]
    [SerializeField] private float _maxMouseOffset = 0.15f; // 15% of screen

    private Camera _camera;
    private KlyrasReach.Player.ShipController _shipController;
    private Texture2D _dotTexture;
    private Texture2D _circleTexture;
    private Vector2 _mouseOffset = Vector2.zero;

    void Start()
    {
        _camera = Camera.main;

        // Create dot texture
        _dotTexture = new Texture2D(1, 1);
        _dotTexture.SetPixel(0, 0, Color.white);
        _dotTexture.Apply();

        // Create circle texture
        CreateCircleTexture();
    }

    void Update()
    {
        // Only update when ship is active
        if (_shipController == null || !_shipController.IsActive)
        {
            _mouseOffset = Vector2.zero;
            return;
        }

        // Get mouse delta (how much mouse moved this frame)
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            // Accumulate mouse movement
            _mouseOffset += mouseDelta * 0.5f; // Scale down for smoother control

            // Clamp to max distance
            float maxOffset = Screen.height * _maxMouseOffset;
            if (_mouseOffset.magnitude > maxOffset)
            {
                _mouseOffset = _mouseOffset.normalized * maxOffset;
            }

            // Slowly drift back to center when mouse isn't moving much
            if (mouseDelta.magnitude < 0.1f)
            {
                _mouseOffset = Vector2.Lerp(_mouseOffset, Vector2.zero, Time.deltaTime * 2f);
            }
        }
    }

    void OnGUI()
    {
        // Only show when ship is active
        if (_shipController == null || !_shipController.IsActive)
        {
            // Try to find active ship
            FindActiveShip();
            return;
        }

        GUI.color = _crosshairColor;

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        // Draw large circle at screen center (ship direction indicator)
        Rect circleRect = new Rect(
            screenCenter.x - _circleRadius,
            screenCenter.y - _circleRadius,
            _circleRadius * 2,
            _circleRadius * 2
        );
        DrawCircle(circleRect, _circleThickness);

        // Draw small dot at mouse offset from center (lead indicator)
        Vector2 dotPos = screenCenter + _mouseOffset;
        Rect dotRect = new Rect(
            dotPos.x - _dotSize / 2f,
            Screen.height - dotPos.y - _dotSize / 2f, // Flip Y for GUI
            _dotSize,
            _dotSize
        );
        GUI.DrawTexture(dotRect, _dotTexture);

        GUI.color = Color.white;
    }

    void CreateCircleTexture()
    {
        int size = 64;
        _circleTexture = new Texture2D(size, size);
        Color transparent = new Color(0, 0, 0, 0);
        Color white = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pixelPos = new Vector2(x - size / 2f, y - size / 2f);
                float distance = pixelPos.magnitude;
                float radius = size / 2f;

                // Draw circle edge
                if (Mathf.Abs(distance - radius) < 2f)
                {
                    _circleTexture.SetPixel(x, y, white);
                }
                else
                {
                    _circleTexture.SetPixel(x, y, transparent);
                }
            }
        }

        _circleTexture.Apply();
    }

    void DrawCircle(Rect rect, float thickness)
    {
        // Draw circle outline using 4 arcs (simple approximation)
        int segments = 32;
        Vector2 center = new Vector2(rect.x + rect.width / 2f, rect.y + rect.height / 2f);
        float radius = rect.width / 2f;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = (i / (float)segments) * Mathf.PI * 2f;
            float angle2 = ((i + 1) / (float)segments) * Mathf.PI * 2f;

            Vector2 p1 = center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * radius;
            Vector2 p2 = center + new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * radius;

            DrawLine(p1, p2, thickness);
        }
    }

    void DrawLine(Vector2 start, Vector2 end, float thickness)
    {
        Vector2 direction = (end - start).normalized;
        float distance = Vector2.Distance(start, end);

        Rect lineRect = new Rect(
            start.x,
            start.y,
            distance,
            thickness
        );

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        GUIUtility.RotateAroundPivot(angle, start);
        GUI.DrawTexture(lineRect, _dotTexture);
        GUIUtility.RotateAroundPivot(-angle, start);
    }

    void FindActiveShip()
    {
        // Only search every 30 frames
        if (Time.frameCount % 30 != 0)
            return;

        GameObject[] ships = GameObject.FindGameObjectsWithTag("Ship");
        foreach (GameObject ship in ships)
        {
            var controller = ship.GetComponent<KlyrasReach.Player.ShipController>();
            if (controller != null && controller.IsActive)
            {
                _shipController = controller;
                return;
            }
        }
    }
}
