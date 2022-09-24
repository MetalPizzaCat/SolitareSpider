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
    public int CardValue
    {
        get
        {
            switch (CardType)
            {
                case CardType.Number:
                    return CardNumericalValue;
                case CardType.Atlas:
                    return 0;
                case CardType.Jester:
                    return 11;
                case CardType.Queen:
                    return 12;
                case CardType.King:
                    return 13;
                default:
                    return -1;
            }
        }
    }
    public CardInfo() { }

    public override string ToString()
    {
        return $"Type :{CardType.ToString()}, Value : {CardNumericalValue}, Suit : {Suit.ToString()}";
    }
}