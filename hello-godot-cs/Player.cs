using Godot;
 
// The leader: glides toward the mouse cursor.
public partial class Player : Sprite2D
{
	[Export] public float Speed { get; set; } = 600.0f; // px/s
 
	public override void _Ready()
	{
		Scale = new Vector2(0.5f, 0.5f); // smaller icon: room for the flock
	}
 
	public override void _Process(double delta)
	{
		// Keep the target inside the window even if the mouse leaves it.
		Vector2 screen = GetViewportRect().Size;
   
	 Vector2 mouse = GetGlobalMousePosition().Clamp(Vector2.Zero, screen);
		float step = Speed * (float)delta;
		GlobalPosition = GlobalPosition.MoveToward(mouse, step);
	}
}
