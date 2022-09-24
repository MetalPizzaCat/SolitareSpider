using Godot;
using System;

public enum CardType
{
	Number,
	Atlas,
	Queen,
	Jester,
	King
}

public enum CardSuit
{
	Diamond,
	Club,
	Heart,
	Spade
}

public class Card : Node2D
{
	[Signal]
	public delegate void CardPressed(int cardId);

	private CardInfo _info;
	public CardInfo Info => _info;

	[Export]
	private bool _revealed = false;

	private bool _followsMouse = false;

	private int _id = -1;
	public int ColumnId = -1;

	public int Id => _id;

	public bool Revealed
	{
		get => _revealed;
		set
		{
			_revealed = value;
			if (value)
			{
				_cardSprite.RegionRect = new Rect2(new Vector2(_revealed ? 64 : 0, 0), _cardSprite.RegionRect.Size);
				if (_revealed)
				{
					_debugCardNameLabel.Text = _info.CardType == CardType.Number ? _info.CardNumericalValue.ToString() : _info.CardType.ToString();
				}
			}
		}
	}

	private Label _debugCardNameLabel;
	private Sprite _cardSprite;

	public void Init(CardInfo info, int id, int column)
	{
		_info = info;
		_id = id;
		ColumnId = column;
	}
	public override void _Ready()
	{
		_debugCardNameLabel = GetNode<Label>("Label");
		_cardSprite = GetNode<Sprite>("Sprite");
	}

	public override void _Input(InputEvent @event)
	{
		base._Input(@event);
		if (_followsMouse)
		{
			if (@event is InputEventMouseMotion motionEvent)
			{
				Position = motionEvent.Position;
			}
		}
	}

	private void _onMouseDown()
	{
		//_followsMouse = true;
	}

	private void _onMouseUp()
	{
		//_followsMouse = false;
	}

	private void _onButtonPressed()
	{
		EmitSignal(nameof(CardPressed), _id);
	}
}
