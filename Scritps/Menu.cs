using Godot;
using System;

public class Menu : Control
{
	private AnimationPlayer _player;
	public override void _Ready()
	{
		base._Ready();
		_player = GetNode<AnimationPlayer>("AnimationPlayer");
	}
	private void _onOpenPressed()
	{
		_player.Play("Open");
	}


	private void _onClosePressed()
	{
		_player.PlayBackwards("Open");
	}

	private void _onExitPressed()
	{
		GetTree().Quit();
	}


	private void _onRestartPressed()
	{
		GetTree().ReloadCurrentScene();
	}
}



