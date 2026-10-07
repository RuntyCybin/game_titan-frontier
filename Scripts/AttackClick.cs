using Godot;

namespace Cybin;

public partial class AttackClick : Node3D
{
    [Export(PropertyHint.Range, "0.2,2,0.05")] public float Duration { get; set; } = .75f;
    private StandardMaterial3D _material = null!;
    private Tween? _animation;

    public override void _Ready()
    {
        _material = (StandardMaterial3D)GetNode<MeshInstance3D>("Ring").MaterialOverride.Duplicate();
        foreach (Node child in GetChildren())
            if (child is MeshInstance3D mesh) mesh.MaterialOverride = _material;
    }

    public void Play(float targetRadius)
    {
        // A repeated click restarts the same reticle instead of stacking bright rings.
        _animation?.Kill();
        float radius = targetRadius + .65f;
        Scale = new Vector3(radius * 1.45f, 1, radius * 1.45f);
        Rotation = Vector3.Zero;
        Color color = _material.AlbedoColor;
        color.A = .72f;
        _material.AlbedoColor = color;
        _animation = CreateTween();
        _animation.TweenProperty(this, "scale", new Vector3(radius, 1, radius), Duration * .3f)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _animation.TweenProperty(this, "scale", new Vector3(radius * 1.12f, 1, radius * 1.12f), Duration * .7f);
        _animation.Parallel().TweenProperty(_material, "albedo_color:a", 0f, Duration * .7f);
        _animation.TweenCallback(Callable.From(QueueFree));
    }
}
