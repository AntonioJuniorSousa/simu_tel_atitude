using Godot;
using System.Collections.Generic;

public partial class AttitudeController : Node3D
{
    [Export] public SerialReader reader;
    [Export] public bool showBodyAxes = true;

    // Valores de referência para suavização:
    // 0.05f -> suavização forte, modelo mais "fluido", ligeira latência visível
    // 0.15f -> suavização moderada, elimina trepidação sem latência perceptível (recomendado)
    // 0.30f -> suavização leve, quase direto
    // 1.0f  -> sem suavização (comportamento atual)
    [Export] public float SmoothingFactor = 0.15f;

    private Quaternion _smoothedQuaternion = Quaternion.Identity;

    private Node3D axesNode;

    public override void _Ready()
    {
        CreateAxes();
    }

    private void CreateAxes()
    {
        axesNode = new Node3D();
        axesNode.Visible = showBodyAxes;
        AddChild(axesNode);

        // X Axis - Red
        CreateArrow(axesNode, new Color(1, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0, Mathf.Pi / 2));
        // Y Axis - Green
        CreateArrow(axesNode, new Color(0, 1, 0), new Vector3(0, 0.5f, 0), Vector3.Zero);
        // Z Axis - Blue
        CreateArrow(axesNode, new Color(0, 0, 1), new Vector3(0, 0, 0.5f), new Vector3(-Mathf.Pi / 2, 0, 0));
    }

    private void CreateArrow(Node3D parent, Color color, Vector3 pos, Vector3 rotRads)
    {
        Node3D arrowRoot = new Node3D();
        arrowRoot.Position = pos;
        arrowRoot.Rotation = rotRads;
        parent.AddChild(arrowRoot);

        // Create Material
        StandardMaterial3D material = new StandardMaterial3D();
        material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        material.AlbedoColor = color;

        // Shaft (Cylinder)
        MeshInstance3D shaft = new MeshInstance3D();
        CylinderMesh shaftMesh = new CylinderMesh();
        shaftMesh.TopRadius = 0.025f;
        shaftMesh.BottomRadius = 0.025f;
        shaftMesh.Height = 1.0f;
        shaft.Mesh = shaftMesh;
        shaft.MaterialOverride = material;
        arrowRoot.AddChild(shaft);

        // Tip (Cone via CylinderMesh)
        MeshInstance3D tip = new MeshInstance3D();
        CylinderMesh tipMesh = new CylinderMesh();
        tipMesh.TopRadius = 0.0f;
        tipMesh.BottomRadius = 0.07f;
        tipMesh.Height = 0.2f;
        tip.Mesh = tipMesh;
        tip.MaterialOverride = material;
        // Move tip up to the end of the shaft
        tip.Position = new Vector3(0, 0.6f, 0);
        arrowRoot.AddChild(tip);
    }

    private Quaternion ConvertImuToGodot(Quaternion qImu)
    {
        return new Quaternion(qImu.X, qImu.Z, -qImu.Y, qImu.W).Normalized();
    }

    public override void _Process(double delta)
    {
        if (reader == null) return;

        if (axesNode != null)
        {
            axesNode.Visible = showBodyAxes;
        }

        Quaternion qImu = reader.LatestQuaternion;
        Quaternion qGodot = ConvertImuToGodot(qImu);
        
        _smoothedQuaternion = _smoothedQuaternion.Slerp(qGodot, SmoothingFactor);
        this.Quaternion = _smoothedQuaternion;
    }

    // Método para ser chamado pelo HUD para obter Euler RPY
    public Vector3 GetEulerAnglesDeg()
    {
        Vector3 eulerRads = this.Quaternion.GetEuler();
        return new Vector3(
            Mathf.RadToDeg(eulerRads.X),  // Pitch
            Mathf.RadToDeg(eulerRads.Y),  // Yaw
            -Mathf.RadToDeg(eulerRads.Z)   // Roll (negado para convenção aeronáutica)
        );
    }
}
