using Godot;
using System;

public partial class StatsLabel : Label
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var fps = Engine.GetFramesPerSecond();
		var playerPosition = GetNode<Camera3D>("../Camera3D").Position;
		var labelText = $"FPS: {fps}\nPlayer: ({playerPosition.X:0.00}, {playerPosition.Y:0.00}, {playerPosition.Z:0.00})";

		this.Text = labelText;
	}
}
