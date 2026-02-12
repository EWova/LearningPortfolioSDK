using System;

using UnityEngine;

namespace EWova.VirtualKeyboard
{
    public enum LayoutType
    {
        UpperCase,
        LowerCase,
        Symbol
    }
    public class VirtualKeyboard : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private LayoutType m_layout;
        [Header("Components")]
        public GameObject Root;
        public KeyLayout UpperCaseKeys;
        public KeyLayout LowerCaseKeys;
        public KeyLayout SymbolKeys;
        [Header("Components Fixed Keys")]
        public Key Submit;
        public Key Backspace;
        public Key Space;
        public Key Shift;
        public Sprite ShiftKeepUpperCase;
        public Sprite ShiftUpperCase;
        public Sprite ShiftLowerCase;
        public Key SwitchLayout;
        public Sprite SwitchLayoutCase;
        public Sprite SwitchLayoutSymbol;

        public Action<Key.Args> OnAnyKeyPress;
        public Action<string> OnTextKeyPress;

        public Action<Key.Args> OnKeySubmit;
        public Action<Key.Args> OnKeyBackspace;
        public Action<Key.Args> OnKeySpace;
        public Action<Key.Args> OnKeyShift;
        public Action<Key.Args> OnKeySwitchLayout;

        public LayoutType Layout
        {
            get => m_layout;
            set
            {
                m_layout = value;
                MarkLayoutDirty();
            }
        }

        private bool m_keepUpperCase;

        private bool t_isLayoutDirty;

        public void Show()
        {
            Root.SetActive(true);
            MarkLayoutDirty();
        }
        public void Hide()
        {
            Root.SetActive(false);
            MarkLayoutDirty();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                FocusUpdateLayout();
        }
        private void Awake()
        {
            RegistryFixedKey();
            RegistryKey();

            FocusUpdateLayout();
        }
        private void Update()
        {
            if (!Root.activeSelf)
                return;

            if (t_isLayoutDirty)
            {
                t_isLayoutDirty = false;
                FocusUpdateLayout();
            }
        }
        private void RegistryFixedKey()
        {
            Shift.OnPress += args =>
            {
                m_layout = m_layout switch
                {
                    LayoutType.LowerCase => LayoutType.UpperCase,
                    LayoutType.UpperCase => LayoutType.LowerCase,
                    LayoutType.Symbol => LayoutType.Symbol,
                    _ => m_layout
                };
                m_keepUpperCase = false;
                OnKeyShift?.Invoke(args);
                OnAnyKeyPress?.Invoke(args);
                MarkLayoutDirty();
            };
            Shift.OnDoublePress += args =>
            {
                m_layout = m_layout switch
                {
                    LayoutType.LowerCase => LayoutType.UpperCase,
                    LayoutType.UpperCase => LayoutType.UpperCase,
                    LayoutType.Symbol => m_layout,
                    _ => m_layout
                };
                m_keepUpperCase = true;
                OnKeyShift?.Invoke(args);
                OnAnyKeyPress?.Invoke(args);
                MarkLayoutDirty();
            };
            SwitchLayout.OnPress += args =>
            {
                m_layout = m_layout switch
                {
                    LayoutType.UpperCase => LayoutType.Symbol,
                    LayoutType.LowerCase => LayoutType.Symbol,
                    LayoutType.Symbol => LayoutType.LowerCase,
                    _ => LayoutType.LowerCase
                };
                OnKeySwitchLayout?.Invoke(args);
                OnAnyKeyPress?.Invoke(args);
                MarkLayoutDirty();
            };
            Submit.OnPress += args =>
            {
                OnKeySubmit?.Invoke(args);
                OnAnyKeyPress?.Invoke(args);
            };
            Backspace.OnPress += args =>
            {
                OnKeyBackspace?.Invoke(args);
                OnAnyKeyPress?.Invoke(args);
            };
            Space.OnPress += args =>
            {
                OnKeySpace?.Invoke(args);
                OnAnyKeyPress?.Invoke(args);
            };
        }
        private void RegistryKey()
        {
            foreach (var key in UpperCaseKeys) key.OnPress += args => PressKey(args);
            foreach (var key in LowerCaseKeys) key.OnPress += args => PressKey(args);
            foreach (var key in SymbolKeys) key.OnPress += args => PressKey(args);
        }
        private void PressKey(Key.Args args)
        {
            if (!args.IsSpecialKey && !args.IsDoublePressed)
            {
                if (m_layout is LayoutType.UpperCase && !m_keepUpperCase)
                {
                    m_layout = LayoutType.LowerCase;
                    MarkLayoutDirty();
                }
                OnTextKeyPress(args.Text);
            }
            OnAnyKeyPress?.Invoke(args);
        }
        public void MarkLayoutDirty()
        {
            t_isLayoutDirty = true;
        }
        public void FocusUpdateLayout()
        {
            UpperCaseKeys.IsVisible = m_layout is LayoutType.UpperCase;
            LowerCaseKeys.IsVisible = m_layout is LayoutType.LowerCase;
            SymbolKeys.IsVisible = m_layout is LayoutType.Symbol;

            Shift.Symbol = m_layout switch
            {
                LayoutType.UpperCase => m_keepUpperCase ? ShiftKeepUpperCase : ShiftUpperCase,
                LayoutType.LowerCase => ShiftLowerCase,
                _ => ShiftLowerCase
            };

            Shift.Interactable = m_layout is LayoutType.LowerCase or LayoutType.UpperCase;

            SwitchLayout.Symbol = m_layout switch
            {
                LayoutType.UpperCase => SwitchLayoutSymbol,
                LayoutType.LowerCase => SwitchLayoutSymbol,
                LayoutType.Symbol => SwitchLayoutCase,
                _ => SwitchLayoutSymbol
            };
        }
    }
}