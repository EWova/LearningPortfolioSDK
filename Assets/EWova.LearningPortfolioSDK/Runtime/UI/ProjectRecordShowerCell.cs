using TMPro;

using UnityEngine;
using UnityEngine.UI;
namespace EWova.LearningPortfolio
{

    public class ProjectRecordShowerCell : MonoBehaviour
    {
        public TextMeshProUGUI Label;
        public Image OutlineImage;
        public Image BackgroundImage;
        public LayoutElement LayoutElement;

        private TextAlignmentOptions _originTextAlignmentOptions;
        private TextAlignmentOptions _destinationTextAlignmentOptions;
        public TextAlignmentOptions? OverrideTextAlignmentOptions 
        {
            get
            {
                if (_destinationTextAlignmentOptions == _originTextAlignmentOptions)
                    return null;
                return _destinationTextAlignmentOptions;
            }
            set
            {
                if (value == null)
                {
                    _destinationTextAlignmentOptions = _originTextAlignmentOptions;
                }
                else
                {
                    _destinationTextAlignmentOptions = value.Value;
                }
                Label.alignment = _destinationTextAlignmentOptions;
            }
        }

        public void Init()
        {
            _originTextAlignmentOptions = Label.alignment;
            _destinationTextAlignmentOptions = _originTextAlignmentOptions;
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
        public string LabelText
        {
            get => Label.text;
            set => Label.text = value;
        }
        public Color LabelTextColor
        {
            get => Label.color;
            set => Label.color = value;
        }
        public TextAlignmentOptions LabelTextAlignment
        {
            get => Label.alignment;
            set => Label.alignment = value;
        }
        public Color OutlineColor
        {
            get => OutlineImage.color;
            set => OutlineImage.color = value;
        }
        public Color BackgroundColor
        {
            get => BackgroundImage.color;
            set => BackgroundImage.color = value;
        }
    }
}
