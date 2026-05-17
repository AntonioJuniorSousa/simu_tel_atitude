using Godot;
using System;

public partial class GMeter : Control
{
    [Export] public SerialReader reader;
    
    private Font fallbackFont;
    private Color panelColor = new Color(0.05f, 0.05f, 0.08f, 0.82f);
    private Color cyanColor = new Color(0.0f, 0.85f, 1.0f, 0.9f);
    
    public override void _Ready()
    {
        fallbackFont = ThemeDB.FallbackFont;
        TooltipText = "Aceleração resultante medida pela IMU em múltiplos de g (9,8 m/s²). Em voo nivelado = 1.0G. Manobras aumentam este valor.";
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(0, 0, Size.X, Size.Y), panelColor);
        DrawRect(new Rect2(0, 0, Size.X, Size.Y), cyanColor, false, 1.0f);
        
        // Title
        string title = "G-METER";
        Vector2 titleSize = fallbackFont.GetStringSize(title, HorizontalAlignment.Left, -1, 11);
        DrawString(fallbackFont, new Vector2((Size.X - titleSize.X) / 2, 15), title, HorizontalAlignment.Left, -1, 11, cyanColor);

        float currentG = 1.0f;
        if (reader != null)
        {
            currentG = reader.CurrentG;
        }

        Vector2 center = new Vector2(Size.X / 2.0f, Size.Y / 2.0f + 10);
        float radius = 45f;

        // Gauge arc (210 to -30 deg) -> 240 extent
        // Range 0 to 4G -> 60 degrees per G
        
        float startAngle = Mathf.DegToRad(210);
        float endAngle = Mathf.DegToRad(-30);
        
        int nbPoints = 32;
        
        // Draw regions
        DrawArcRegion(center, radius, 210, 90, new Color(0, 1, 0, 1), 6f); // 0 to 2G (120 deg)
        DrawArcRegion(center, radius, 90, 30, new Color(1, 1, 0, 1), 6f);  // 2 to 3G (60 deg)
        DrawArcRegion(center, radius, 30, -30, new Color(1, 0, 0, 1), 6f); // 3 to 4G (60 deg)

        // Draw ticks
        for (float g = 0; g <= 4.0f; g += 0.5f)
        {
            float angleDeg = 210 - (g * (240f / 4f));
            float angleRad = Mathf.DegToRad(angleDeg);
            
            float length = (g % 1.0f == 0) ? 6f : 3f;
            
            Vector2 p1 = center + new Vector2(Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * radius;
            Vector2 p2 = center + new Vector2(Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * (radius - length);
            
            DrawLine(p1, p2, Colors.White, 1.5f);
        }

        // Clamp G to 4
        float clampedG = Mathf.Clamp(currentG, 0f, 4f);
        float needleAngleRad = Mathf.DegToRad(210 - (clampedG * 60f));
        Vector2 needleEnd = center + new Vector2(Mathf.Cos(needleAngleRad), -Mathf.Sin(needleAngleRad)) * (radius - 2);
        
        DrawLine(center, needleEnd, Colors.White, 2.0f);
        DrawCircle(center, 4f, Colors.White);

        // Value text
        string gLabel = currentG.ToString("F1") + "G";
        Vector2 labelSize = fallbackFont.GetStringSize(gLabel, HorizontalAlignment.Left, -1, 13);
        DrawString(fallbackFont, new Vector2((Size.X - labelSize.X) / 2, Size.Y - 5), gLabel, HorizontalAlignment.Left, -1, 13, Colors.White);
    }
    
    private void DrawArcRegion(Vector2 center, float radius, float startDeg, float endDeg, Color color, float thickness)
    {
        // Go from startDeg to endDeg (CCW on math, but Y is down in Godot)
        int segments = 16;
        var points = new Vector2[segments + 1];
        
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angleDeg = Mathf.Lerp(startDeg, endDeg, t);
            float angleRad = Mathf.DegToRad(angleDeg);
            points[i] = center + new Vector2(Mathf.Cos(angleRad), -Mathf.Sin(angleRad)) * radius;
        }
        
        for (int i = 0; i < segments; i++)
        {
            DrawLine(points[i], points[i+1], color, thickness);
        }
    }
}