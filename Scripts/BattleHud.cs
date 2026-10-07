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
        Button(buttons, "[Q] EXPLORADOR\n100 Ti · 4 s", "Rápido, ideal para asegurar depósitos.", () => Battle.Train(UnitKind.Scout));
        Button(buttons, "[E] BASTIÓN\n180 Ti · 7 s", "Blindado de primera línea.", () => Battle.Train(UnitKind.Tank));
        Button(buttons, "[T] TANQUE PEQUEÑO\n120 Ti · 5 s", "Blindado ligero y barato.", () => Battle.Train(UnitKind.SmallTank));
        Button(buttons, "[R] ARTILLERÍA\n260 Ti · 10 s", "Gran alcance y daño de área; frágil.", () => Battle.Train(UnitKind.Artillery));
        Button(buttons, "[B] EXTRACTOR\n250 Ti · +12/s", "Despliega sobre un depósito a menos de 18 m de tus fuerzas.", () => { if (!Battle.Ended && !Battle.Paused) { Battle.BuildMode = !Battle.BuildMode; Battle.AttackOrder = false; } });
        _queue = Text(production, "", 13, _muted);
        _status = Text(production, "", 12, new("ffcc83"));
        _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _status.CustomMinimumSize = new(490, 36);

        _modal = Panel(new(.5f, .5f), new(.5f, .5f), new(-250, -165, 250, 165));
        var modalColumn = new VBoxContainer(); modalColumn.AddThemeConstantOverride("separation", 18); _modal.AddChild(modalColumn);
        _modalTitle = Text(modalColumn, "PAUSA TÁCTICA", 24, Visuals.Cyan);
        _modalTitle.HorizontalAlignment = HorizontalAlignment.Center;
        _resume = Button(modalColumn, "CONTINUAR", "Esc", TogglePause);
        Button(modalColumn, "REINICIAR ESCARAMUZA", "Nueva partida", () => { GetTree().Paused = false; GetTree().ReloadCurrentScene(); });
        Button(modalColumn, "VOLVER AL MENÚ", "Elegir otro mapa", ReturnToMenu);
        _modal.Visible = false;
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
        _resources.Text = $"◈ {Battle.Credits:N0} Ti   +{Battle.Income}/s";
        _wave.Text = $"OLEADA {Battle.Wave + 1:00}   /   {Mathf.CeilToInt(Battle.NextWave):00}s";
        _selection.Text = Battle.Selection.Count == 1 ? Battle.Selection[0].DisplayName : $"{Battle.Selection.Count:00} UNIDADES SELECCIONADAS";
        _queue.Text = Battle.Production.Count == 0 ? "COLA VACÍA  /  Producción disponible" : $"EN PRODUCCIÓN  /  {Battle.Production.Peek()} · {Battle.ProductionRemaining:0.0}s · {Battle.Production.Count}/8 en cola";
        _status.Text = Battle.BuildMode ? "DESPLIEGUE: haz clic en un depósito. Esc cancela." : Battle.Status;
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
        Vector2 ToMap(Vector3 p) => mapRect.Position + new Vector2((p.X + 60) / 120 * mapRect.Size.X, (p.Z + 60) / 120 * mapRect.Size.Y);
        foreach (var deposit in Battle.Deposits) _map.DrawCircle(ToMap(deposit), 3, new("ffcc83"));
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
            var point = mouse.Position / _map.Size * 120 - Vector2.One * 60;
            if (mouse.ButtonIndex == MouseButton.Left) Battle.CenterCamera(new(point.X, 0, point.Y));
            if (mouse.ButtonIndex == MouseButton.Right) Battle.IssueOrder(new(point.X, 0, point.Y), false);
            _map.AcceptEvent();
        }
    }
}
