using System.Linq;
using ColorSort.Core.Board;

namespace ColorSort.Core.Tests
{
    internal static class TestBoards
    {
        public static Bottle Bottle(int capacity, string layers) =>
            new Bottle(capacity, layers.Select(c => (byte)c).ToArray());

        public static BoardState Make(int capacity, params string[] bottles) =>
            new BoardState(bottles.Select(b => Bottle(capacity, b)));

        public static string Describe(Bottle bottle) =>
            new string(bottle.ToArray().Select(b => (char)b).ToArray());

        public static string[] Describe(BoardState board) =>
            board.Bottles.Select(Describe).ToArray();
    }
}
