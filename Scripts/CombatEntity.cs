using Godot;

namespace Cybin;

public enum UnitKind { Scout, Tank, Artillery, Headquarters, Extractor }

[Tool]
public partial class CombatEntity : Node3D
{
	public Battlefield Battle = null!;
	[Export] public UnitKind Kind { get; set; }
	[Export(PropertyHint.Enum, "Player,Enemy")] public int Team { get; set; }
	[ExportGroup("Combat")]
	[Export(PropertyHint.Range, "1,10000,1")] public float MaxHealth { get; set; } = 220;
	[Export(PropertyHint.Range, "0,30,0.1")] public float Speed { get; set; } = 5.5f;
	[Export(PropertyHint.Range, "0.1,60,0.1")] public float Range { get; set; } = 13;
	[Export(PropertyHint.Range, "0,1000,1")] public float Damage { get; set; } = 25;
	[Export(PropertyHint.Range, "0.05,10,0.05")] public float FireInterval { get; set; } = 1.1f;
	[Export(PropertyHint.Range, "0.1,10,0.05")] public float Radius { get; set; } = 1.25f;
	[ExportGroup("Editor Preview")]
	[Export] public bool PreviewAttackRange { get; set; }
	public float Health;
	public Vector3 Destination;
	public CombatEntity? Target;
	public bool Moving, Selected, AttackMove;
	public bool IsBuilding => Kind >= UnitKind.Headquarters;
	public bool Alive => Health > 0 && !IsQueuedForDeletion();
	public string DisplayName => Kind switch { UnitKind.Scout => "EXPLORADOR", UnitKind.Tank => "TANQUE BASTIÓN", UnitKind.Artillery => "ARTILLERÍA", UnitKind.Headquarters => "CENTRO DE MANDO", _ => "EXTRACTOR" };
	private MeshInstance3D _ring = null!;
	private MeshInstance3D? _rangeRing;
	private Node3D _body = null!;
	private float _cooldown;

	public override void _Ready()
	{
		_body = GetNode<Node3D>("Body");
		_ring = GetNode<MeshInstance3D>("SelectionRing");
		_rangeRing = GetNodeOrNull<MeshInstance3D>("AttackRange");
		// Each instance owns its indicator resources: changing one unit cannot resize another.
		_ring.Mesh = (Mesh)_ring.Mesh.Duplicate();
		_ring.MaterialOverride = (Material)_ring.MaterialOverride.Duplicate();
		if (_rangeRing != null)
		{
			_rangeRing.Mesh = (Mesh)_rangeRing.Mesh.Duplicate();
			_rangeRing.MaterialOverride = (Material)_rangeRing.MaterialOverride.Duplicate();
		}
		Health = MaxHealth;
		Destination = Position;
		if (!Engine.IsEditorHint() && GetParent() is Battlefield battle) Battle = battle;
		UpdatePreview();
	}

	private int _previewTeam = -1;
	private float _previewRange = -1, _previewRadius = -1;

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint()) UpdatePreview();
	}

	private void UpdatePreview()
	{
		if (_ring == null) return;
		if (_previewRange != Range && _rangeRing?.Mesh is TorusMesh rangeMesh)
		{
			rangeMesh.OuterRadius = Mathf.Max(.1f, Range) + .045f;
			rangeMesh.InnerRadius = Mathf.Max(.1f, Range) - .045f;
			_previewRange = Range;
		}
		if (_previewRadius != Radius && _ring.Mesh is TorusMesh selectionMesh)
		{
			selectionMesh.OuterRadius = Radius + .405f;
			selectionMesh.InnerRadius = Radius + .295f;
			_previewRadius = Radius;
		}
		_ring.Visible = !Engine.IsEditorHint() && Selected;
		if (_rangeRing != null) _rangeRing.Visible = !IsBuilding && (Engine.IsEditorHint() ? PreviewAttackRange : Selected);
		if (_previewTeam == Team) return;
		_previewTeam = Team;
		Color accent = Team == 0 ? Visuals.Cyan : Visuals.Red;
		Color armor = Team == 0 ? new Color("344b58") : new Color("65443f");
		foreach (Node child in _body.GetChildren())
		{
			if (child is not MeshInstance3D mesh || !mesh.HasMeta("team_role") || mesh.MaterialOverride is not StandardMaterial3D original) continue;
			var material = (StandardMaterial3D)original.Duplicate();
			string role = mesh.GetMeta("team_role").AsString();
			material.AlbedoColor = role switch { "accent" => accent, "turret" => armor.Lightened(.15f), "upper" => armor.Lightened(.12f), _ => armor };
			if (material.EmissionEnabled) material.Emission = material.AlbedoColor;
			mesh.MaterialOverride = material;
		}
		if (_ring.MaterialOverride is StandardMaterial3D ringMaterial)
		{
			ringMaterial.AlbedoColor = accent; ringMaterial.Emission = accent;
		}
		if (_rangeRing?.MaterialOverride is StandardMaterial3D rangeMaterial)
			rangeMaterial.AlbedoColor = new Color(accent, rangeMaterial.AlbedoColor.A);
	}
	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint() || Battle == null || !Alive || Battle.Ended) return;
		float dt = (float)delta;
		_ring.Visible = Selected;
		if (_rangeRing != null) _rangeRing.Visible = Selected;
		_cooldown -= dt;
		if (Target != null && !Target.Alive) Target = null;
		if (Target == null && (!Moving || AttackMove || IsBuilding)) Target = Battle.NearestEnemy(this, Range);
		if (Target != null)
		{
			var difference = Target.Position - Position;
			if (difference.Length() <= Range + Target.Radius)
			{
				Face(Target.Position, dt);
				if (_cooldown <= 0 && Damage > 0)
				{
					_cooldown = FireInterval;
					Battle.Shoot(this, Target);
				}
				return;
			}
			if (!IsBuilding) MoveTowards(Target.Position, dt);
		}
		else if (Moving)
		{
			if (Position.DistanceTo(Destination) < .5f) Moving = false;
			else MoveTowards(Destination, dt);
		}
	}

	private void Face(Vector3 point, float dt)
	{
		if (IsBuilding) return;
		var dir = point - Position;
		_body.Rotation = new(0, Mathf.LerpAngle(_body.Rotation.Y, Mathf.Atan2(-dir.X, -dir.Z), dt * 8), 0);
	}

	private void MoveTowards(Vector3 point, float dt)
	{
		Vector3 direction = (point - Position).Normalized();
		Vector3 separation = Vector3.Zero;
		foreach (var other in Battle.Entities)
		{
			if (other == this || !other.Alive) continue;
			Vector3 away = Position - other.Position;
			float distance = away.Length();
			float gap = Radius + other.Radius + .25f;
			if (distance < gap && distance > .01f) separation += away / distance * (gap - distance) * 2;
		}
		var movement = direction + separation;
		if (movement.Length() > 1) movement = movement.Normalized();
		Position += movement * Speed * dt;
		Position = new(Mathf.Clamp(Position.X, -57, 57), 0, Mathf.Clamp(Position.Z, -57, 57));
		Face(point, dt);
	}

	public void TakeDamage(float amount)
	{
		if (!Alive) return;
		Health = Mathf.Max(0, Health - amount);
		if (Health <= 0) Battle.DestroyEntity(this);
	}
}
