using Godot;
using System.Linq;

namespace Cybin;

public partial class BattleHud : Control
{
    public Battlefield Battle = null!;
    private Label _resources = null!, _wave = null!, _selection = null!, _status = null!, _queue = null!;
    private PanelContainer _modal = null!;
    private Label _modalTitle = null!;
    private Button _resume = null!;
    private Control _map = null!;
    private PanelContainer _build = null!;
    private Label _buildHint = null!;
    private readonly UnitKind[] _buildKinds = { UnitKind.Extractor, UnitKind.Headquarters };
    private readonly Label[] _ready = new Label[2];
    private readonly Button[] _buildButtons = new Button[2];
    private readonly Color _muted = new("93a7b3");

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); MouseFilter = MouseFilterEnum.Ignore;
        var top = Panel(new(0, 0), new(1, 0), new(18, 18, -18, 86));
        var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 32); top.AddChild(row);
        var brand = new VBoxContainer(); row.AddChild(brand);
        Text(brand, "C Y B I N  / /  R T S", 22, Visuals.Cyan);
        Text(brand, Battle.MapTitle + "     /     ESCARAMUZA", 11, _muted);
        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill }; row.AddChild(spacer);
        _resources = Text(row, "", 20, Visuals.Cyan);
        _wave = Text(row, "", 16, new("ffcc83"));
        var pause = Button(row, "II  PAUSA", "Esc", TogglePause); pause.CustomMinimumSize = new(110, 36);

        var objective = Panel(new(0, 0), new(0, 0), new(18, 101, 368, 170));
        var objectives = new VBoxContainer(); objective.AddChild(objectives);
        Text(objectives, "01  /  ROMPER EL FRENTE", 15, Colors.White);
        Text(objectives, Battle.ObjectiveText, 13, _muted);

        var bottom = Panel(new(0, 1), new(1, 1), new(18, -192, -18, -18));
        var columns = new HBoxContainer(); columns.AddThemeConstantOverride("separation", 22); bottom.AddChild(columns);
        _map = new Control { CustomMinimumSize = new(148, 148), MouseFilter = MouseFilterEnum.Stop };
        columns.AddChild(_map);
        _map.GuiInput += MapInput;
        _map.Draw += DrawMap;
        var info = new VBoxContainer { CustomMinimumSize = new(230, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill }; columns.AddChild(info);
        Text(info, "CONTROL TÁCTICO", 12, Visuals.Cyan);
        _selection = Text(info, "", 17, Colors.White);
        Text(info, "Arrastrar: seleccionar · Mayús: añadir\nClic derecho: mover / atacar\nF: avanzar atacando · X: detener\nWASD: cámara · Rueda: zoom\nTab: ejército · Espacio: base\nCtrl + 1–5: guardar grupo · 1–5: recuperar", 12, _muted);
        var production = new VBoxContainer { CustomMinimumSize = new(510, 0) }; columns.AddChild(production);
        Text(production, "FABRICACIÓN / DESPLIEGUE", 12, Visuals.Cyan);
        var buttons = new HBoxContainer(); production.AddChild(buttons);
        Button(buttons, "[Q] HUMVEE\n300 · 4 s", "Vehículo ligero con ametralladora; rápido y barato.", () => Battle.Train(UnitKind.Humvee));
        Button(buttons, "[E] ABRAMS\n1000 · 7 s", "Blindado de primera línea.", () => Battle.Train(UnitKind.Abrams));
        Button(buttons, "[T] PUMA\n600 · 5 s", "Vehículo de combate de infantería: ágil y económico.", () => Battle.Train(UnitKind.Puma));
        Button(buttons, "[L] LEOPARD\n1500 · 8 s", "Carro de combate pesado con cañón de gran calibre.", () => Battle.Train(UnitKind.Leopard));
        Button(buttons, "[R] ARTILLERÍA\n2000 · 10 s", "Gran alcance y daño de área; frágil.", () => Battle.Train(UnitKind.Artillery));
        Button(buttons, "[V] TRUCK\n800 · 6 s", "Constructor: selecciónalo para construir Extractores y Centros de mando.", () => Battle.Train(UnitKind.Truck));
        _queue = Text(production, "", 13, _muted);
        _status = Text(production, "", 12, new("ffcc83"));
        _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _status.CustomMinimumSize = new(490, 36);

        _build = Panel(new(1, 1), new(1, 1), new(-398, -346, -18, -202));
        var buildColumn = new VBoxContainer(); _build.AddChild(buildColumn);
        _buildHint = Text(buildColumn, "CONSTRUCCIÓN / TRUCK", 12, Visuals.Cyan);
        var slots = new HBoxContainer(); slots.AddThemeConstantOverride("separation", 12); buildColumn.AddChild(slots);
        for (int i = 0; i < 2; i++)
        {
            int slot = i;
            var column = new VBoxContainer(); slots.AddChild(column);
            _ready[i] = Text(column, "READY", 15, new("7dff9a")); _ready[i].HorizontalAlignment = HorizontalAlignment.Center;
            _buildButtons[i] = Button(column, "", "Pulsa para construir; cuando termine, pulsa READY y colócalo en el mapa.", () => BuildSlot(slot));
            _buildButtons[i].CustomMinimumSize = new(172, 62);
        }
        _build.Visible = false;

        _modal = Panel(new(.5f, .5f), new(.5f, .5f), new(-250, -165, 250, 165));
        var modalColumn = new VBoxContainer(); modalColumn.AddThemeConstantOverride("separation", 18); _modal.AddChild(modalColumn);
        _modalTitle = Text(modalColumn, "PAUSA TÁCTICA", 24, Visuals.Cyan);
        _modalTitle.HorizontalAlignment = HorizontalAlignment.Center;
        _resume = Button(modalColumn, "CONTINUAR", "Esc", TogglePause);
        Button(modalColumn, "REINICIAR ESCARAMUZA", "Nueva partida", () => { GetTree().Paused = false; GetTree().ReloadCurrentScene(); });
        Button(modalColumn, "VOLVER AL MENÚ", "Elegir otro mapa", ReturnToMenu);
        _modal.Visible = false;
    }

    private CombatEntity? SelectedTruck() => Battle.Selection.FirstOrDefault(e => e.Kind == UnitKind.Truck && e.Alive);

    private void BuildSlot(int slot)
    {
        var truck = SelectedTruck();
        if (truck == null || Battle.Ended || Battle.Paused) return;
        var kind = _buildKinds[slot];
        if (truck.BuildKind == null) Battle.StartConstruction(truck, kind);
        else if (truck.BuildKind == kind && truck.BuildReady) Battle.BeginPlacement(truck);
    }

    public void ReturnToMenu()
    {
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://Scenes/UI/MainMenu.tscn");
    }

    private PanelContainer Panel(Vector2 anchorStart, Vector2 anchorEnd, Vector4 offsets)
    {
        var panel = new PanelContainer { AnchorLeft = anchorStart.X, AnchorTop = anchorStart.Y, AnchorRight = anchorEnd.X, AnchorBottom = anchorEnd.Y, OffsetLeft = offsets.X, OffsetTop = offsets.Y, OffsetRight = offsets.Z, OffsetBottom = offsets.W };
        panel.AddThemeStyleboxOverride("panel", Style(new("101d29"), new("344952"), 14)); AddChild(panel); return panel;
    }

    private static StyleBoxFlat Style(Color background, Color border, int padding) => new()
    {
        BgColor = background, BorderColor = border, BorderWidthBottom = 1, BorderWidthTop = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
        ContentMarginLeft = padding, ContentMarginRight = padding, ContentMarginTop = padding, ContentMarginBottom = padding,
        CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5
    };

    private static Label Text(Node parent, string value, int size, Color color)
    {
        var label = new Label { Text = value, MouseFilter = MouseFilterEnum.Ignore, VerticalAlignment = VerticalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", color); parent.AddChild(label); return label;
    }

    private static Button Button(Node parent, string caption, string tooltip, System.Action pressed)
    {
        var button = new Button { Text = caption, TooltipText = tooltip, FocusMode = FocusModeEnum.None, CustomMinimumSize = new(120, 58), MouseDefaultCursorShape = CursorShape.PointingHand };
        button.AddThemeFontSizeOverride("font_size", 12);
        button.AddThemeColorOverride("font_color", new Color("dcf4f2"));
        button.AddThemeStyleboxOverride("normal", Style(new("20333e"), new("395964"), 10));
        button.AddThemeStyleboxOverride("hover", Style(new("2b5059"), Visuals.Cyan, 10));
        button.AddThemeStyleboxOverride("pressed", Style(new("33666a"), Visuals.Cyan, 10));
        button.AddThemeStyleboxOverride("disabled", Style(new("182229"), new("2a3a42"), 10));
        button.AddThemeColorOverride("font_disabled_color", new Color("5f737c"));
        button.Pressed += pressed; parent.AddChild(button); return button;
    }

    public void TogglePause()
    {
        if (Battle.Ended) return;
        GetTree().Paused = !GetTree().Paused;
        Battle.Dragging = false;
    }

    public override void _Process(double delta)
    {
        _resources.Text = $"◈ {Battle.Credits:N0} monedas   +{Battle.Income}/s   ·   Reservas {Battle.TotalReserve:N0}";
        _wave.Text = $"OLEADA {Battle.Wave + 1:00}   /   {Mathf.CeilToInt(Battle.NextWave):00}s";
        _selection.Text = Battle.Selection.Count == 1 ? Battle.Selection[0].DisplayName : $"{Battle.Selection.Count:00} UNIDADES SELECCIONADAS";
        _queue.Text = Battle.Production.Count == 0 ? "COLA VACÍA  /  Producción disponible" : $"EN PRODUCCIÓN  /  {Battle.Production.Peek()} · {Battle.ProductionRemaining:0.0}s · {Battle.Production.Count}/8 en cola";
        _status.Text = Battle.BuildMode ? (Battle.PlaceTruck?.BuildKind == UnitKind.Extractor ? "COLOCAR EXTRACTOR: haz clic en un depósito. Esc cancela." : "COLOCAR CENTRO DE MANDO: haz clic en el terreno. Esc cancela.") : Battle.Status;
        var truck = SelectedTruck();
        _build.Visible = truck != null && !Battle.Ended;
        if (truck != null)
            for (int i = 0; i < 2; i++)
            {
                var kind = _buildKinds[i];
                bool mine = truck.BuildKind == kind, ready = mine && truck.BuildReady;
                string name = Battlefield.BuildingName(kind);
                _ready[i].Modulate = new Color(1, 1, 1, ready ? 1 : 0);
                _buildButtons[i].Disabled = truck.BuildKind != null && !mine;
                _buildButtons[i].Text = ready ? $"{name}\nCOLOCAR" : mine ? $"{name}\nEN OBRAS · {truck.BuildRemaining:0.0} s"
                    : $"{name}\n{Battlefield.Cost(kind)} · {Battlefield.BuildTime(kind):0} s";
            }
        _modal.Visible = Battle.Paused || Battle.Ended;
        _modalTitle.Text = Battle.Ended ? (Battle.Won ? "SECTOR ASEGURADO · VICTORIA" : "DERROTA") : "PAUSA TÁCTICA";
        _resume.Visible = !Battle.Ended;
        QueueRedraw();
        _map.QueueRedraw();
    }

    public override void _Input(InputEvent input)
    {
        if (Battle.Paused && input is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape)
        {
            TogglePause(); GetViewport().SetInputAsHandled();
        }
    }

    public override void _Draw()
    {
        if (Battle.Camera == null || _map == null) return;
        foreach (var entity in Battle.Entities)
        {
            if (Battle.Camera.IsPositionBehind(entity.Position)) continue;
            if (!entity.Selected && entity.Health >= entity.MaxHealth) continue;
            Vector2 point = Battle.Camera.UnprojectPosition(entity.Position + new Vector3(0, entity.IsBuilding ? 6 : 2.5f, 0));
            DrawRect(new(point - new Vector2(19, 3), new(38, 5)), new("0a151d"));
            DrawRect(new(point - new Vector2(18, 2), new(36 * entity.Health / entity.MaxHealth, 3)), entity.Team == 0 ? Visuals.Cyan : Visuals.Red);
        }
        if (Battle.Entities.Count(e => e.Team == 0 && e.Kind == UnitKind.Headquarters) > 1 && !Battle.Camera.IsPositionBehind(Battle.PlayerBase.Position))
        {
            Vector2 main = Battle.Camera.UnprojectPosition(Battle.PlayerBase.Position + new Vector3(0, 7.5f, 0));
            var font = GetThemeDefaultFont();
            var size = font.GetStringSize("★ PRINCIPAL", HorizontalAlignment.Left, -1, 13);
            DrawRect(new(main - new Vector2(size.X / 2 + 6, size.Y), size + new Vector2(12, 6)), new Color("0a151dcc"));
            DrawString(font, main - new Vector2(size.X / 2, 4), "★ PRINCIPAL", HorizontalAlignment.Left, -1, 13, Visuals.Cyan);
        }
        if (Battle.Dragging)
        {
            var rect = new Rect2(Battle.DragStart, Battle.DragEnd - Battle.DragStart).Abs();
            DrawRect(rect, new Color(.28f, .9f, .83f, .10f)); DrawRect(rect, Visuals.Cyan, false, 1);
        }
    }

    private void DrawMap()
    {
        Rect2 mapRect = new(Vector2.Zero, _map.Size);
        _map.DrawRect(mapRect, new("0a161d"));
        for (int i = 1; i < 4; i++)
        {
            _map.DrawLine(mapRect.Position + new Vector2(mapRect.Size.X * i / 4, 0), mapRect.Position + new Vector2(mapRect.Size.X * i / 4, mapRect.Size.Y), new("25373c"));
            _map.DrawLine(mapRect.Position + new Vector2(0, mapRect.Size.Y * i / 4), mapRect.Position + new Vector2(mapRect.Size.X, mapRect.Size.Y * i / 4), new("25373c"));
        }
        Vector2 ToMap(Vector3 p) => mapRect.Position + new Vector2((p.X + Battle.MapSize / 2) / Battle.MapSize * mapRect.Size.X, (p.Z + Battle.MapSize / 2) / Battle.MapSize * mapRect.Size.Y);
        foreach (var deposit in Battle.Deposits) _map.DrawCircle(ToMap(deposit), 3, Battle.DepositReserve[Battle.DepositIndex(deposit)] > 0 ? new("ffcc83") : new("4a4a4a"));
        foreach (var entity in Battle.Entities) _map.DrawCircle(ToMap(entity.Position), entity.IsBuilding ? 4 : 2, entity.Team == 0 ? Visuals.Cyan : Visuals.Red);
        var viewport = GetViewportRect().Size;
        var corners = new[] { new Vector2(0, 86), new Vector2(viewport.X, 86), new Vector2(viewport.X, viewport.Y - 192), new Vector2(0, viewport.Y - 192) };
        for (int i = 0; i < 4; i++)
        {
            Vector2 a = ToMap(Battle.GroundPoint(corners[i])).Clamp(mapRect.Position, mapRect.End);
            Vector2 b = ToMap(Battle.GroundPoint(corners[(i + 1) % 4])).Clamp(mapRect.Position, mapRect.End);
            _map.DrawLine(a, b, new Color(1, 1, 1, .6f));
        }
        _map.DrawRect(mapRect, new("47616a"), false);
    }

    private void MapInput(InputEvent input)
    {
        if (Battle.Paused || Battle.Ended) return;
        if (input is InputEventMouseButton mouse && mouse.Pressed)
        {
            var point = mouse.Position / _map.Size * Battle.MapSize - Vector2.One * (Battle.MapSize / 2);
            if (mouse.ButtonIndex == MouseButton.Left) Battle.CenterCamera(new(point.X, 0, point.Y));
            if (mouse.ButtonIndex == MouseButton.Right) Battle.IssueOrder(new(point.X, 0, point.Y), false);
            _map.AcceptEvent();
        }
    }
}
