using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cybin;

public partial class Battlefield : Node3D
{
	[Export] public string MapTitle { get; set; } = "FRONTERA DE TITANIO";
	[Export] public string ObjectiveText { get; set; } = "Destruye el mando enemigo al noreste.";
	public readonly List<CombatEntity> Entities = new();
	public readonly List<CombatEntity> Selection = new();
	public readonly List<Vector3> Deposits = new();
	public readonly Queue<UnitKind> Production = new();
	public Camera3D Camera = null!;
	public CombatEntity PlayerBase = null!, EnemyBase = null!;
	public BattleHud Hud = null!;
	public const int DepositTotal = 100000, BaseIncome = 20, ExtractorRate = 60, ExtractorCost = 1000, HeadquartersCost = 5000;
	public int Credits = 4000, Wave;
	public float[] DepositReserve = System.Array.Empty<float>();
	public float NextWave = 55, ProductionRemaining;
	public bool Ended, Won, BuildMode, AttackOrder;
	// Truck currently placing its finished building (BuildMode is the placement cursor).
	public CombatEntity? PlaceTruck;
	public string Status = "Selecciona tus vehículos y asegura los depósitos de titanio.";
	public Vector2 DragStart, DragEnd;
	public bool Dragging;
	public bool Paused => GetTree().Paused;
	private Node3D _cameraRig = null!;
	private bool _rotating;
	private float _zoom = 50, _incomeTime;
	private readonly RandomNumberGenerator _rng = new();
	private readonly Dictionary<int, List<CombatEntity>> _groups = new();
	private MeshInstance3D _buildPreview = null!;
	private bool _smokeTest;
	private int _smokeFrames;
	private int _previewFrames;
	private PackedScene? _pixelImpactScene;

	public override void _Ready()
	{
		_rng.Seed = 26092026;
		_cameraRig = GetNode<Node3D>("CameraRig");
		Camera = _cameraRig.GetNode<Camera3D>("Camera3D");
		_zoom = Camera.Size;
		foreach (var entity in GetChildren().OfType<CombatEntity>())
		{
			entity.Battle = this;
			Entities.Add(entity);
		}
		PlayerBase = Entities.Single(e => e.Kind == UnitKind.Headquarters && e.Team == 0);
		EnemyBase = Entities.Single(e => e.Kind == UnitKind.Headquarters && e.Team == 1);
		foreach (var deposit in GetNode<Node3D>("Map/Deposits").GetChildren().OfType<Node3D>())
			Deposits.Add(ToLocal(deposit.GlobalPosition));
		DepositReserve = Deposits.Select(_ => (float)DepositTotal).ToArray();
		_buildPreview = Visuals.Ring(this, 2.8f, Visuals.Cyan); _buildPreview.Visible = false;
		var canvas = new CanvasLayer(); AddChild(canvas);
		Hud = new BattleHud { Battle = this }; canvas.AddChild(Hud);
		_smokeTest = OS.GetCmdlineUserArgs().Contains("--smoke-test");
		if (_smokeTest) RunSmokeChecks();
	}

	[Export] public float MapSize { get; set; } = 120;

	[ExportGroup("Production Scenes")]
	[Export] public PackedScene HumveeScene { get; set; } = null!;
	[Export] public PackedScene AbramsScene { get; set; } = null!;
	[Export] public PackedScene PumaScene { get; set; } = null!;
	[Export] public PackedScene TruckScene { get; set; } = null!;
	[Export] public PackedScene LeopardScene { get; set; } = null!;
	[Export] public PackedScene ArtilleryScene { get; set; } = null!;
	[Export] public PackedScene HeadquartersScene { get; set; } = null!;
	[Export] public PackedScene ExtractorScene { get; set; } = null!;

