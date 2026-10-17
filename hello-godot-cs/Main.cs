using Godot;
using System.Runtime.InteropServices;
// Attach this script to the root "Main" Node2D.
public partial class Main : Node2D
{
[Export] public int WandererCount { get; set; } = 12;
[Export] public float WandererSpeed { get; set; } = 150.0f;
private Sprite2D[] _wanderers = System.Array.Empty<Sprite2D>();
private Vector2[] _velocities = System.Array.Empty<Vector2>();
public override void _Ready()
{
GD.Print("Hello, World from C#!");

// Prove which engine build and which .NET runtime are running.
var info = Engine.GetVersionInfo();
string godotVersion = info["string"].AsString();
string dotnetVersion = RuntimeInformation.FrameworkDescription;
GD.Print($"Godot: {godotVersion}");
GD.Print($"Runtime: {dotnetVersion}");
var label = GetNode<Label>("InfoLabel");
label.Text = "Hello, World from C#!\n"
+ $"Godot {godotVersion}\n"
+ $"{dotnetVersion}\n"
+ "Arrows: move | Space: recolor";
SpawnWanderers();
}
private void SpawnWanderers()
{
var texture = GD.Load<Texture2D>("res://icon.svg");
Vector2 size = GetViewportRect().Size;
_wanderers = new Sprite2D[WandererCount];
_velocities = new Vector2[WandererCount];
for (int i = 0; i < WandererCount; i++)
{
var sprite = new Sprite2D
{
Texture = texture,
Scale = new Vector2(0.3f, 0.3f),
Position = new Vector2(GD.Randf() * size.X, GD.Randf() * size.Y),
Modulate = Color.FromHsv(GD.Randf(), 0.5f, 1.0f),
ZIndex = -1, // draw behind the label and the player
};
AddChild(sprite);
_wanderers[i] = sprite;
_velocities[i] = Vector2.FromAngle(GD.Randf() * Mathf.Tau) * WandererSpeed;
}
GD.Print($"Spawned {WandererCount} wanderers.");
}
public override void _Process(double delta)
{
Vector2 size = GetViewportRect().Size;
for (int i = 0; i < _wanderers.Length; i++)
{
Vector2 pos = _wanderers[i].Position + _velocities[i] * (float)delta;
// Bounce off the window edges.
if (pos.X < 0 || pos.X > size.X) _velocities[i].X *= -1;
if (pos.Y < 0 || pos.Y > size.Y) _velocities[i].Y *= -1;

_wanderers[i].Position = pos.Clamp(Vector2.Zero, size);
_wanderers[i].Rotation = _velocities[i].Angle(); // face the direction of travel
}
}
}
