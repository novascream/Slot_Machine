using UnityEngine;
using UnityEngine.UI;

public class UILineConnector : MonoBehaviour
{
    [Header("Line 1 (Machine to Wallet)")]
    public GameObject start1;
    public GameObject end1;
    public Image lineImage1;

    [Header("Line 2 (Machine to Log)")]
    public GameObject start2;
    public GameObject end2;
    public Image lineImage2;

    [Header("Pulse Settings")]
    [SerializeField] private float baseThickness = 4f;
    [SerializeField] private float pulseAmount = 2f;
    [SerializeField] private float pulseSpeed = 10f;

    private bool _showLine1;
    private bool _showLine2;

    void Update()
    {
        if (_showLine1) UpdateLine(start1, end1, lineImage1);
        if (_showLine2) UpdateLine(start2, end2, lineImage2);
    }

    public void ToggleLine1(bool active) { _showLine1 = active; lineImage1.gameObject.SetActive(active); }
    public void ToggleLine2(bool active) { _showLine2 = active; lineImage2.gameObject.SetActive(active); }

    private void UpdateLine(GameObject start, GameObject end, Image line)
    {
        if (start == null || end == null || line == null) return;

        RectTransform startRT = start.GetComponent<RectTransform>();
        RectTransform endRT = end.GetComponent<RectTransform>();
        RectTransform lineRT = line.rectTransform;

        // 1. Position & Rotation
        Vector2 direction = endRT.anchoredPosition - startRT.anchoredPosition;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        lineRT.anchoredPosition = startRT.anchoredPosition;
        lineRT.localRotation = Quaternion.Euler(0, 0, angle);

        // 2. Pulsing Thickness Logic
        float currentThickness = baseThickness + (Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);

        // 3. Set Size (Width = Distance, Height = Pulsing Thickness)
        lineRT.sizeDelta = new Vector2(distance, currentThickness);
    }
}