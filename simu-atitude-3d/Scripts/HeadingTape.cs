using Godot;
using System;

public partial class HeadingTape : Control
{
    [Export] public AttitudeController attitudeController;
    
    private Font fallbackFont;
    private Color panelColor = new Color(0.05f, 0.05f, 0.08f, 0.82f);
    private Color cyanColor = new Color(0.0f, 0.85f, 1.0f, 0.9f);
    private Color textColorPrimary = new Color(0.0f, 1.0f, 0.4f, 1.0f);
    
    private const float PIXELS_PER_DEGREE = 3.0f;

    public override void _Ready()
    {
        fallbackFont = ThemeDB.FallbackFont;
        TooltipText = "Fita de rumo (Heading Tape). Mostra o ângulo de yaw em relação à referência inicial. Equivalente ao HSI em aviônica.";
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(0, 0, Size.X, Size.Y), panelColor);
        DrawLine(new Vector2(0, Size.Y - 1), new Vector2(Size.X, Size.Y - 1), cyanColor, 1.0f);
        
        float yaw = 0f;
        if (attitudeController != null)
        {
            Vector3 euler = attitudeController.GetEulerAnglesDeg();
            yaw = -euler.Y; // Adjust based on how yaw is reported, usually inverted for heading
            
            // Normalize yaw 0-360
            while (yaw < 0) yaw += 360f;
            while (yaw >= 360f) yaw -= 360f;
        }

        float centerX = Size.X / 2.0f;
        
        float startYaw = yaw - (centerX / PIXELS_PER_DEGREE);
        float endYaw = yaw + (centerX / PIXELS_PER_DEGREE);

        int minDegree = Mathf.FloorToInt(startYaw / 10f) * 10;
        int maxDegree = Mathf.CeilToInt(endYaw / 10f) * 10;

        for (int deg = minDegree; deg <= maxDegree; deg += 10)
        {
            float normalizedDeg = deg;
            while (normalizedDeg < 0) normalizedDeg += 360f;
            while (normalizedDeg >= 360f) normalizedDeg -= 360f;

            float x = centerX + (deg - yaw) * PIXELS_PER_DEGREE;

            if (deg % 30 == 0)
            {
                DrawLine(new Vector2(x, Size.Y - 15), new Vector2(x, Size.Y), cyanColor, 2.0f);
                
                string label = normalizedDeg.ToString("000");
                if (normalizedDeg == 0) label = "N";
                else if (normalizedDeg == 45) label = "NE";
                else if (normalizedDeg == 90) label = "L";
                else if (normalizedDeg == 135) label = "SE";
                else if (normalizedDeg == 180) label = "S";
                else if (normalizedDeg == 225) label = "SO";
                else if (normalizedDeg == 270) label = "O";
                else if (normalizedDeg == 315) label = "NO";
                
                Vector2 stringSize = fallbackFont.GetStringSize(label, HorizontalAlignment.Left, -1, 11);
                DrawString(fallbackFont, new Vector2(x - stringSize.X / 2, Size.Y - 20), label, HorizontalAlignment.Left, -1, 11, cyanColor);
            }
            else
            {
                DrawLine(new Vector2(x, Size.Y - 8), new Vector2(x, Size.Y), cyanColor, 1.0f);
            }
        }

        // Central pointer (triangle pointing down)
        var pointerPoints = new Vector2[]
        {
            new Vector2(centerX - 6, Size.Y - 10),
            new Vector2(centerX + 6, Size.Y - 10),
            new Vector2(centerX, Size.Y)
        };
        DrawColoredPolygon(pointerPoints, cyanColor);

        // Center box
        Rect2 boxRect = new Rect2(centerX - 20, 2, 40, 18);
        DrawRect(boxRect, new Color(0, 0, 0, 0.8f));
        DrawRect(boxRect, cyanColor, false, 1.0f);
        
        string centerLabel = Mathf.RoundToInt(yaw).ToString("000") + "°";
        Vector2 centerStrSize = fallbackFont.GetStringSize(centerLabel, HorizontalAlignment.Left, -1, 13);
        DrawString(fallbackFont, new Vector2(centerX - centerStrSize.X / 2, 15), centerLabel, HorizontalAlignment.Left, -1, 13, textColorPrimary);
    }
}