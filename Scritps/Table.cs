using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
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

	private Timer _animationTimer;
	private List<CardInfo> _deck = new List<CardInfo>();
	/// <summary>
	/// Array of size 10 of dynamic arrays of cards
	/// </summary>
	private List<Card>[] _columns = new List<Card>[10];

	private int _currentCardId = 0;
	private int _initialDealColumnId = 0;
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
			card.Init(_deck[_currentCardId]);
			AddChild(card);
			card.Position = new Vector2(_initialDealColumnId * CardWidth, (_currentCardId / 10) * VerticalOffset) + _cardStartPosition.Position;

			_columns[_initialDealColumnId].Add(card);
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

	private void _finishInitialDeal()
	{
		for (int i = 0; i < 10; i++)
		{
			_columns[i].Last().Revealed = true;
		}
	}

	public override void _Ready()
	{
		_animationTimer = GetNode<Timer>("DeckDealingAnimationTimer");
		_cardStartPosition = GetNode<Position2D>("CardStartPosition");
		for (int i = 0; i < 10; i++)
		{
			_columns[i] = new List<Card>();
		}

		_generateDeck();
		_animationTimer.Start();
	}

	private void _onAnimationTimerTimeout()
	{
		_dealInitialDeck();
	}
}
