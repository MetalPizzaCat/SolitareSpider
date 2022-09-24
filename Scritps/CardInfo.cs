public class CardInfo
{
    public readonly CardType CardType = CardType.Number;

    public readonly int CardNumericalValue = 1;
    public readonly CardSuit Suit = CardSuit.Diamond;

    public CardInfo(CardType cardType, int cardNumericalValue, CardSuit suit)
    {
        CardType = cardType;
        CardNumericalValue = cardNumericalValue;
        Suit = suit;
    }
    public CardInfo() { }

    public override string ToString()
    {
        return $"Type :{CardType.ToString()}, Value : {CardNumericalValue}, Suit : {Suit.ToString()}";
    }
}