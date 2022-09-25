/**
	Main file of the game where most of the game logic is contained
*/
//Uncomment this define to remove all placement validity checks
//#define DEBUG_CARD_LOOSE_CHECK

//Uncomment to make additionally cards get immediately get deleted once spawned
//#define DEBUG_SUICIDE_CARDS

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;


public enum TableState
{
	Idle,
	Moving
}
/// <summary>
/// Table is the main class of the game that contains card placement and interaction logic
/// </summary>
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
	[Export]
	public readonly PackedScene EmptyColumnButtonScene;

	private int _currentCompletionCount = 0;

	private Position2D _cardStartPosition;
	private Vector2 _cardSpawnPosition = Vector2.Zero;

	private TableState _state = TableState.Idle;
	public TableState State => _state;

	private Timer _animationTimer;
	private Timer _cardDealAnimTimer;
	private List<CardInfo> _deck = new List<CardInfo>();
	/// <summary>
	/// Array of size 10 of dynamic arrays of cards
	/// </summary>
	private List<int>[] _columns = new List<int>[10];

	private List<EmptyColumnButton> _emptyColumnButtons = new List<EmptyColumnButton>();

	/// <summary>
	/// Current interactive cards. Key matches index of a card in the _deck array
	/// </summary>
	/// <typeparam name="int">Id of the card in the main card deck</typeparam>
	/// <typeparam name="Card">Interactive card object itself</typeparam>
	private Dictionary<int, Card> _currentCards = new Dictionary<int, Card>();

	private int _lastDealtCard = 0;
	private int _initialDealColumnId = 0;
	private int _currentlyMovedCardId = -1;

	private int _currentCardDealCount = 0;
	private void _generateDeck()
	{
		//spider uses two 52 card decks
		for (int deckCount = 0; deckCount < 2; deckCount++)
		{
			//generate the actual deck
			for (int i = 2; i <= 10; i++)
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
		}
		Random rng = new Random();
		//shuffle the deck
		_deck = _deck.OrderBy(p => rng.Next()).ToList();
#if DEBUG_PRINT_CARDS
		foreach (CardInfo info in _deck)
		{
			GD.Print(info.ToString());
		}
#endif
	}

	private void _spawnCard(Vector2 destination, int id, int column, bool reveal = false)
	{
		Card card = CardScene.Instance<Card>();
		card.Init(_deck[id], id, column, reveal);
		AddChild(card);
		card.Connect(nameof(Card.CardPressed), this, nameof(_onCardPressed));
		card.Position = _cardSpawnPosition;
		card.MoveTo(destination);
		_currentCards.Add(id, card);
		_columns[column].Add(id);
	}

	/// <summary>
	/// Deal cards until first 54 cards were dealt, using timers to call this function
	/// </summary>
	private void _dealInitialDeck()
	{
		if (_lastDealtCard < 54) // rules of the game define that it deals 54 cards first
		{
			_spawnCard
			(
				new Vector2(_initialDealColumnId * CardWidth, (_lastDealtCard / 10) * VerticalOffset) + _cardStartPosition.Position,
				_lastDealtCard,
				_initialDealColumnId
			);
			_initialDealColumnId++;
			_lastDealtCard++;

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

	/// <summary>
	/// Checks if the given card can be used as the destination
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	private bool _isValidDestinationCard(int id)
	{
#if !DEBUG_CARD_LOOSE_CHECK
		//cards that already have cards on top of them are not valid destinations
		return _columns[_currentCards[id].ColumnId].IndexOf(id) == (_columns[_currentCards[id].ColumnId].Count - 1);
#else
		return true;
#endif

	}

	private bool _isValidSelectionCard(int id)
	{
		//Valid selection cards are cards the are
		//1) Revealed
		//2) Have no cards on top of them
		//3) Or if they have cards on top of them, they follow the proper order
		if (!_currentCards[id].Revealed)
		{
			return false;
		}
#if !DEBUG_CARD_LOOSE_CHECK
		if (_columns[_currentCards[id].ColumnId].Last() == id)
		{
			return true;
		}

		List<int> column = _columns[_currentCards[id].ColumnId];
		int start = column.IndexOf(id);
		for (int i = start + 1; i < column.Count; i++)
		{
			if (_currentCards[column[i - 1]].Info.CardValue - 1 != _currentCards[column[i]].Info.CardValue)
			{
				return false;
			}
		}
#endif
		return true;
	}

	/// <summary>
	/// React to card being pressed
	/// </summary>
	/// <param name="id"></param>
	private void _onCardPressed(int id)
	{
		//if we have a current card, assume that we are trying to place a card
		if (_currentlyMovedCardId == -1)
		{
			if (_isValidSelectionCard(id))
			{
				_currentlyMovedCardId = id;
				_state = TableState.Moving;
			}
			else
			{
				GD.Print("This card can not be selected");
			}
			return;

		}
#if !DEBUG_CARD_LOOSE_CHECK
		//We can only place card on a card if destination card is one point of value higher
		if (_deck[_currentlyMovedCardId].CardValue + 1 != _deck[id].CardValue || !_isValidDestinationCard(id))
		{
			GD.Print("Can not put card there!");
			_state = TableState.Idle;
			_currentlyMovedCardId = -1;
			return;
		}
#endif
		_moveCards(_currentlyMovedCardId, _currentCards[id].ColumnId);
		_currentlyMovedCardId = -1;
	}

	/// <summary>
	/// Checks if all cards from id to the last one in the column form proper order
	/// </summary>
	/// <param name="id">Id of the card to start the check from, index must be taken from column and not the card id</param>
	/// <param name="column">Id of the column</param>
	/// <returns>true if all cards from id to the last one in the column form proper order</returns>
	private bool _isFullDeck(int id, int column)
	{
		//there must be 12 cards
		//anything more means there is something on top of the column
		//anything less means there is not enough cards to form a deck
		if (_columns[column].Count() - 1 - id != 12)
		{
			return false;
		}
		//proper order is king,queen,jester,10,9,8,7,6,5,4,3,2,atlas
		if (_currentCards[_columns[column][id]].Info.CardType != CardType.King)
		{
			return false;
		}
		for (int i = 1; i < 12; i++)
		{
			//every card must be one unit smaller then previous one
			if (_currentCards[_columns[column][i + id]].Info.CardValue != _currentCards[_columns[column][i + id - 1]].Info.CardValue - 1)
			{
				return false;
			}
		}
		return true;
	}

	private void _checkCompletion()
	{
		if (_currentCompletionCount >= 8)
		{
			GD.Print("YOU WON!");
		}
	}
	private void _checkColumn(int column)
	{
		for (int i = 0; i < _columns[column].Count; i++)
		{
			if (_isFullDeck(i, column))
			{
				for (int j = i; j < _columns[column].Count; j++)
				{
					_currentCards[_columns[column][j]].MoveTo(GetNode<Node2D>("FinalDestinationLocation").Position);
					_currentCards[_columns[column][j]].MarkForDeletion();
					_currentCards.Remove(_columns[column][j]);
				}
				_columns[column].RemoveRange(i, _columns[column].Count - i);
				if (_columns[column].Count > 0)
				{
					_currentCards[_columns[column].Last()].Revealed = true;
				}
				_currentCompletionCount++;
				GetNode<Node2D>($"FinalDestinationLocation/Sprite{_currentCompletionCount}").Visible = true;
				GD.Print("You collected full deck!");
				_checkCompletion();
				return;
			}
		}
	}

	/// <summary>
	/// Moves a card(and every card below it) to the row of the dstCard
	/// </summary>
	/// <param name="dstCardId"></param>
	private void _moveCards(int srcCard, int columnId)
	{
		if (!_currentCards.ContainsKey(srcCard))
		{
			GD.PrintErr($"Attempted to move card with id {srcCard}, but there is no card with this id");
			return;
		}
		Card movedCard = _currentCards[srcCard];
		List<int> dstColumn = _columns[columnId];
		List<int> srcColumn = _columns[movedCard.ColumnId];

		//if card is not the last card in the column all cards that are attached to it must also move
		//unless one of the cards in the sequence doesn't follow the descending number
		if (srcColumn.Last() != srcCard)
		{
			int ind = srcColumn.IndexOf(srcCard);
			int count = srcColumn.Count() - ind;
			int previousDstColumnLength = dstColumn.Count;
			List<int> movedCards = srcColumn.GetRange(ind, count);

			dstColumn.AddRange(movedCards);
			srcColumn.RemoveRange(ind, count);

			foreach (int cardId in movedCards)
			{
				_currentCards[cardId].MoveTo(new Vector2(columnId * CardWidth, previousDstColumnLength * VerticalOffset) + _cardStartPosition.Position);
				RemoveChild(_currentCards[cardId]);
				AddChild(_currentCards[cardId]);
				_currentCards[cardId].ColumnId = columnId;
				previousDstColumnLength++;
			}
		}
		else
		{
			srcColumn.Remove(srcCard);
			dstColumn.Add(srcCard);
			movedCard.MoveTo(new Vector2(columnId * CardWidth, (dstColumn.Count - 1) * VerticalOffset) + _cardStartPosition.Position);
			//re-add child node so that it would be put lower in the scene tree and would not mess with rendering and button presses
			RemoveChild(movedCard);
			AddChild(movedCard);
			movedCard.ColumnId = columnId;
		}
		if (srcColumn.Count > 0)
		{
			_currentCards[srcColumn.Last()].Revealed = true;
		}
		_checkColumn(columnId);

	}

	/// <summary>
	/// Reveal last dealt card in each column
	/// </summary>
	private void _finishInitialDeal()
	{
		for (int i = 0; i < 10; i++)
		{
			_currentCards[_columns[i].Last()].Revealed = true;
		}
#if DEBUG_SUICIDE_CARDS
		for (int i = 0; i < 10; i++)
		{
			_currentCards[_columns[i].Last()].MoveTo(GetNode<Node2D>("FinalDestinationLocation").Position);
			_currentCards[_columns[i].Last()].MarkForDeletion();
			_currentCards.Remove(_columns[i].Last());
		}
#endif
	}

	public override void _Ready()
	{
		_animationTimer = GetNode<Timer>("DeckDealingAnimationTimer");
		_cardStartPosition = GetNode<Position2D>("CardStartPosition");
		_cardDealAnimTimer = GetNode<Timer>("CardDealingAnimationTimer");
		_cardSpawnPosition = GetNode<Node2D>("Node2D").Position;
		for (int i = 0; i < 10; i++)
		{
			_columns[i] = new List<int>();
			EmptyColumnButton btn = EmptyColumnButtonScene.Instance<EmptyColumnButton>();
			btn.ColumnId = i;
			AddChild(btn);
			btn.Position = new Vector2(i * CardWidth, 0) + _cardStartPosition.Position;
			_emptyColumnButtons.Add(btn);
			btn.Connect(nameof(EmptyColumnButton.OnEmptyColumnSelected), this, nameof(_onEmptyColumnSelected));
		}

		_generateDeck();
		_animationTimer.Start();
	}

	private void _onEmptyColumnSelected(int id)
	{
		if (_currentlyMovedCardId != -1)
		{
			_moveCards(_currentlyMovedCardId, id);
			_currentlyMovedCardId = -1;
		}
	}

	private void _onAnimationTimerTimeout()
	{
		_dealInitialDeck();
	}
	private void _onAddMoreCardsButtonPressed()
	{
		foreach (List<int> column in _columns)
		{
			if (column.Count == 0)
			{
				GD.Print("Can request more cards only if all columns have cards");
				return;
			}
		}
		if (_lastDealtCard >= _deck.Count)
		{
			GetNode<Node2D>("Node2D").Visible = false;
			return;
		}
		_cardDealAnimTimer.Start();
	}

	private void _onCardDealingAnimationTimerTimeout()
	{
		if (_currentCardDealCount < 10 && _lastDealtCard < _deck.Count)
		{
			_spawnCard
			(
				new Vector2(_currentCardDealCount * CardWidth, _columns[_currentCardDealCount].Count * VerticalOffset) + _cardStartPosition.Position,
				_lastDealtCard,
				_currentCardDealCount,
				true
			);
			_cardDealAnimTimer.Start();
			_lastDealtCard++;
			_currentCardDealCount++;
		}
		else
		{
			_currentCardDealCount = 0;
		}
	}
}
