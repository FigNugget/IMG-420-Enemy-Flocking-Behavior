using Godot;
using System.Collections.Generic;

public partial class Flock : Node2D
{
	private readonly List<Boid> _boids = new();
	private Vector2[] _steering = System.Array.Empty<Vector2>();

	[Export] public int BoidCount { get; set; } = 60;
	[Export] public float MaxSpeed { get; set; } = 220f;
	[Export] public float MaxForce { get; set; } = 500f;

	[ExportGroup("Flocking")]
	[Export] public bool UseSeparation { get; set; } = true;
	[Export] public bool UseAlignment { get; set; } = true;
	[Export] public bool UseCohesion { get; set; } = true;
	[Export] public float SeparationRadius { get; set; } = 28f;
	[Export] public float NeighborRadius { get; set; } = 90f;
	[Export] public float SeparationWeight { get; set; } = 1.5f;
	[Export] public float AlignmentWeight { get; set; } = 1f;
	[Export] public float CohesionWeight { get; set; } = 1f;

	[ExportGroup("Follow & Gather")]
	[Export] public bool UseFollow { get; set; } = false;
	[Export] public Node2D Target { get; set; }
	[Export] public float FollowWeight { get; set; } = 1f;
	[Export] public float GatherRadius { get; set; } = 85f;
	[Export] public float SlowingDistance { get; set; } = 160f;

	[ExportGroup("Debug")]
	[Export] public bool DebugDraw { get; set; } = false;

	public override void _Ready()
	{
		ZIndex = -1;
		SpawnBoids();
	}

	private void SpawnBoids()
	{
		Vector2 screen = GetViewportRect().Size;

		for (int i = 0; i < BoidCount; i++)
		{
			float angle = GD.Randf() * Mathf.Tau;
			float hue = 0.5f + 0.1f * GD.Randf();

			var boid = new Boid
			{
				Position = new Vector2(GD.Randf(), GD.Randf()) * screen,
				Velocity = Vector2.FromAngle(angle) * MaxSpeed * 0.5f,
				BodyColor = Color.FromHsv(hue, 0.55f, 1f),
			};

			boid.Rotation = boid.Velocity.Angle();
			AddChild(boid);
			_boids.Add(boid);
		}

		_steering = new Vector2[_boids.Count];
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		Vector2 screen = GetViewportRect().Size;

		// Calculate every boid's steering from the same flock snapshot.
		for (int i = 0; i < _boids.Count; i++)
		{
			Boid boid = _boids[i];

			Vector2 sep = UseSeparation
				? Separation(boid) * SeparationWeight
				: Vector2.Zero;

			Vector2 ali = UseAlignment
				? Alignment(boid) * AlignmentWeight
				: Vector2.Zero;

			Vector2 coh = UseCohesion
				? Cohesion(boid) * CohesionWeight
				: Vector2.Zero;

			bool following = UseFollow && Target != null && IsInstanceValid(Target);
			Vector2 fol = Vector2.Zero;

			if (following)
			{
				Vector2 target = ToLocal(Target.GlobalPosition);
				float distance = boid.Position.DistanceTo(target);
				float fade = Mathf.Clamp(
					(distance - GatherRadius) / SlowingDistance,
					0f,
					1f
				);

				ali *= fade;
				coh *= fade;
				fol = Follow(boid, target) * FollowWeight;
			}

			boid.DebugSeparation = sep;
			boid.DebugAlignment = ali;
			boid.DebugCohesion = coh;
			boid.DebugFollow = fol;

			_steering[i] = (sep + ali + coh + fol).LimitLength(MaxForce);
		}

		// Apply the calculated steering after all boids have been evaluated.
		for (int i = 0; i < _boids.Count; i++)
		{
			Boid boid = _boids[i];
			boid.Integrate(_steering[i], MaxSpeed, dt);
			boid.Position = Wrap(boid.Position, screen);
		}

		if (DebugDraw)
			QueueRedraw();
	}

