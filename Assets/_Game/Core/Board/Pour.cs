namespace ColorSort.Core.Board
{
    public readonly struct Pour
    {
        public Pour(int from, int to, byte color, int amount)
        {
            From = from;
            To = to;
            Color = color;
            Amount = amount;
        }

        public int From { get; }
        public int To { get; }
        public byte Color { get; }
        public int Amount { get; }

        public override string ToString() => $"{From}->{To} ({Amount}x{Color})";
    }

    public enum PourCheck
    {
        Allowed,
        InvalidIndex,
        SameBottle,
        SourceEmpty,
        SourceComplete,
        TargetFull,
        ColorMismatch,
        SessionOver,
    }
}