	public CombatEntity Spawn(UnitKind kind, int team, Vector3 position)
	{
		var scene = kind switch
		{
			UnitKind.Humvee => HumveeScene, UnitKind.Abrams => AbramsScene,
			UnitKind.Artillery => ArtilleryScene, UnitKind.Puma => PumaScene, UnitKind.Leopard => LeopardScene, UnitKind.Truck => TruckScene, UnitKind.Headquarters => HeadquartersScene,
			_ => ExtractorScene
		};
		var entity = scene.Instantiate<CombatEntity>();
		entity.Battle = this; entity.Team = team; entity.Position = position;
		Entities.Add(entity); AddChild(entity);
		return entity;
	}
	public override void _Process(double delta)
	{
		if (OS.GetCmdlineUserArgs().Contains("--capture-preview") && ++_previewFrames == 60)
		{
			GetViewport().GetTexture().GetImage().SavePng("res://preview.png");
			GetTree().Quit(); return;
		}
		if (_smokeTest && ++_smokeFrames == 180)
		{
			bool moved = Entities.Any(e => e.Team == 0 && !e.IsBuilding && e.Position.X > -20);
			bool produced = Production.Count == 0 && Entities.Count(e => e.Team == 0 && e.Kind == UnitKind.Humvee) == 2;
			bool earned = Credits > 4000 - Cost(UnitKind.Humvee);
			Hud.TogglePause(); bool paused = Paused; Hud.TogglePause();
			PlayerBase.TakeDamage(99999);
			bool defeat = Ended && !Won;
			bool passed = moved && produced && earned && paused && !Paused && defeat;
			GD.Print(passed ? "SMOKE PASS: scene, economy, production, movement, combat, pause, victory, defeat." : $"SMOKE FAIL: moved={moved}, produced={produced}, earned={earned}, paused={paused}, defeat={defeat}");
			GetTree().Quit(passed ? 0 : 1); return;
		}
		if (Ended) return;
		float dt = (float)delta;
		Vector3 pan = Vector3.Zero;
		if (Input.IsPhysicalKeyPressed(Key.W) || Input.IsPhysicalKeyPressed(Key.Up)) pan.Z -= 1;
		if (Input.IsPhysicalKeyPressed(Key.S) || Input.IsPhysicalKeyPressed(Key.Down)) pan.Z += 1;
		if (Input.IsPhysicalKeyPressed(Key.A) || Input.IsPhysicalKeyPressed(Key.Left)) pan.X -= 1;
		if (Input.IsPhysicalKeyPressed(Key.D) || Input.IsPhysicalKeyPressed(Key.Right)) pan.X += 1;
		_cameraRig.Position += _cameraRig.Basis * pan.Normalized() * dt * _zoom * .65f;
		float limit = MapSize / 2 - 7;
		_cameraRig.Position = new(Mathf.Clamp(_cameraRig.Position.X, -limit, limit), 0, Mathf.Clamp(_cameraRig.Position.Z, -limit, limit));
		_incomeTime += dt;
		if (_incomeTime >= 1)
		{
			_incomeTime -= 1;
			Credits += Mine();
		}
		if (Production.Count > 0)
		{
			ProductionRemaining -= dt;
			if (ProductionRemaining <= 0)
			{
				var kind = Production.Dequeue();
				// Vehicles roll out through the headquarters door (front face, -Z).
				var unit = Spawn(kind, 0, PlayerBase.Position + PlayerBase.Basis * new Vector3(0, 0, -2.4f));
				unit.Destination = PlayerBase.Position + PlayerBase.Basis * new Vector3(_rng.RandfRange(-8, 8), 0, -13);
				OpenDoor(PlayerBase);
				unit.Moving = true;
				ProductionRemaining = Production.Count > 0 ? BuildTime(Production.Peek()) : 0;
				Status = unit.DisplayName + " listo para recibir órdenes.";
			}
		}
		NextWave -= dt;
		if (NextWave <= 0) LaunchWave();
		TickConstruction(dt);
		if (BuildMode && (PlaceTruck == null || !IsInstanceValid(PlaceTruck) || !PlaceTruck.Alive)) { BuildMode = false; PlaceTruck = null; }
		_buildPreview.Visible = BuildMode;
		if (BuildMode)
		{
			var ground = GroundPoint(GetViewport().GetMousePosition());
			bool extractor = PlaceTruck?.BuildKind == UnitKind.Extractor;
			var spot = extractor ? Deposits.OrderBy(p => p.DistanceSquaredTo(ground)).First() : new Vector3(ground.X, 0, ground.Z);
			_buildPreview.Scale = extractor ? Vector3.One : Vector3.One * 1.9f;
			_buildPreview.Position = spot + new Vector3(0, .2f, 0);
		}
	}

