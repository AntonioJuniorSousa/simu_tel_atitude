using Godot;
using System;

public partial class PitchTape : Control
{
    [Export] public AttitudeController attitudeController;
    
    private Font fallbackFont;
    private Color panelColor = new Color(0.05f, 0.05f, 0.08f, 0.82f);
    private Color cyanColor = new Color(0.0f, 0.85f, 1.0f, 0.9f);
    private Color textColorPrimary = new Color(0.0f, 1.0f, 0.4f, 1.0f);
    
    private const float PIXELS_PER_DEGREE = 4.0f;

    public override void _Ready()
    {
        fallbackFont = ThemeDB.FallbackFont;
        TooltipText = "Fita de arfagem (Pitch Tape). Ângulo entre o nariz do veículo e o plano horizontal. Positivo = nariz acima do horizonte.";
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(0, 0, Size.X, Size.Y), panelColor);
        DrawLine(new Vector2(Size.X - 1, 0), new Vector2(Size.X - 1, Size.Y), cyanColor, 1.0f);
        
        float pitch = 0f;
        if (attitudeController != null)
        {
            Vector3 euler = attitudeController.GetEulerAnglesDeg();
            pitch = euler.X;
        }

        float centerY = Size.Y / 2.0f;
        
        float startPitch = pitch - (centerY / PIXELS_PER_DEGREE);
        float endPitch = pitch + (centerY / PIXELS_PER_DEGREE);

        int minDegree = Mathf.FloorToInt(startPitch / 5f) * 5;
        int maxDegree = Mathf.CeilToInt(endPitch / 5f) * 5;

        for (int deg = minDegree; deg <= maxDegree; deg += 5)
        {
            if (deg < -90 || deg > 90) continue;

            float y = centerY - (deg - pitch) * PIXELS_PER_DEGREE;

            if (deg % 10 == 0)
            {
                // Long tick
                DrawLine(new Vector2(Size.X - 15, y), new Vector2(Size.X, y), cyanColor, 2.0f);
                
                string label = (deg > 0 ? "+" : "") + deg.ToString();
                Vector2 stringSize = fallbackFont.GetStringSize(label, HorizontalAlignment.Left, -1, 11);
                DrawString(fallbackFont, new Vector2(Size.X - 18 - stringSize.X, y + stringSize.Y / 2 - 2), label, HorizontalAlignment.Left, -1, 11, cyanColor);
            }
            else
            {
                // Short tick
                DrawLine(new Vector2(Size.X - 8, y), new Vector2(Size.X, y), cyanColor, 1.0f);
            }
        }

        // Central pointer (triangle pointing right)
        var pointerPoints = new Vector2[]
        {
            new Vector2(Size.X - 10, centerY - 6),
            new Vector2(Size.X - 10, centerY + 6),
            new Vector2(Size.X, centerY)
        };
        DrawColoredPolygon(pointerPoints, cyanColor);

        // Center box
        Rect2 boxRect = new Rect2(2, centerY - 10, 36, 20);
        DrawRect(boxRect, new Color(0, 0, 0, 0.8f));
        DrawRect(boxRect, cyanColor, false, 1.0f);
        
        string centerLabel = (Mathf.RoundToInt(pitch) > 0 ? "+" : "") + Mathf.RoundToInt(pitch).ToString() + "°";
        Vector2 centerStrSize = fallbackFont.GetStringSize(centerLabel, HorizontalAlignment.Left, -1, 13);
        DrawString(fallbackFont, new Vector2(boxRect.Position.X + (boxRect.Size.X - centerStrSize.X) / 2, centerY + 5), centerLabel, HorizontalAlignment.Left, -1, 13, textColorPrimary);
    }
}