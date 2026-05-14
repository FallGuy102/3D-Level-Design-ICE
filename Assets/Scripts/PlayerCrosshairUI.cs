using UnityEngine;

public class PlayerCrosshairUI : MonoBehaviour
{
    [SerializeField] private PlayerIceGunController iceGunController;
    [SerializeField] private PlayerHammerController hammerController;
    [SerializeField] private Color dotColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color aimColor = new Color(0.6f, 0.95f, 1f, 0.95f);
    [SerializeField] private Color shatterColor = new Color(1f, 0.72f, 0.25f, 1f);
    [SerializeField] private float dotSize = 5f;
    [SerializeField] private float lineLength = 12f;
    [SerializeField] private float lineThickness = 2f;
    [SerializeField] private float lineGap = 7f;
    [SerializeField] private float shatterBracketSize = 22f;

    private Texture2D whiteTexture;

    private void Awake()
    {
        if (iceGunController == null)
            iceGunController = GetComponent<PlayerIceGunController>();

        if (hammerController == null)
            hammerController = GetComponent<PlayerHammerController>();

        whiteTexture = Texture2D.whiteTexture;
    }

    private void OnGUI()
    {
        if (whiteTexture == null)
            whiteTexture = Texture2D.whiteTexture;

        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        bool isAiming = iceGunController != null && iceGunController.IsAiming;
        bool canShatter = hammerController != null && hammerController.IsLookingAtBreakableIce;

        if (isAiming)
            DrawCross(center, canShatter ? shatterColor : aimColor);
        else
            DrawDot(center, canShatter ? shatterColor : dotColor);

        if (canShatter)
            DrawShatterCue(center);
    }

    private void DrawDot(Vector2 center, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(new Rect(center.x - dotSize * 0.5f, center.y - dotSize * 0.5f, dotSize, dotSize), whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawCross(Vector2 center, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(new Rect(center.x - lineGap - lineLength, center.y - lineThickness * 0.5f, lineLength, lineThickness), whiteTexture);
        GUI.DrawTexture(new Rect(center.x + lineGap, center.y - lineThickness * 0.5f, lineLength, lineThickness), whiteTexture);
        GUI.DrawTexture(new Rect(center.x - lineThickness * 0.5f, center.y - lineGap - lineLength, lineThickness, lineLength), whiteTexture);
        GUI.DrawTexture(new Rect(center.x - lineThickness * 0.5f, center.y + lineGap, lineThickness, lineLength), whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawShatterCue(Vector2 center)
    {
        GUI.color = shatterColor;
        float half = shatterBracketSize * 0.5f;
        float shortLine = shatterBracketSize * 0.35f;

        GUI.DrawTexture(new Rect(center.x - half, center.y - half, shortLine, lineThickness), whiteTexture);
        GUI.DrawTexture(new Rect(center.x - half, center.y - half, lineThickness, shortLine), whiteTexture);
        GUI.DrawTexture(new Rect(center.x + half - shortLine, center.y - half, shortLine, lineThickness), whiteTexture);
        GUI.DrawTexture(new Rect(center.x + half - lineThickness, center.y - half, lineThickness, shortLine), whiteTexture);
        GUI.DrawTexture(new Rect(center.x - half, center.y + half - lineThickness, shortLine, lineThickness), whiteTexture);
        GUI.DrawTexture(new Rect(center.x - half, center.y + half - shortLine, lineThickness, shortLine), whiteTexture);
        GUI.DrawTexture(new Rect(center.x + half - shortLine, center.y + half - lineThickness, shortLine, lineThickness), whiteTexture);
        GUI.DrawTexture(new Rect(center.x + half - lineThickness, center.y + half - shortLine, lineThickness, shortLine), whiteTexture);
        GUI.color = Color.white;
    }
}