	public int DepositIndex(Vector3 position) => Deposits.Select((p, i) => (p, i)).OrderBy(x => x.p.DistanceSquaredTo(position)).First().i;
	public int TotalReserve => Mathf.RoundToInt(DepositReserve.Sum());
	private bool Mining(CombatEntity e) => e.Alive && e.Kind == UnitKind.Extractor && DepositReserve[DepositIndex(e.Position)] > 0;
	// Income per second for the player: base income plus every extractor whose deposit still has coins.
	public int Income => BaseIncome + Entities.Count(e => e.Team == 0 && Mining(e)) * ExtractorRate;
	// Extractors of both teams drain their deposit; only the player's yield is credited.
	private int Mine()
	{
		int gained = BaseIncome;
		foreach (var extractor in Entities.Where(Mining).ToList())
		{
			int i = DepositIndex(extractor.Position);
			float taken = Mathf.Min(ExtractorRate, DepositReserve[i]);
			DepositReserve[i] -= taken;
			if (extractor.Team == 0) gained += Mathf.RoundToInt(taken);
		}
		return gained;
	}
	public static int Cost(UnitKind kind) => kind switch { UnitKind.Humvee => 300, UnitKind.Abrams => 1000, UnitKind.Puma => 600, UnitKind.Leopard => 1500, UnitKind.Artillery => 2000, UnitKind.Truck => 800, UnitKind.Headquarters => HeadquartersCost, _ => ExtractorCost };
	public static float BuildTime(UnitKind kind) => kind switch { UnitKind.Humvee => 4, UnitKind.Abrams => 7, UnitKind.Puma => 5, UnitKind.Leopard => 8, UnitKind.Truck => 6, UnitKind.Headquarters => 20, UnitKind.Extractor => 8, _ => 10 };

	public void Train(UnitKind kind)
	{
		if (Ended || Paused) return;
		if (Production.Count >= 8) { Status = "Cola completa: máximo 8 vehículos."; return; }
		if (Entities.Count(e => e.Team == 0 && !e.IsBuilding) + Production.Count >= 60) { Status = "Límite de ejército: 60 vehículos."; return; }
		if (Credits < Cost(kind)) { Status = "Monedas insuficientes."; return; }
		Credits -= Cost(kind);
		if (Production.Count == 0) ProductionRemaining = BuildTime(kind);
		Production.Enqueue(kind); Status = "Fabricación iniciada.";
	}

	private readonly Dictionary<CombatEntity, Tween> _doorTweens = new();

	// Rolls the headquarters door up, holds it while the vehicle leaves, then closes it.
	private void OpenDoor(CombatEntity headquarters)
	{
		if (headquarters.GetNodeOrNull<Node3D>("Body/DoorPivot") is not { } door) return;
		if (_doorTweens.TryGetValue(headquarters, out var running) && running.IsValid()) running.Kill();
		var tween = headquarters.CreateTween();
		tween.TweenProperty(door, "scale:y", .08f, .3);
		tween.TweenInterval(1.6);
		tween.TweenProperty(door, "scale:y", 1f, .5);
		_doorTweens[headquarters] = tween;
	}

	private void TickConstruction(float dt)
	{
		foreach (var truck in Entities.Where(e => e.Team == 0 && e.Kind == UnitKind.Truck && e.BuildKind != null && !e.BuildReady).ToList())
		{
			truck.BuildRemaining -= dt;
			if (truck.BuildRemaining <= 0)
			{
				truck.BuildRemaining = 0; truck.BuildReady = true;
				Status = $"{BuildingName(truck.BuildKind!.Value)} listo: pulsa READY para colocarlo.";
			}
		}
	}

	public static string BuildingName(UnitKind kind) => kind == UnitKind.Headquarters ? "CENTRO DE MANDO" : "EXTRACTOR";

