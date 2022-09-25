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

/// <summary>
/// Card is a representation of the actual card in the game
/// </summary>
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
	private bool _pendingKill = false;

	public void MarkForDeletion()
	{
		_pendingKill = true;
	}

	public bool Revealed
	{
		get => _revealed;
		set
		{
			_revealed = value;
			if (value)
			{
				_cardSprite.RegionRect = new Rect2(new Vector2(_revealed ? 64 : 0, 0), _cardSprite.RegionRect.Size);
				string val = _info.CardType == CardType.Number ? _info.CardNumericalValue.ToString() : _info.CardType.ToString();
				_cardValueTopLabel.Text = val;
				_cardValueBottomLabel.Text = val;
				int offset = 0;
				switch (Info.Suit)
				{
					case CardSuit.Diamond:
						offset = 64 * 2;
						break;
					case CardSuit.Club:
						offset = 64;
						break;
					case CardSuit.Heart:
						offset = 64 * 3;
						break;
					case CardSuit.Spade:
						offset = 0;
						break;
				}
				_suitSprite.RegionRect = new Rect2(new Vector2(offset, 0), new Vector2(64, 89));
			}
			_suitSprite.Visible = value;
			_cardValueBottomLabel.Visible = value;
			_cardValueTopLabel.Visible = value;
		}
	}

	private bool _moving = false;
	private Vector2 _destination = Vector2.Zero;
	private Vector2 _movementVector = Vector2.Zero;

	private Label _cardValueTopLabel;
	private Label _cardValueBottomLabel;
	private Sprite _cardSprite;
	private Sprite _suitSprite;

	public void Init(CardInfo info, int id, int column, bool revealed = false)
	{
		_info = info;
		_id = id;
		ColumnId = column;
		_revealed = revealed;
	}
	public override void _Ready()
	{
		_cardValueTopLabel = GetNode<Label>("Label");
		_cardValueBottomLabel = GetNode<Label>("Label2");
		_cardSprite = GetNode<Sprite>("Sprite");
		_suitSprite = GetNode<Sprite>("SuitSprite");

		Revealed = _revealed;
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
			if (_pendingKill)
			{
				QueueFree();
			}
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
		if (Revealed && !_pendingKill && !_moving)
		{
			EmitSignal(nameof(CardPressed), _id);
		}
		else
		{
			GD.Print("Can not select card that was not revealed");
		}
	}
}
