using System;
using ColorSort.Core.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.Game.UI
{
    public sealed class ModeSelectScreen : UIScreen
    {
        [SerializeField] private Button _classic;
        [SerializeField] private Button _timed;
        [Tooltip("Moves and time limit together.")]
        [SerializeField] private Button _hard;
        [SerializeField] private Button _close;

        public event Action<GameMode> ModeChosen;
        public event Action Closed;

        public void Init()
        {
            Bind(_classic, () => ModeChosen?.Invoke(GameMode.Moves));
            Bind(_timed, () => ModeChosen?.Invoke(GameMode.Timed));
            Bind(_hard, () => ModeChosen?.Invoke(GameMode.MovesAndTimed));
            Bind(_close, () => Closed?.Invoke());
        }
    }
}