	// Starts a building on an idle truck: pays up front, then counts down like a unit in production.
	public bool StartConstruction(CombatEntity truck, UnitKind kind)
	{
		if (Ended || Paused || truck.Kind != UnitKind.Truck || !truck.Alive) return false;
		if (kind != UnitKind.Extractor && kind != UnitKind.Headquarters) return false;
		if (truck.BuildKind != null) { Status = "Este Truck ya tiene una obra en curso."; return false; }
		if (Credits < Cost(kind)) { Status = "Monedas insuficientes."; return false; }
		Credits -= Cost(kind);
		truck.BuildKind = kind; truck.BuildRemaining = BuildTime(kind); truck.BuildReady = false;
		Status = $"Construcción de {BuildingName(kind)} iniciada.";
		return true;
	}

	// Enters the placement cursor for a finished building (the "Ready" button).
	public bool BeginPlacement(CombatEntity truck)
	{
		if (Ended || Paused || !truck.Alive || truck.BuildKind == null || !truck.BuildReady) return false;
		PlaceTruck = truck; BuildMode = true; AttackOrder = false;
		return true;
	}

	public bool PlaceBuilding(Vector3 position)
	{
		if (Ended || Paused || PlaceTruck == null || !IsInstanceValid(PlaceTruck) || !PlaceTruck.Alive || PlaceTruck.BuildKind is not UnitKind kind || !PlaceTruck.BuildReady) return false;
		Vector3 spot = position;
		if (kind == UnitKind.Extractor)
		{
			spot = Deposits.OrderBy(p => p.DistanceSquaredTo(position)).First();
			if (spot.DistanceTo(position) > 5) { Status = "Coloca el extractor sobre un depósito luminoso."; return false; }
			if (Entities.Any(e => e.Alive && e.IsBuilding && e.Position.DistanceTo(spot) < 5)) { Status = "Este depósito ya está ocupado."; return false; }
			if (DepositReserve[DepositIndex(spot)] <= 0) { Status = "Este depósito está agotado."; return false; }
			if (PlaceTruck.Position.DistanceTo(spot) > 18) { Status = "Acerca el Truck a menos de 18 m del depósito."; return false; }
		}
		else
		{
			float limit = MapSize / 2 - 8;
			spot = new(position.X, 0, position.Z);
			if (Mathf.Abs(spot.X) > limit || Mathf.Abs(spot.Z) > limit) { Status = "Demasiado cerca del borde del mapa."; return false; }
			if (PlaceTruck.Position.DistanceTo(spot) > 30) { Status = "Acerca el Truck a menos de 30 m del lugar."; return false; }
			if (Entities.Any(e => e.Alive && e.IsBuilding && e.Position.DistanceTo(spot) < 11)) { Status = "Hay otro edificio demasiado cerca."; return false; }
			if (Deposits.Any(d => d.DistanceTo(spot) < 7)) { Status = "No se puede construir sobre un depósito."; return false; }
		}
		Spawn(kind, 0, spot);
		PlaceTruck.BuildKind = null; PlaceTruck.BuildReady = false; PlaceTruck.BuildRemaining = 0;
		PlaceTruck = null; BuildMode = false;
		Status = kind == UnitKind.Extractor ? $"Extractor operativo. +{ExtractorRate} monedas por segundo." : "Nuevo centro de mando operativo.";
		return true;
	}

	private void LaunchWave()
	{
		Wave++; NextWave = Mathf.Max(30, 65 - Wave * 3);
		for (int i = 0; i < Math.Min(3 + Wave, 12); i++)
		{
			var unit = Spawn(i % 4 == 3 ? UnitKind.Artillery : i % 3 == 0 ? UnitKind.Humvee : UnitKind.Abrams, 1, EnemyBase.Position + new Vector3(-8 + i % 4 * 4, 0, 9 + i / 4 * 4));
			unit.Destination = PlayerBase.Position; unit.Moving = true; unit.AttackMove = true;
		}
		Status = $"ALERTA · Oleada {Wave} en marcha desde el mando enemigo.";
	}

	public CombatEntity? NearestEnemy(CombatEntity source, float range) => Entities
		.Where(e => e.Alive && e.Team != source.Team && e.Position.DistanceTo(source.Position) <= range + e.Radius)
		.OrderBy(e => e.Position.DistanceSquaredTo(source.Position)).FirstOrDefault();

