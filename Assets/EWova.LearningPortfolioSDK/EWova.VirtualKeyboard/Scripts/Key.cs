using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EWova.VirtualKeyboard
{
    public class Key : MonoBehaviour, IPointerDownHandler
    {
        public struct Args
        {
            public static implicit operator string(Args args) => args.Text;
            public bool IsDoublePressed;
            public bool IsSpecialKey;
            public string Text;
        }

        public Button Button;
        public Image Image;
        [Space]
        public bool IsDoublePressEnabled;
        public bool IsSpecialKey;
        public string Code;

        public Action<Args> OnPress;
        public Action<Args> OnDoublePress;

        private float m_doubleClickThreshold = 0.33333333f;
        private float t_lastClickTime = -1f;


        // Note: If you prefer to use Button's onClick event instead of IPointerDownHandler,

        //private void Awake()
        //{
        //    Button.onClick.AddListener(OnClick);
        //}

        // Note: IPointerDownHandler is used here to provide a more responsive experience.
        public void OnPointerDown(PointerEventData eventData)
        {
            OnClick();
        }


        private void OnClick()
        {
            if (!IsDoublePressEnabled)
            {
                TriggerPress();
            }
            else
            {
                float time = Time.time;

                TriggerPress();

                if (time - t_lastClickTime <= m_doubleClickThreshold)
                {
                    TriggerDoublePress();
                    t_lastClickTime = -1f;
                }
                else
                {
                    t_lastClickTime = time;
                }
            }
        }

        private void TriggerPress()
        {
            OnPress?.Invoke(new Args
            {
                IsDoublePressed = false,
                IsSpecialKey = IsSpecialKey,
                Text = Code
            });
        }

        private void TriggerDoublePress()
        {
            OnDoublePress?.Invoke(new Args
            {
                IsDoublePressed = true,
                IsSpecialKey = IsSpecialKey,
                Text = Code
            });
        }


        public bool Interactable
        {
            get => Button.interactable;
            set
            {
                if (Button.interactable != value)
                    Button.interactable = value;
            }
        }

        public Sprite Symbol
        {
            get => Image.sprite;
            set
            {
                if (Image.sprite != value)
                    Image.sprite = value;
            }
        }
    }
}
