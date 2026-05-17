using Godot;

public partial class CompassRose : Control
{
    [Export] public AttitudeController attitudeController;

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (attitudeController == null) return;

        Vector2 center = Size / 2;
        float radius = Mathf.Min(Size.X, Size.Y) / 2.0f;
        
        float yawRads = Mathf.DegToRad(attitudeController.GetEulerAnglesDeg().Y);
        
        // Círculo principal
        DrawCircle(center, radius, new Color(0, 0, 0, 0.5f));
        DrawArc(center, radius, 0, Mathf.Pi * 2, 32, Colors.White, 2);
        
        DrawSetTransform(center, yawRads, Vector2.One);
        
        // Desenha marcações
        var font = ThemeDB.FallbackFont;
        for (int i = 0; i < 360; i += 30)
        {
            float rad = Mathf.DegToRad(i);
            Vector2 dir = new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad));
            Vector2 start = dir * (radius - 10);
            Vector2 end = dir * radius;
            DrawLine(start, end, Colors.White, 2);
            
            if (i % 90 == 0)
            {
                string text = i == 0 ? "N" : i == 90 ? "L" : i == 180 ? "S" : "O";
                Vector2 textPos = (dir * (radius - 25)) - font.GetStringSize(text, fontSize: 16) / 2;
                DrawString(font, textPos + new Vector2(0, 12), text, alignment: HorizontalAlignment.Center, width: -1f, fontSize: 16, modulate: Colors.White);
            }
        }

        // Ponteiro fixo
        DrawSetTransform(center, 0, Vector2.One);
        DrawLine(new Vector2(0, -radius), new Vector2(0, -radius + 15), Colors.Red, 4);
    }
}
