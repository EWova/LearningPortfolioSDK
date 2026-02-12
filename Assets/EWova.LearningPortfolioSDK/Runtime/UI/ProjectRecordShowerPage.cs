using System;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace EWova.LearningPortfolio
{
    public class ProjectRecordShowerPage : MonoBehaviour
    {
        public Action OnClick;
        public TextMeshProUGUI Label;
        public BinderButton BackgroundButton;
        public LayoutElement LayoutElement;

        [Space]
        public Color SecondaryNormalColor;
        public Color SecondaryHighlightedColor;
        public Color SecondaryDisabledColor;

        public bool IsSelected
        {
            get => BackgroundButton.interactable == false;
            set => BackgroundButton.interactable = !value;
        }

        public float LabelTextSize
        {
            get => Label.fontSize;
            set => Label.fontSize = value;
        }
        public string LabelText
        {
            get => Label.text;
            set => Label.text = value;
        }

        public void ForceUpdate()
        {
            LayoutElement.minWidth = LayoutElement.preferredWidth = m_width + m_paddingX;
            LayoutElement.minHeight = LayoutElement.preferredHeight = m_height + m_paddingY;
        }

        private float m_paddingX;
        private float m_paddingY;
        private float m_width;
        private float m_height;

        public float PaddingX
        {
            get => m_paddingX;
            set
            {
                m_paddingX = value;
                ForceUpdate();
            }
        }
        public float PaddingY
        {
            get => m_paddingY;
            set
            {
                m_paddingY = value;
                ForceUpdate();
            }
        }
        public float Width
        {
            get => m_width;
            set
            {
                m_width = value;
                ForceUpdate();
            }
        }
        public float Height
        {
            get => m_height;
            set
            {
                m_height = value;
                ForceUpdate();
            }
        }

        private void Awake()
        {
            BackgroundButton.onClick.AddListener(Click);
            BackgroundButton.BindingState(Label,
                                          SecondaryNormalColor,
                                          SecondaryHighlightedColor,
                                          SecondaryNormalColor,
                                          SecondaryNormalColor,
                                          SecondaryDisabledColor);
        }
        private void Click()
        {
            OnClick?.Invoke();
        }
    }
}