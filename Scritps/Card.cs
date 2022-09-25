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

	/// <summary>
	/// Top speed at which cards can move. To prevent cards from teleporting when moving from one side of screen to other
	/// </summary>
	[Export]
	public readonly float MaxSpeed = 650f;

	/// <summary>
	/// How fast cards move on the screen
	/// </summary>
	[Export]
	public readonly float TravelTime = 1f;

	private float _speed = 1000f;

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

	private bool _moving = false;
	private Vector2 _destination = Vector2.Zero;
	private Vector2 _movementVector = Vector2.Zero;

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

	public override void _Process(float delta)
	{
		base._Process(delta);
		if (!_moving)
		{
			return;
		}
		Position += _movementVector * _speed * delta;
		if (Position.DistanceTo(_destination) <= 10f)
		{
			Position = _destination;
			_moving = false;
		}
	}

	/// <summary>
	/// Starts animation for moving card to the destination
	/// </summary>
	public void MoveTo(Vector2 destination)
	{
		_moving = true;
		_destination = destination;
		_movementVector = (destination - Position).Normalized();
		_speed = Mathf.Min((destination - Position).Length() / TravelTime, MaxSpeed);
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
