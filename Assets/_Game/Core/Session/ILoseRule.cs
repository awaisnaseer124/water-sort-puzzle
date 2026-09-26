namespace ColorSort.Core.Session
{
    public interface ILoseRule
    {
        bool IsLost(GameSession session);
    }
}