	public void Shoot(CombatEntity source, CombatEntity target)
	{
		Vector3 start = source.Position + new Vector3(0, source.IsBuilding ? 3 : 1.5f, 0);
		Vector3 impactSide = (source.Position - target.Position).Normalized();
		Vector3 end = target.Position + new Vector3(0, target.IsBuilding ? 1.8f : 1.1f, 0) + impactSide * target.Radius;
		var trace = Visuals.Box(this, new(source.Kind == UnitKind.Artillery ? .16f : .07f, .07f, start.DistanceTo(end)), (start + end) / 2, source.Team == 0 ? Visuals.Cyan : Visuals.Red, true);
		if (start.DistanceTo(end) > .01f) trace.LookAt(end);
		var tween = CreateTween(); tween.TweenInterval(.09); tween.TweenCallback(Callable.From(trace.QueueFree));
		_pixelImpactScene ??= GD.Load<PackedScene>("res://Scenes/Effects/PixelImpact.tscn");
		var impact = _pixelImpactScene.Instantiate<PixelImpact>();
		impact.Position = end;
		if (source.Kind == UnitKind.Artillery)
		{
			impact.Amount = 20;
			impact.InitialVelocityMax = 6;
			impact.ScaleAmountMin = .85f;
			impact.ScaleAmountMax = 1.7f;
		}
		// Keep the burst in the battlefield so a lethal hit cannot delete it with its target.
		AddChild(impact);
		if (source.Kind == UnitKind.Artillery)
		{
			foreach (var victim in Entities.ToArray())
				if (victim.Alive && victim.Team != source.Team && victim.Position.DistanceTo(target.Position) <= 3.5f) victim.TakeDamage(source.Damage);
		}
		else target.TakeDamage(source.Damage);
	}

	private PackedScene? _pixelDestructionScene;
	public void DestroyEntity(CombatEntity entity)
	{
		_pixelDestructionScene ??= GD.Load<PackedScene>("res://Scenes/Effects/PixelDestruction.tscn");
		var explosion = _pixelDestructionScene.Instantiate<PixelImpact>();
		explosion.Position = entity.Position + Vector3.Up * (entity.IsBuilding ? 1.8f : 1.0f);
		if (entity.IsBuilding)
		{
			explosion.Amount = 72;
			explosion.Lifetime = 1.1;
			explosion.InitialVelocityMin *= 1.4f;
			explosion.InitialVelocityMax *= 1.4f;
			explosion.ScaleAmountMin *= 1.4f;
			explosion.ScaleAmountMax *= 1.4f;
		}
		// Lives independently of the destroyed unit and cleans itself up when finished.
		AddChild(explosion);
		Selection.Remove(entity); Entities.Remove(entity); _doorTweens.Remove(entity);
		if (entity.Team == 1 && !entity.IsBuilding)
		{
			int bounty = Cost(entity.Kind) / 10;
			Credits += bounty; Status = $"{entity.DisplayName} enemigo destruido · +{bounty} monedas.";
		}
		if (entity.Kind == UnitKind.Headquarters && Entities.FirstOrDefault(e => e.Kind == UnitKind.Headquarters && e.Team == entity.Team && e.Alive) is { } survivor)
		{
			if (entity.Team == 0) PlayerBase = survivor; else EnemyBase = survivor;
			Status = "Centro de mando perdido, pero queda otro en pie.";
		}
		else if (entity.Kind == UnitKind.Headquarters)
		{
			Ended = true; Won = entity.Team == 1;
			Status = Won ? "VICTORIA · Mando enemigo destruido" : "DERROTA · Hemos perdido el centro de mando";
			BuildMode = false; _buildPreview.Visible = false;
		}
		if (PlaceTruck == entity) { PlaceTruck = null; BuildMode = false; }
		entity.QueueFree();
	}

	public Vector3 GroundPoint(Vector2 screen)
	{
		var origin = Camera.ProjectRayOrigin(screen); var direction = Camera.ProjectRayNormal(screen);
		if (Mathf.Abs(direction.Y) < .001f) return Vector3.Zero;
		return origin + direction * (-origin.Y / direction.Y);
	}

	private CombatEntity? HeadquartersAt(Vector2 screen)
	{
		var point = GroundPoint(screen);
		return Entities.FirstOrDefault(e => e.Team == 0 && e.Kind == UnitKind.Headquarters && e.Alive && e.Position.DistanceTo(point) < e.Radius + 1);
	}

