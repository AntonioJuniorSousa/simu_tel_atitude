using Godot;
using System.Collections.Generic;

public partial class ArtificialHorizon : Control
{
    [Export] public AttitudeController attitudeController;
    private Color _skyColor = new Color(0.2f, 0.6f, 0.9f);
    private Color _groundColor = new Color(0.6f, 0.4f, 0.2f);
    
    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 center = Size / 2.0f;
        float radius = Mathf.Min(Size.X, Size.Y) / 2.0f - 4.0f;

        if (attitudeController == null)
        {
            DrawCircle(center, radius, Colors.Black);
            DrawArc(center, radius, 0, Mathf.Pi * 2, 64, Colors.White, 3);
            return;
        }
        
        Vector3 euler = attitudeController.GetEulerAnglesDeg();
        float rollRads = Mathf.DegToRad(euler.Z);
        float pitchDeg = euler.X;

        // Apply roll rotation
        DrawSetTransform(center, -rollRads, Vector2.One);
        
        // Pitch offset
        float pitchOffset = pitchDeg * 3.0f;
        pitchOffset = Mathf.Clamp(pitchOffset, -radius, radius);
        
        float intersectionX = Mathf.Sqrt(Mathf.Max(0, radius * radius - pitchOffset * pitchOffset));

        List<Vector2> skyPts = new List<Vector2>();
        List<Vector2> groundPts = new List<Vector2>();

        int arcPoints = 32;

        float theta = Mathf.Asin(pitchOffset / radius);

        // Sky (cima, Y < pitchOffset)
        // Vai de theta atravessando o topo (-PI/2) até o lado esquerdo (-PI - theta)
        float skyStart = theta;
        float skyEnd = -Mathf.Pi - theta;

        for (int i = 0; i <= arcPoints; i++)
        {
            float t = (float)i / arcPoints;
            float angle = Mathf.Lerp(skyStart, skyEnd, t);
            skyPts.Add(new Vector2(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle)));
        }

        // Terra (baixo, Y > pitchOffset)
        // Vai de theta atravessando o fundo (PI/2) até o lado esquerdo (PI - theta)
        float groundStart = theta;
        float groundEnd = Mathf.Pi - theta;

        for (int i = 0; i <= arcPoints; i++)
        {
            float t = (float)i / arcPoints;
            float angle = Mathf.Lerp(groundStart, groundEnd, t);
            groundPts.Add(new Vector2(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle)));
        }

        DrawColoredPolygon(skyPts.ToArray(), _skyColor);
        DrawColoredPolygon(groundPts.ToArray(), _groundColor);

        // Horizonte (linha central do instrumento dividindo céu e terra)
        DrawLine(new Vector2(-intersectionX, pitchOffset), new Vector2(intersectionX, pitchOffset), Colors.White, 3);
        
        // Marcações de pitch (escala de atitude)
        for (int i = -9; i <= 9; i++)
        {
            if (i == 0) continue;
            
            float pLineY = pitchOffset - (i * 10 * 3.0f); // 3px por grau, 10 graus de espaçamento
            
            // Só desenha se estiver dentro do círculo do instrumento
            if (pLineY > -radius && pLineY < radius)
            {
                float lineWidth = (i % 2 == 0) ? 20f : 10f;
                // Clip lineWidth against intersection circle bounds max
                float maxHalfWidth = Mathf.Sqrt(Mathf.Max(0, radius * radius - pLineY * pLineY));
                lineWidth = Mathf.Min(lineWidth, maxHalfWidth);
                
                DrawLine(new Vector2(-lineWidth, pLineY), new Vector2(lineWidth, pLineY), Colors.White, 2);
            }
        }

        // Borda do instrumento
        DrawArc(Vector2.Zero, radius, 0, Mathf.Pi * 2, 64, Colors.White, 3);
        
        // Retorna a transformação para a central fixa
        DrawSetTransform(center, 0, Vector2.One);
        
        // Avião simplificado ao centro (linha amarela e ponto)
        DrawLine(new Vector2(-30, 0), new Vector2(-10, 0), Colors.Yellow, 3);   // Asa esquerda
        DrawLine(new Vector2(10, 0), new Vector2(30, 0), Colors.Yellow, 3);     // Asa direita
        DrawCircle(Vector2.Zero, 3, Colors.Yellow);                             // Centro
    }
}
