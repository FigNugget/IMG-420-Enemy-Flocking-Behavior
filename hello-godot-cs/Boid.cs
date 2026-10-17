using Godot;

public partial class Boid : Node2D
{
	public Vector2 DebugSeparation;
	public Vector2 DebugAlignment;
	public Vector2 DebugCohesion;
	public Vector2 DebugFollow;
	public Vector2 Velocity;
	public Color BodyColor = Colors.White;
 
	// Arrowhead pointing along +X (forward at Rotation 0).
	private static readonly Vector2[] Shape =
	{
		new Vector2(10, 0), new Vector2(-6, -6),
		new Vector2(-2, 0), new Vector2(-6, 6),
	};
 
	public override void _Draw()
	{
		DrawColoredPolygon(Shape, BodyColor);
	}
	
	// Reynolds' simple vehicle model (forward Euler):
	// velocity += accel * dt, capped at maxSpeed;
	// position += velocity * dt.
	public void Integrate(Vector2 accel, float maxSpeed, float dt)
	{
		Velocity = (Velocity + accel * dt).LimitLength(maxSpeed);
		Position += Velocity * dt;
	 
		// Face the way we move (skip when nearly stopped).
		if (Velocity.LengthSquared() > 100f)
		{
			float facing = Velocity.Angle();
			Rotation = Mathf.LerpAngle(Rotation, facing, 10f * dt);
		}
	}
}