	// The main headquarters is where new vehicles roll out (double right-click on one to choose it).
	public void SetMainBase(CombatEntity headquarters)
	{
		if (headquarters.Team != 0 || headquarters.Kind != UnitKind.Headquarters || !headquarters.Alive) return;
		PlayerBase = headquarters;
		Status = "Centro de mando principal cambiado: los vehículos saldrán de aquí.";
	}

	public void CenterCamera(Vector3 position) => _cameraRig.Position = new(position.X, 0, position.Z);

	public override void _Input(InputEvent input)
	{
		if (input is InputEventKey key && key.Pressed && !key.Echo)
		{
			if (key.Keycode == Key.Escape)
			{
				if (BuildMode || AttackOrder) { BuildMode = false; AttackOrder = false; }
				else Hud.TogglePause();
				GetViewport().SetInputAsHandled();
			}
		}
		// Hold the middle button and drag horizontally to orbit the camera 360 degrees.
		if (input is InputEventMouseButton middle && middle.ButtonIndex == MouseButton.Middle)
			_rotating = middle.Pressed && !Ended && !Paused;
		if (input is InputEventMouseMotion orbit && _rotating)
		{
			if (Ended || Paused) _rotating = false;
			else _cameraRig.RotateY(-orbit.Relative.X * .006f);
		}
		// Release a selection even when the pointer ends over a HUD panel.
		if (input is InputEventMouseButton release && !release.Pressed && release.ButtonIndex == MouseButton.Left && Dragging)
		{
			DragEnd = release.Position; FinishSelection(release.ShiftPressed); Dragging = false;
		}
	}

	public override void _UnhandledInput(InputEvent input)
	{
		if (Ended || Paused) return;
		if (input is InputEventMouseMotion motion && Dragging) DragEnd = motion.Position;
		if (input is InputEventMouseButton mouse && mouse.Pressed)
		{
			if (mouse.ButtonIndex == MouseButton.WheelUp || mouse.ButtonIndex == MouseButton.WheelDown)
			{ _zoom = Mathf.Clamp(_zoom + (mouse.ButtonIndex == MouseButton.WheelUp ? -4 : 4), 26, 90); Camera.Size = _zoom; }
			if (mouse.ButtonIndex == MouseButton.Left)
			{
				if (BuildMode) PlaceBuilding(GroundPoint(mouse.Position));
				else if (AttackOrder) { IssueOrder(GroundPoint(mouse.Position), true); AttackOrder = false; }
				else { DragStart = DragEnd = mouse.Position; Dragging = true; }
			}
			if (mouse.ButtonIndex == MouseButton.Right)
			{
				if (BuildMode || AttackOrder) { BuildMode = false; AttackOrder = false; }
				else if (mouse.DoubleClick && HeadquartersAt(mouse.Position) is { } headquarters) SetMainBase(headquarters);
				else IssueOrder(GroundPoint(mouse.Position), false);
			}
		}
		if (input is InputEventKey key && key.Pressed && !key.Echo)
		{
			switch (key.Keycode)
			{
				case Key.Q: Train(UnitKind.Humvee); break;
				case Key.E: Train(UnitKind.Abrams); break;
				case Key.T: Train(UnitKind.Puma); break;
				case Key.L: Train(UnitKind.Leopard); break;
				case Key.R: Train(UnitKind.Artillery); break;
				case Key.V: Train(UnitKind.Truck); break;
				case Key.F: AttackOrder = true; BuildMode = false; Status = "Haz clic en el destino para avanzar atacando."; break;
				case Key.X: foreach (var unit in Selection) { unit.Moving = false; unit.Target = null; } break;
				case Key.Space: CenterCamera(PlayerBase.Position); break;
				case Key.Tab: SetSelection(Entities.Where(e => e.Team == 0 && !e.IsBuilding)); break;
			}
			if (key.Keycode >= Key.Key1 && key.Keycode <= Key.Key5)
			{
				int group = (int)key.Keycode - (int)Key.Key1;
				if (key.CtrlPressed) _groups[group] = Selection.ToList();
				else if (_groups.TryGetValue(group, out var units)) SetSelection(units.Where(e => IsInstanceValid(e) && e.Alive));
			}
		}
	}

