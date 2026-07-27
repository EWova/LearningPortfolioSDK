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

        [Header("NotSupportLogin")]
        public GameObject NotSupportLoginRoot;
        //TODO 未來可以讓使用者輸入 LaunchTicket 來登入，避免 DeepLink 失效或不支援問題。待確認需求後再開發。
        public TMP_InputField NotSupportLoginLaunchTicketInputField;

        [Header("Connect")]
        public Button ReconnectButton;
        public Button ConnectingButton;

        [Header("Login")]
        public GameObject LoginRoot;
        public TextMeshProUGUI LoginStateText;
        public Button LoginButton;
        public BinderButton LoginSkipButton;
        public TextMeshProUGUI LoginSkipButtonChildText;
        public Image LoginSkipButtonChildImage;

        [Header("Login Redirect")]
        public GameObject LoginRedirectRoot;
        public Transform LoginRedirectIconRotate;
        public BinderButton CancelLoginButton;
        public GameObject LoginRedirectPCIssueTipText;

        [Header("Getting User Data")]
        public GameObject GettingUserDataRoot;
        public TextMeshProUGUI GettingUserDataStateText;
        public Button CancelGettingUserDataButton;

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

            CancelLoginButton.BindingState(
                CancelLoginButton.GetComponentInChildren<TextMeshProUGUI>()
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

            LoginRedirectPCIssueTipText.SetActive(false);
        }
        private void Update()
        {
            if (LoginRedirectRoot.activeSelf)
            {
                LoginRedirectIconRotate.Rotate(0f, 0f, -120f * Time.deltaTime);
            }
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