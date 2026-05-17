# Estrutura da Cena Principal (Main.tscn)

- **Main** (Node3D)
  - **DirectionalLight3D** (Luz do Sol fixa)
  - **Camera3D** (Câmera do observador, posicionada atrás/diagonal do avião)
  - **GridPlane** (MeshInstance3D) - Usa um PlaneMesh e o shader de grid com fade.
  - **SerialReader** (Node) - Script: `SerialReader.cs`. Configurar "COM5" e "115200" no Inspector.
  
  - **Vehicle** (Node3D) - Raiz do veículo para visualização. Script: `AttitudeController.cs`
    - **JetFighter** (Node3D) - Arraste o modelo `Models/source/JetFighter.glb` para cá.
    - **BodyAxes** (Node3D) - Criado dinamicamente via `AttitudeController.cs` (Cylinders para eixos X, Y, Z).

  - **HUD** (CanvasLayer)
    - **HeadingTape** (Control) - Script: `HeadingTape.cs`. Ancorado no Topo, 48px de altura, full-width.
    - **PitchTape** (Control) - Script: `PitchTape.cs`. Ancorado à Esquerda, 56px de largura, altura central.
    - **HUDController** (Control) - Script: `HUDController.cs`. StatusPanel (220x220px) ancorado no Canto Superior Direito.
    - **ArtificialHorizon** (Control) - Script: `ArtificialHorizon.cs`. Canto inferior esquerdo (160x160px), offset (16, -176). Com tooltip_text.
    - **CompassRose** (Control) - Script: `CompassRose.cs`. Centro inferior (140x140px), offset (-70, -156). Com tooltip_text.
    - **GMeter** (Control) - Script: `GMeter.cs`. Canto inferior direito (120x120px), margem 16px.