	private static Vector2 Wrap(Vector2 position, Vector2 size) =>
		new(
			Mathf.PosMod(position.X, size.X),
			Mathf.PosMod(position.Y, size.Y)
		);

	private Vector2 SteerTowards(Boid boid, Vector2 direction)
	{
		if (direction.IsZeroApprox())
			return Vector2.Zero;

		Vector2 desired = direction.Normalized() * MaxSpeed;
		return (desired - boid.Velocity).LimitLength(MaxForce);
	}

	private Vector2 Separation(Boid boid)
	{
		Vector2 push = Vector2.Zero;

		foreach (Boid other in _boids)
		{
			if (other == boid)
				continue;

			Vector2 away = boid.Position - other.Position;
			float distanceSquared = away.LengthSquared();

			if (distanceSquared > 0f &&
				distanceSquared < SeparationRadius * SeparationRadius)
			{
				push += away / distanceSquared;
			}
		}

		return SteerTowards(boid, push);
	}

	private Vector2 Alignment(Boid boid)
	{
		Vector2 headingSum = Vector2.Zero;

		foreach (Boid other in _boids)
		{
			if (other == boid)
				continue;

			float distanceSquared = boid.Position.DistanceSquaredTo(other.Position);

			if (distanceSquared < NeighborRadius * NeighborRadius)
				headingSum += other.Velocity;
		}

		return SteerTowards(boid, headingSum);
	}

	private Vector2 Cohesion(Boid boid)
	{
		Vector2 center = Vector2.Zero;
		int count = 0;

		foreach (Boid other in _boids)
		{
			if (other == boid)
				continue;

			float distanceSquared = boid.Position.DistanceSquaredTo(other.Position);

			if (distanceSquared < NeighborRadius * NeighborRadius)
			{
				center += other.Position;
				count++;
			}
		}

		if (count == 0)
			return Vector2.Zero;

		center /= count;
		return SteerTowards(boid, center - boid.Position);
	}

	private Vector2 Follow(Boid boid, Vector2 target)
	{
		Vector2 toTarget = target - boid.Position;
		float distance = toTarget.Length();

		if (distance < 0.01f)
			return Vector2.Zero;

		float gap = distance - GatherRadius;
		float ramp = gap > 0f ? SlowingDistance : GatherRadius * 0.5f;
		float speed = MaxSpeed * Mathf.Clamp(gap / ramp, -1f, 1f);
		Vector2 desired = toTarget / distance * speed;

		return (desired - boid.Velocity).LimitLength(MaxForce);
	}

	public override void _Draw()
	{
		if (!DebugDraw)
			return;

		const float scale = 0.04f;
		var neighborRing = new Color(1f, 1f, 1f, 0.08f);
		var personalRing = new Color(1f, 0.4f, 0.4f, 0.35f);

		foreach (Boid boid in _boids)
		{
			Vector2 position = boid.Position;
			DrawArc(position, NeighborRadius, 0f, Mathf.Tau, 32, neighborRing);
			DrawArc(position, SeparationRadius, 0f, Mathf.Tau, 16, personalRing);
			DrawLine(position, position + boid.DebugSeparation * scale, Colors.Red, 2f);
			DrawLine(position, position + boid.DebugAlignment * scale, Colors.LimeGreen, 2f);
			DrawLine(position, position + boid.DebugCohesion * scale, Colors.DeepSkyBlue, 2f);
			DrawLine(position, position + boid.DebugFollow * scale, Colors.Gold, 2f);
		}

		if (UseFollow && Target != null && IsInstanceValid(Target))
		{
			Vector2 target = ToLocal(Target.GlobalPosition);
			float outer = GatherRadius + SlowingDistance;
			var faintGold = new Color(1f, 0.84f, 0f, 0.3f);

			DrawArc(target, GatherRadius, 0f, Mathf.Tau, 64, Colors.Gold, 2f);
			DrawArc(target, outer, 0f, Mathf.Tau, 64, faintGold, 1f);
		}
	}
}
