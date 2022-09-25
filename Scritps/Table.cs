/**
	Main file of the game where most of the game logic is contained
*/
//Uncomment this define to remove all placement validity checks
//#define DEBUG_CARD_LOOSE_CHECK

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

    private Position2D _cardStartPosition;

    private TableState _state = TableState.Idle;
    public TableState State => _state;

    private Timer _animationTimer;
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

    private int _currentInitialDealCardId = 0;
    private int _initialDealColumnId = 0;

    private int _currentlyMovedCardId = -1;
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

    /// <summary>
    /// Deal cards until first 54 cards were dealt, using timers to call this function
    /// </summary>
    private void _dealInitialDeck()
    {
        if (_currentInitialDealCardId < 54) // rules of the game define that it deals 54 cards first
        {
            Card card = CardScene.Instance<Card>();
            card.Init(_deck[_currentInitialDealCardId], _currentInitialDealCardId, _initialDealColumnId);
            AddChild(card);
            card.Connect(nameof(Card.CardPressed), this, nameof(_onCardPressed));
            card.Position = new Vector2(_initialDealColumnId * CardWidth, (_currentInitialDealCardId / 10) * VerticalOffset) + _cardStartPosition.Position;
            _currentCards.Add(_currentInitialDealCardId, card);
            _columns[_initialDealColumnId].Add(_currentInitialDealCardId);
            _initialDealColumnId++;
            _currentInitialDealCardId++;

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
    }

    public override void _Ready()
    {
        _animationTimer = GetNode<Timer>("DeckDealingAnimationTimer");
        _cardStartPosition = GetNode<Position2D>("CardStartPosition");
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
            _moveCards(_currentlyMovedCardId , id);
        }
    }

    private void _onAnimationTimerTimeout()
    {
        _dealInitialDeck();
    }
}
