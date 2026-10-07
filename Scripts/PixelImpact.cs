using Godot;

namespace Cybin;

public partial class PixelImpact : CpuParticles3D
{
    public override void _Ready()
    {
        Finished += QueueFree;
        Restart();
        Emitting = true;
    }
}
