using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public enum TableState
{
	Idle,
	Moving
}
public class Table : Node2D
{
	[Export]
	public readonly int DeckSize = 104;

	[Export]
	public readonly int CardWidth = 96;

	[Export]
	public readonly int VerticalOffset = 15;

	[Export]
	public readonly PackedScene CardScene;

	private Position2D _cardStartPosition;

	private TableState _state = TableState.Idle;
	public TableState State => _state;

	private Timer _animationTimer;
	private List<CardInfo> _deck = new List<CardInfo>();
	/// <summary>
	/// Array of size 10 of dynamic arrays of cards
	/// </summary>
	private List<int>[] _columns = new List<int>[10];

	/// <summary>
	/// Current interactive cards. Key matches index of a card in the _deck array
	/// </summary>
	/// <typeparam name="int">Id of the card in the main card deck</typeparam>
	/// <typeparam name="Card">Interactive card object itself</typeparam>
	private Dictionary<int, Card> _currentCards = new Dictionary<int, Card>();

	private int _currentCardId = 0;
	private int _initialDealColumnId = 0;

	private int _currentlyMovedCardId = -1;
	private void _generateDeck()
	{
		Random rng = new Random();
		//generate the actual deck
		for (int i = 1; i <= 10; i++)
		{
			foreach (CardSuit suit in (CardSuit[])Enum.GetValues(typeof(CardSuit)))
			{
				_deck.Add(new CardInfo(CardType.Number, i, suit));
			}
		}
		foreach (CardSuit suit in (CardSuit[])Enum.GetValues(typeof(CardSuit)))
		{
			_deck.Add(new CardInfo(CardType.Atlas, -1, suit));
			_deck.Add(new CardInfo(CardType.Jester, -1, suit));
			_deck.Add(new CardInfo(CardType.King, -1, suit));
			_deck.Add(new CardInfo(CardType.Queen, -1, suit));
		}
		//shuffle the deck
		_deck = _deck.OrderBy(p => rng.Next()).ToList();
#if DEBUG_PRINT_CARDS
		foreach (CardInfo info in _deck)
		{
			GD.Print(info.ToString());
		}
#endif
	}

	private void _dealInitialDeck()
	{
		if (_currentCardId < 54) // rules of the game define that it deals 54 cards first
		{
			Card card = CardScene.Instance<Card>();
			card.Init(_deck[_currentCardId], _currentCardId, _initialDealColumnId);
			AddChild(card);
			card.Connect(nameof(Card.CardPressed), this, nameof(_onCardPressed));
			card.Position = new Vector2(_initialDealColumnId * CardWidth, (_currentCardId / 10) * VerticalOffset) + _cardStartPosition.Position;
			_currentCards.Add(_currentCardId, card);
			_columns[_initialDealColumnId].Add(_currentCardId);
			_initialDealColumnId++;
			_currentCardId++;

			if (_initialDealColumnId >= 10)
			{
				_initialDealColumnId = 0;
			}
			_animationTimer.Start();
		}
		else
		{
			_finishInitialDeal();
		}
	}

	private bool _isValidDestinationCard(int id)
	{
		//cards that already have cards on top of them are not valid destinations
		return _columns[_currentCards[id].ColumnId].IndexOf(id) == (_columns[_currentCards[id].ColumnId].Count - 1);
	}
	private void _onCardPressed(int id)
	{
		//if we have a current card, assume that we are trying to place a card
		if (_currentlyMovedCardId == -1)
		{
			_currentlyMovedCardId = id;
			_state = TableState.Moving;
			return;

		}
		_state = TableState.Idle;
		if (_deck[_currentlyMovedCardId].CardValue >= _deck[id].CardValue || !_isValidDestinationCard(id))
		{
			GD.Print("Can not put card there!");
			_currentlyMovedCardId = -1;
			return;
		}

		Card movedCard = _currentCards[_currentlyMovedCardId];
		List<int> dstColumn = _columns[_currentCards[id].ColumnId];
		List<int> srcColumn = _columns[movedCard.ColumnId];

		//if card is not the last card in the column all cards that are attached to it must also move
		//unless one of the cards in the sequence doesn't follow the descending number
		if (srcColumn.Last() != _currentlyMovedCardId)
		{
			int ind = srcColumn.IndexOf(_currentlyMovedCardId);
			int count = srcColumn.Count() - ind;
			int previousDstColumnLength = dstColumn.Count;
			List<int> movedCards = srcColumn.GetRange(ind, count);

			dstColumn.AddRange(movedCards);
			srcColumn.RemoveRange(ind, count);

			foreach (int cardId in movedCards)
			{
				_currentCards[cardId].Position = new Vector2(_currentCards[id].ColumnId * CardWidth, previousDstColumnLength * VerticalOffset) + _cardStartPosition.Position;
				RemoveChild(_currentCards[cardId]);
				AddChild(_currentCards[cardId]);
				_currentCards[cardId].ColumnId = _currentCards[id].ColumnId;
				previousDstColumnLength++;
			}
		}
		else
		{
			srcColumn.Remove(_currentlyMovedCardId);
			dstColumn.Add(_currentlyMovedCardId);
			movedCard.Position = new Vector2(_currentCards[id].ColumnId * CardWidth, (dstColumn.Count - 1) * VerticalOffset) + _cardStartPosition.Position;
			//re-add child node so that it would be put lower in the scene tree and would not mess with rendering and button presses
			RemoveChild(movedCard);
			AddChild(movedCard);
			movedCard.ColumnId = _currentCards[id].ColumnId;
		}
		if (srcColumn.Count > 0)
		{
			_currentCards[srcColumn.Last()].Revealed = true;
		}

		_currentlyMovedCardId = -1;
	}

	private void _finishInitialDeal()
	{
		for (int i = 0; i < 10; i++)
		{
			_currentCards[_columns[i].Last()].Revealed = true;
		}
	}

	public override void _Ready()
	{
		_animationTimer = GetNode<Timer>("DeckDealingAnimationTimer");
		_cardStartPosition = GetNode<Position2D>("CardStartPosition");
		for (int i = 0; i < 10; i++)
		{
			_columns[i] = new List<int>();
		}

		_generateDeck();
		_animationTimer.Start();
	}

	private void _onAnimationTimerTimeout()
	{
		_dealInitialDeck();
	}
}