	private void FinishSelection(bool additive)
	{
		var rect = new Rect2(DragStart, DragEnd - DragStart).Abs();
		IEnumerable<CombatEntity> chosen;
		if (rect.Size.Length() < 8)
		{
			var point = GroundPoint(DragEnd);
			chosen = Entities.Where(e => e.Team == 0 && e.Position.DistanceTo(point) < e.Radius + 1).OrderBy(e => e.Position.DistanceSquaredTo(point)).Take(1);
		}
		else chosen = Entities.Where(e => e.Team == 0 && !e.IsBuilding && !Camera.IsPositionBehind(e.Position) && rect.HasPoint(Camera.UnprojectPosition(e.Position)));
		SetSelection(additive ? Selection.Concat(chosen).Distinct().ToArray() : chosen.ToArray());
	}

	public void SetSelection(IEnumerable<CombatEntity> entities)
	{
		var list = entities.ToList();
		foreach (var unit in Selection) unit.Selected = false;
		Selection.Clear();
		foreach (var unit in list) { unit.Selected = true; Selection.Add(unit); }
	}

	public void IssueOrder(Vector3 position, bool attackMove)
	{
		if (Ended || Paused) return;
		var units = Selection.Where(e => e.Alive && e.Team == 0 && !e.IsBuilding).ToArray();
		if (units.Length == 0) return;
		var target = Entities.Where(e => e.Alive && e.Team == 1 && e.Position.DistanceTo(position) < e.Radius + 1)
			.OrderBy(e => e.Position.DistanceSquaredTo(position)).FirstOrDefault();
		int columns = Mathf.CeilToInt(Mathf.Sqrt(units.Length));
		// Formation slots wide enough for the largest vehicle, so units do not fight over the same spot.
		float spacing = Mathf.Max(3.2f, units.Max(e => e.Radius) * 2 + .6f), limit = MapSize / 2 - 3;
		for (int i = 0; i < units.Length; i++)
		{
			units[i].Target = target;
			units[i].Destination = new(Mathf.Clamp(position.X + (i % columns - (columns - 1) / 2f) * spacing, -limit, limit), 0, Mathf.Clamp(position.Z + (i / columns) * spacing, -limit, limit));
			units[i].Moving = true; units[i].AttackMove = attackMove;
		}
		if (units.Length > 0) Status = target != null ? "Objetivo fijado." : attackMove ? "Avanzando con ataque automático." : "Desplazamiento en formación.";
		if (target != null)
		{
			var click = target.GetNodeOrNull<AttackClick>("AttackClick");
			if (click == null || click.IsQueuedForDeletion())
			{
				if (click != null) click.Name = "ExpiredAttackClick";
				click = GD.Load<PackedScene>("res://Scenes/Effects/AttackClick.tscn").Instantiate<AttackClick>();
				target.AddChild(click);
			}
			click.Play(target.Radius);
			return;
		}
		var marker = Visuals.Ring(this, 1, attackMove ? Visuals.Red : Visuals.Cyan); marker.Position = position + new Vector3(0, .12f, 0);
		var tween = CreateTween(); tween.TweenProperty(marker, "scale", Vector3.One * .1f, .6); tween.TweenCallback(Callable.From(marker.QueueFree));
	}

