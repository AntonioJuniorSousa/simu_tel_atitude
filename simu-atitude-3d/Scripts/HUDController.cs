using Godot;
using System;

public partial class HUDController : Control
{
    [Export] public SerialReader reader;
    [Export] public AttitudeController attitudeController;

    private Label _rollValue;
    private Label _pitchValue;
    private Label _yawValue;
    private Label _linkValue;
    private Label _magValue;
    private Label _errorValue;
    private Label _lastValue;
    private Label _freezeAlert;
    
    private double _timeAccumulator;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Pass;

        var panelContainer = new PanelContainer();
        panelContainer.SetAnchorsPreset(LayoutPreset.TopRight);
        panelContainer.Position = new Vector2(-230, 52); // Offset to be inside and below HeadingTape
        panelContainer.Size = new Vector2(220, 220);
        
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0.05f, 0.05f, 0.08f, 0.82f);
        styleBox.BorderColor = new Color(0.0f, 0.85f, 1.0f, 0.9f);
        styleBox.BorderWidthBottom = 1;
        styleBox.BorderWidthTop = 1;
        styleBox.BorderWidthLeft = 1;
        styleBox.BorderWidthRight = 1;
        panelContainer.AddThemeStyleboxOverride("panel", styleBox);

        AddChild(panelContainer);

        var marginContainer = new MarginContainer();
        marginContainer.AddThemeConstantOverride("margin_top", 10);
        marginContainer.AddThemeConstantOverride("margin_bottom", 10);
        marginContainer.AddThemeConstantOverride("margin_left", 10);
        marginContainer.AddThemeConstantOverride("margin_right", 10);
        panelContainer.AddChild(marginContainer);

        var vbox = new VBoxContainer();
        marginContainer.AddChild(vbox);

        // Header
        var titleLabel = CreateLabel("◈ TELEMETRIA", new Color(0.0f, 0.85f, 1.0f, 0.9f), 13);
        vbox.AddChild(titleLabel);
        
        var separator = new ColorRect();
        separator.Color = new Color(0.0f, 0.85f, 1.0f, 0.9f);
        separator.CustomMinimumSize = new Vector2(0, 1);
        vbox.AddChild(separator);

        _rollValue = AddRow(vbox, "ROLL", "Rotação em torno do eixo longitudinal (asa). Positivo = asa esquerda para baixo.");
        _pitchValue = AddRow(vbox, "PITCH", "Inclinação do nariz. Positivo = nariz para cima (cabrada).");
        _yawValue = AddRow(vbox, "YAW", "Rotação em torno do eixo vertical. Equivalente ao rumo magnético relativo.");

        var separator2 = new ColorRect();
        separator2.Color = new Color(0.0f, 0.85f, 1.0f, 0.4f);
        separator2.CustomMinimumSize = new Vector2(0, 1);
        vbox.AddChild(separator2);

        _linkValue = AddRow(vbox, "LINK", "Taxa de pacotes recebidos via serial por segundo. Abaixo de 30 Hz indica problema de comunicação.");
        _magValue = AddRow(vbox, "MAG(Q)", "Magnitude do quaternion. Deve ser ≈ 1.0000. Desvio indica erro de normalização no filtro Madgwick.");
        _errorValue = AddRow(vbox, "ERROS", "Pacotes recebidos com formato inválido ou falha de parse acumulados desde o início da sessão.");
        _lastValue = AddRow(vbox, "ÚLTIMO", "Tempo desde o último pacote válido. Acima de 1s aciona alerta de freeze.");

        _freezeAlert = CreateLabel("⚠ FREEZE", new Color(1.0f, 0.3f, 0.1f, 1.0f), 13);
        _freezeAlert.Visible = false;
        vbox.AddChild(_freezeAlert);
    }

    private Label AddRow(VBoxContainer parent, string name, string tooltip)
    {
        var hbox = new HBoxContainer();
        
        var nameLabel = CreateLabel(name, new Color(0.7f, 0.7f, 0.7f, 1.0f), 11);
        nameLabel.MouseFilter = MouseFilterEnum.Stop;
        nameLabel.TooltipText = tooltip;
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        
        var valueLabel = CreateLabel("", new Color(0.0f, 1.0f, 0.4f, 1.0f), 13);
        valueLabel.HorizontalAlignment = HorizontalAlignment.Right;
        
        hbox.AddChild(nameLabel);
        hbox.AddChild(valueLabel);
        parent.AddChild(hbox);
        
        return valueLabel;
    }

    private Label CreateLabel(string text, Color color, int fontSize)
    {
        var label = new Label();
        label.Text = text;
        label.LabelSettings = new LabelSettings
        {
            FontColor = color,
            FontSize = fontSize,
            Font = ThemeDB.FallbackFont
        };
        return label;
    }

    public override void _Process(double delta)
    {
        if (reader == null || attitudeController == null) return;

        Vector3 euler = attitudeController.GetEulerAnglesDeg();
        float mag = reader.LatestQuaternion.Length();
        double timeSinceLast = (DateTime.Now - reader.LastPacketTime).TotalSeconds;

        _rollValue.Text = $"{euler.Z:F1}°";
        _pitchValue.Text = $"{euler.X:F1}°";
        _yawValue.Text = $"{euler.Y:F1}°";

        _linkValue.Text = $"{reader.CurrentHz} Hz";
        _magValue.Text = $"{mag:F4}";
        _errorValue.Text = $"{reader.ErrorCount}";
        _lastValue.Text = $"{timeSinceLast:F2}s";

        if (timeSinceLast > 1.0)
        {
            _lastValue.LabelSettings.FontColor = new Color(1.0f, 0.3f, 0.1f, 1.0f);
            
            _timeAccumulator += delta;
            if (_timeAccumulator > 0.5)
            {
                _freezeAlert.Visible = !_freezeAlert.Visible;
                _timeAccumulator = 0;
            }
        }
        else
        {
            _lastValue.LabelSettings.FontColor = new Color(0.0f, 1.0f, 0.4f, 1.0f);
            _freezeAlert.Visible = false;
        }
    }
}
