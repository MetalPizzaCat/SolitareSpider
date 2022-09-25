using Godot;
using System;

public class EmptyColumnButton : Node2D
{
	[Signal]
	public delegate void OnEmptyColumnSelected(int column);

	private int _columnId = -1;

	public int ColumnId
	{
		get => _columnId;
		set => _columnId = value;
	}
	private void _onButtonPressed()
	{
		EmitSignal(nameof(OnEmptyColumnSelected), _columnId);
	}
}



