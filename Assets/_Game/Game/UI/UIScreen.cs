using System;
using UnityEngine.UI;
using UnityEngine;

namespace ColorSort.Game.UI
{
    public abstract class UIScreen : MonoBehaviour
    {
        public bool IsVisible => gameObject.activeSelf;

        public virtual void Show() => gameObject.SetActive(true);

        public virtual void Hide() => gameObject.SetActive(false);

        protected void Bind(Button button, Action onClick)
        {
            if (button == null)
                throw new InvalidOperationException($"{GetType().Name} on '{name}' has an unassigned button.");
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }
    }
}
