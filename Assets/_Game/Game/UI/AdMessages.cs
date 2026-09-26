using ColorSort.Services.Ads;

namespace ColorSort.Game.UI
{
    public static class AdMessages
    {
        public static string For(AdResult result)
        {
            switch (result)
            {
                case AdResult.Skipped: return "Watch the whole video to get the reward";
                case AdResult.NotAvailable: return "No video available right now, try again soon";
                case AdResult.Failed: return "The video could not be played";
                default: return null; // Completed needs no message; Busy means an ad is already on screen
            }
        }
    }
}
