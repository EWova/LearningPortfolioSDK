using System;

using UnityEngine.UI;
using UnityEngine;

namespace EWova.LearningPortfolio
{
    public class BinderButton : UnityEngine.UI.Button
    {
        public enum State
        {
            Normal,
            Highlighted,
            Pressed,
            Selected,
            Disabled,
        }

        public event Action<State> OnChangedSelectionState;
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            OnChangedSelectionState?.Invoke((State)state);
        }

        public void BindingState(Image graphic = null,
                                 Sprite Normal = default,
                                 Sprite Highlighted = default,
                                 Sprite Pressed = default,
                                 Sprite Selected = default,
                                 Sprite Disabled = default)
        {
            OnChangedSelectionState += (state) =>
            {
                switch (state)
                {
                    case BinderButton.State.Normal:
                        graphic.sprite = Normal; break;
                    case BinderButton.State.Highlighted:
                        graphic.sprite = Highlighted != null ? Highlighted : Normal; break;
                    case BinderButton.State.Pressed:
                        graphic.sprite = Pressed != null ? Pressed : Normal; break;
                    case BinderButton.State.Selected:
                        graphic.sprite = Selected != null ? Selected : Normal; break;
                    case BinderButton.State.Disabled:
                        graphic.sprite = Disabled != null ? Disabled : Normal; break;
                }
            };
        }
        public void BindingState(Graphic graphic = null,
                                 Color Normal = default,
                                 Color Highlighted = default,
                                 Color Pressed = default,
                                 Color Selected = default,
                                 Color Disabled = default)
        {
            OnChangedSelectionState += (state) =>
            {
                switch (state)
                {
                    case BinderButton.State.Normal:
                        graphic.color = Normal; break;
                    case BinderButton.State.Highlighted:
                        graphic.color = Highlighted; break;
                    case BinderButton.State.Pressed:
                        graphic.color = Pressed; break;
                    case BinderButton.State.Selected:
                        graphic.color = Selected; break;
                    case BinderButton.State.Disabled:
                        graphic.color = Disabled; break;
                }
            };
        }
    }
}
