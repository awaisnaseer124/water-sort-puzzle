using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.Game.UI
{
    public sealed class Toast : MonoBehaviour
    {
        [SerializeField] private Text _text;
        [SerializeField, Min(0.5f)] private float _seconds = 2f;

        public void Show(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            CancelInvoke(nameof(Hide));
            _text.text = message;
            gameObject.SetActive(true);
            Invoke(nameof(Hide), _seconds);
        }

        private void Hide() => gameObject.SetActive(false);
    }
}