	private void RunSmokeChecks()
	{
		try
		{
			if (Entities.Count != 10 || Income != BaseIncome + ExtractorRate) throw new Exception("Initial economy/entities");
			var editedTank = AbramsScene.Instantiate<CombatEntity>();
			editedTank.MaxHealth = 333;
			editedTank.Range = 17;
			editedTank.Team = 1;
			AddChild(editedTank);
			var editedRing = (TorusMesh)editedTank.GetNode<MeshInstance3D>("AttackRange").Mesh;
			var originalTank = Entities.First(e => e.Kind == UnitKind.Abrams && e.Team == 0);
			var originalRing = (TorusMesh)originalTank.GetNode<MeshInstance3D>("AttackRange").Mesh;
			if (editedTank.Health != 333 || !Mathf.IsEqualApprox(editedRing.OuterRadius, 17.045f)
				|| !Mathf.IsEqualApprox(originalRing.OuterRadius, originalTank.Range + .045f)
				|| editedRing == originalRing) throw new Exception("Scene overrides / independent range meshes");
			var originalColor = ((StandardMaterial3D)originalTank.GetNode<MeshInstance3D>("Body/LeftTrim").MaterialOverride).AlbedoColor;
			var enemyColor = ((StandardMaterial3D)editedTank.GetNode<MeshInstance3D>("Body/LeftTrim").MaterialOverride).AlbedoColor;
			if (originalColor != Visuals.Cyan || enemyColor != Visuals.Red) throw new Exception("Independent team materials");
			editedTank.Free();
			Train(UnitKind.Humvee);
			if (Credits != 4000 - Cost(UnitKind.Humvee) || Production.Count != 1) throw new Exception("Production payment");
			ProductionRemaining = .01f;
			var truck = Spawn(UnitKind.Truck, 0, Deposits[1] + new Vector3(4, 0, 0));
			int beforeBuild = Credits;
			if (!StartConstruction(truck, UnitKind.Extractor) || Credits != beforeBuild - ExtractorCost || truck.BuildReady) throw new Exception("Truck construction payment");
			if (StartConstruction(truck, UnitKind.Headquarters)) throw new Exception("Truck busy");
			if (BeginPlacement(truck)) throw new Exception("Placement before ready");
			truck.BuildRemaining = .01f; _incomeTime = 0;
			TickConstruction(.05f);
			if (!truck.BuildReady) throw new Exception("Truck countdown");
			if (!BeginPlacement(truck) || !BuildMode) throw new Exception("Placement mode");
			if (PlaceBuilding(Deposits[1] + new Vector3(0, 0, 12))) throw new Exception("Extractor off deposit");
			if (!PlaceBuilding(Deposits[1]) || Income != BaseIncome + 2 * ExtractorRate) throw new Exception("Extractor construction");
			int before = Credits;
			truck.BuildKind = UnitKind.Extractor; truck.BuildReady = true;
			if (BeginPlacement(truck) && PlaceBuilding(Deposits[1]) || Credits != before) throw new Exception("Duplicate extractor");
			truck.BuildKind = null; truck.BuildReady = false; BuildMode = false; PlaceTruck = null;
			Entities.Remove(truck); truck.QueueFree();
			var friendly = Entities.First(e => e.Team == 0 && e.Kind == UnitKind.Abrams);
			var enemy = Spawn(UnitKind.Humvee, 1, friendly.Position + new Vector3(3, 0, 0));
			IssueOrder(enemy.Position, false);
			if (enemy.HasNode("AttackClick")) throw new Exception("Attack marker without selection");
			SetSelection(new[] { friendly });
			IssueOrder(enemy.Position, false);
			var click = enemy.GetNodeOrNull<AttackClick>("AttackClick");
			if (click == null || friendly.Target != enemy) throw new Exception("Single-unit attack feedback");
			SetSelection(Entities.Where(e => e.Team == 0 && !e.IsBuilding));
			IssueOrder(enemy.Position, false);
			if (enemy.GetNodeOrNull<AttackClick>("AttackClick") != click || Selection.Any(e => e.Target != enemy))
				throw new Exception("Group attack / repeated click feedback");
			Shoot(friendly, enemy);
			if (enemy.Health >= enemy.MaxHealth) throw new Exception("Combat damage");
			int creditsBeforeKill = Credits;
			enemy.TakeDamage(999);
			if (Entities.Contains(enemy)) throw new Exception("Death cleanup");
			if (Credits != creditsBeforeKill + Cost(UnitKind.Humvee) / 10) throw new Exception("Kill bounty");
			SetSelection(Entities.Where(e => e.Team == 0 && !e.IsBuilding));
			IssueOrder(new(-10, 0, 25), false);
			LaunchWave();
			if (Wave != 1) throw new Exception("Enemy waves");
			EnemyBase.TakeDamage(99999);
			if (!Ended || !Status.Contains("VICTORIA")) throw new Exception("Victory");
			Ended = false; NextWave = 999;
			GD.Print("SMOKE: initial assertions passed");
		}
		catch (Exception error) { GD.PushError("SMOKE FAIL: " + error); GetTree().Quit(1); }
	}
}
