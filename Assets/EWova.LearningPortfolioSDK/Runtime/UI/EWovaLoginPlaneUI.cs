using System;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace EWova.LearningPortfolio
{
    public class EWovaLoginPlaneUI : MonoBehaviour
    {
        public VirtualKeyboard.VirtualKeyboard VirtualKeyboard;

        [Header("Login Info")]
        public GameObject LoginInfoRoot;
        public TextMeshProUGUI LoginInfoAccountOrg;
        public TextMeshProUGUI LoginInfoAccountName;
        public BinderButton LoginInfoChangeUserButton;
        public TextMeshProUGUI LoginInfoChangeUserButtonChildText;

        [Header("Connect")]
        public Button ConnectButton;

        [Header("Login")]
        public GameObject LoginRoot;
        public TMP_InputField LoginAccountInput;
        public TMP_InputField LoginPasswordInput;
        public TextMeshProUGUI LoginStateText;
        public Toggle LoginPasswordInputShow;
        public Sprite LoginPasswordInputShowOnNormalImage;
        public Sprite LoginPasswordInputShowOnHighlightedImage;
        public Sprite LoginPasswordInputShowOffNormalImage;
        public Sprite LoginPasswordInputShowOffHighlightedImage;
        public Button LoginButton;
        public BinderButton LoginSkipButton;
        public TextMeshProUGUI LoginSkipButtonChildText;
        public Image LoginSkipButtonChildImage;

        [Header("Check Account")]
        public GameObject CheckAccountRoot;
        public Button CheckAccountStartButton;
        public BinderButton CheckAccountViewLearningPortfolioButton;
        public TextMeshProUGUI CheckAccountViewLearningPortfolioButtonChildText;

        [Header("Log Color")]
        public Color LoginStateTextNormal;
        public Color LoginStateTextWarning;
        public Color LoginStateTextError;

        public Color SecondaryNormalColor;
        public Color SecondaryHighlightedColor;
        public Color SecondaryDisabledColor;

        [Header("Runtime")]
        public TMP_InputField FocusInputField;

        private void Awake()
        {
            LoginAccountInput.onSelect.AddListener((text) =>
            {
                FocusInputField = LoginAccountInput;
            });
            LoginPasswordInput.onSelect.AddListener((text) =>
            {
                FocusInputField = LoginPasswordInput;
            });

            LoginAccountInput.onSubmit.AddListener((text) =>
            {
                LoginPasswordInput.Select();
            });
            LoginPasswordInput.onSubmit.AddListener((text) =>
            {
                LoginButton.onClick.Invoke();
            });

            LoginInfoChangeUserButton.BindingState(
                LoginInfoChangeUserButtonChildText
                , SecondaryNormalColor
                , SecondaryHighlightedColor
                , SecondaryNormalColor
                , SecondaryNormalColor
                , SecondaryDisabledColor);

            LoginSkipButton.BindingState(
                LoginSkipButtonChildText
                , SecondaryNormalColor
                , SecondaryHighlightedColor
                , SecondaryHighlightedColor
                , SecondaryNormalColor
                , SecondaryDisabledColor);
            LoginSkipButton.BindingState(
                LoginSkipButtonChildImage
                , SecondaryNormalColor
                , SecondaryHighlightedColor
                , SecondaryHighlightedColor
                , SecondaryNormalColor
                , SecondaryDisabledColor);

            CheckAccountViewLearningPortfolioButton.BindingState(
                CheckAccountViewLearningPortfolioButtonChildText
                , SecondaryNormalColor
                , SecondaryHighlightedColor
                , SecondaryHighlightedColor
                , SecondaryNormalColor
                , SecondaryDisabledColor);

            LoginPasswordInputShow.onValueChanged.AddListener((enabled) =>
            {
                var temp = LoginPasswordInputShow.spriteState;
                if (enabled)
                {
                    LoginPasswordInputShow.image.sprite = LoginPasswordInputShowOffNormalImage;
                    temp.selectedSprite = LoginPasswordInputShowOffNormalImage;
                    temp.disabledSprite = LoginPasswordInputShowOffNormalImage;

                    temp.highlightedSprite = LoginPasswordInputShowOffHighlightedImage;
                    temp.pressedSprite = LoginPasswordInputShowOffHighlightedImage;
                }
                else
                {
                    LoginPasswordInputShow.image.sprite = LoginPasswordInputShowOnNormalImage;
                    temp.selectedSprite = LoginPasswordInputShowOnNormalImage;
                    temp.disabledSprite = LoginPasswordInputShowOnNormalImage;

                    temp.highlightedSprite = LoginPasswordInputShowOnHighlightedImage;
                    temp.pressedSprite = LoginPasswordInputShowOnHighlightedImage;
                }
                LoginPasswordInputShow.spriteState = temp;
            });
        }

        private void OnEnable()
        {
            VirtualKeyboard.OnTextKeyPress += InputText;
            VirtualKeyboard.OnKeySubmit += InputSubmit;
            VirtualKeyboard.OnKeyBackspace += InputBackspace;
        }
        private void OnDisable()
        {
            VirtualKeyboard.OnTextKeyPress -= InputText;
            VirtualKeyboard.OnKeySubmit -= InputSubmit;
            VirtualKeyboard.OnKeyBackspace -= InputBackspace;
        }

        public void SetLoginStateTextWithException(Exception ex)
        {
            if (LoginStateText.IsDestroyed())
                return;

            UnityEngine.Debug.LogException(ex);
            SetLoginStateText(ex.Message, LogType.Error);
        }

        public void SetLoginStateText(string text, LogType logType = LogType.Log)
        {
            if (LoginStateText.IsDestroyed())
                return;

            if (logType == LogType.Error || logType == LogType.Exception)
                LoginStateText.color = LoginStateTextError;
            else if (logType == LogType.Warning)
                LoginStateText.color = LoginStateTextWarning;
            else
                LoginStateText.color = LoginStateTextNormal;

            LoginStateText.text = text;
        }

        public void ClearLoginStateText()
        {
            if (LoginStateText.IsDestroyed())
                return;

            LoginStateText.text = string.Empty;
        }

        public void InputSubmit(VirtualKeyboard.Key.Args _)
        {
            if (FocusInputField == null)
                return;

            FocusInputField.onSubmit?.Invoke(FocusInputField.text);
        }

        public void InputBackspace(VirtualKeyboard.Key.Args _)
        {
            if (FocusInputField == null)
                return;

            int start = Mathf.Min(FocusInputField.selectionAnchorPosition, FocusInputField.selectionFocusPosition);
            int end = Mathf.Max(FocusInputField.selectionAnchorPosition, FocusInputField.selectionFocusPosition);

            string text = FocusInputField.text;

            if (text.Length == 0 || (start == 0 && start == end))
                return;

            if (start != end)
            {
                text = text.Remove(start, end - start);
                FocusInputField.text = text;
                FocusInputField.caretPosition = start;
            }
            else
            {
                text = text.Remove(start - 1, 1);
                FocusInputField.text = text;
                FocusInputField.caretPosition = start - 1;
            }

            int newPos = FocusInputField.caretPosition;
            FocusInputField.selectionAnchorPosition = newPos;
            FocusInputField.selectionFocusPosition = newPos;

            FocusInputField.ForceLabelUpdate();
            FocusInputField.ActivateInputField();
        }

        public void InputText(string str)
        {
            if (FocusInputField == null || string.IsNullOrEmpty(str))
                return;

            int start = Mathf.Min(FocusInputField.selectionAnchorPosition, FocusInputField.selectionFocusPosition);
            int end = Mathf.Max(FocusInputField.selectionAnchorPosition, FocusInputField.selectionFocusPosition);

            string text = FocusInputField.text;

            if (start != end)
            {
                text = text.Remove(start, end - start);
            }

            text = text.Insert(start, str);
            FocusInputField.text = text;

            int newPos = start + str.Length;
            FocusInputField.caretPosition = newPos;
            FocusInputField.selectionAnchorPosition = newPos;
            FocusInputField.selectionFocusPosition = newPos;

            FocusInputField.ForceLabelUpdate();
            FocusInputField.ActivateInputField();
        }
    }
}