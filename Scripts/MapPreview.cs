using Godot;
using System.Collections.Generic;

namespace Cybin;

[Tool]
public partial class MapPreview : Control
{
    [Export] public PackedScene BattleScene { get; set; } = null!;
    [Export] public Color TerrainColor { get; set; } = new("243e44");
    private readonly List<Vector3> _deposits = new();
    private Vector3 _player, _enemy;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        // Read the actual scene placements, without starting a battle or its scripts.
        if (BattleScene != null)
        {
            var scene = BattleScene.Instantiate<Node3D>();
            _player = scene.GetNode<Node3D>("PlayerHeadquarters").Position;
            _enemy = scene.GetNode<Node3D>("EnemyHeadquarters").Position;
            foreach (Node3D deposit in scene.GetNode<Node3D>("Map/Deposits").GetChildren())
                _deposits.Add(deposit.Position);
            scene.Free();
        }
        Resized += QueueRedraw;
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), TerrainColor);
        Vector2 Point(Vector3 p) => new((p.X + 60) / 120 * Size.X, (p.Z + 60) / 120 * Size.Y);
        for (int i = 1; i < 8; i++)
        {
            var grid = new Color(1, 1, 1, .055f);
            DrawLine(new(Size.X * i / 8, 0), new(Size.X * i / 8, Size.Y), grid);
            DrawLine(new(0, Size.Y * i / 8), new(Size.X, Size.Y * i / 8), grid);
        }
        DrawLine(Point(_player), Point(_enemy), new Color(1, 1, 1, .12f), 18, true);
        foreach (var deposit in _deposits)
        {
            DrawCircle(Point(deposit), 10, new Color(1, .76f, .38f, .09f));
            DrawCircle(Point(deposit), 3.5f, new("ffcc83"));
        }
        foreach (var (position, color) in new[] { (_player, Visuals.Cyan), (_enemy, Visuals.Red) })
        {
            Vector2 p = Point(position);
            DrawCircle(p, 24, new Color(color, .10f));
            DrawArc(p, 20, 0, Mathf.Tau, 48, new Color(color, .5f), 1, true);
            DrawRect(new Rect2(p - Vector2.One * 6, Vector2.One * 12), color);
        }
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(1, 1, 1, .1f), false);
    }
}
