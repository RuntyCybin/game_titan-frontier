using Godot;

namespace Cybin;

public static class Visuals
{
	public static readonly Color Cyan = new("48e4d5");
	public static readonly Color Red = new("ff705e");
	public static StandardMaterial3D Material(Color color, bool glow = false) => new()
	{
		AlbedoColor = color, Roughness = 0.78f,
		EmissionEnabled = glow, Emission = color, EmissionEnergyMultiplier = glow ? 1.5f : 0
	};

	public static MeshInstance3D Box(Node3D parent, Vector3 size, Vector3 position, Color color, bool glow = false)
	{
		var mesh = new MeshInstance3D { Mesh = new BoxMesh { Size = size }, Position = position, MaterialOverride = Material(color, glow) };
		parent.AddChild(mesh);
		return mesh;
	}

	public static MeshInstance3D Cylinder(Node3D parent, float radius, float height, Vector3 position, Color color, int sides = 16)
	{
		var mesh = new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = height, RadialSegments = sides }, Position = position, MaterialOverride = Material(color) };
		parent.AddChild(mesh);
		return mesh;
	}

	public static MeshInstance3D RangeRing(Node3D parent, float radius, Color color)
	{
		var material = new StandardMaterial3D
		{
			AlbedoColor = new Color(color, 0.24f),
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled
		};
		var mesh = new MeshInstance3D
		{
			Name = "AttackRange",
			Mesh = new TorusMesh
			{
				InnerRadius = radius - 0.045f, OuterRadius = radius + 0.045f,
				Rings = 128, RingSegments = 6
			},
			Position = new(0, 0.10f, 0),
			Scale = new(1, 0.08f, 1),
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		};
		parent.AddChild(mesh);
		return mesh;
	}

	public static MeshInstance3D Ring(Node3D parent, float radius, Color color)
	{
		var mesh = new MeshInstance3D { Mesh = new TorusMesh { InnerRadius = radius - 0.055f, OuterRadius = radius + 0.055f, Rings = 32, RingSegments = 8 }, Position = new(0, 0.08f, 0), MaterialOverride = Material(color, true) };
		parent.AddChild(mesh);
		return mesh;
	}
}
