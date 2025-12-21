using Godot;
using System;

public partial class Camera3D : Godot.Camera3D
{
	private float _rotationX { get; set; }
	private float _rotationY { get; set; }
	private Vector3 _acceleration;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	public override void _Process(double delta)
	{
		var position = this.Position;
		var transform = this.Transform;

		transform.Basis = Basis.Identity;
		this.Transform = transform;

		this.RotateObjectLocal(Vector3.Up, _rotationX);
		this.RotateObjectLocal(Vector3.Right, _rotationY);

		if (Input.IsKeyPressed(Key.W))
		{
			this._acceleration.Z = -Constants.CameraMovementSpeed * (float)delta;
		}
		else if (Input.IsKeyPressed(Key.S)) 
		{
			this._acceleration.Z = Constants.CameraMovementSpeed * (float)delta;
		}

		if (Input.IsKeyPressed(Key.A))
		{
			this._acceleration.X = -Constants.CameraMovementSpeed * (float)delta;
		}
		else if (Input.IsKeyPressed(Key.D))
		{
			this._acceleration.X = Constants.CameraMovementSpeed * (float)delta;
		}

		this.TranslateObjectLocal(this._acceleration);		
		this._acceleration = this._acceleration.Lerp(Vector3.Zero, 0.1F);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseBtn)
		{
			if (mouseBtn.IsPressed() && !mouseBtn.IsEcho())
			{
				Input.MouseMode = Input.MouseModeEnum.ConfinedHidden;
			}

			return;
		}

		if (@event is InputEventMouseMotion mouseMotion) {
			_rotationX += -mouseMotion.Relative.X * Constants.CameraLookSpeed;
			_rotationY += -mouseMotion.Relative.Y * Constants.CameraLookSpeed;
			return;
		}

		if (@event is InputEventKey keyEvent && keyEvent.Keycode == Key.Space) {
			var lightNode = GetNode<DirectionalLight3D>("../DirectionalLight3D");
			lightNode.Position = this.Position;
			lightNode.Rotation = this.Rotation;
		}
	}
}
