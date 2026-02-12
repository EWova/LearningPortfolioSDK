using System;

using TMPro;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EWova.LearningPortfolio
{
    public class ProjectRecordShowerGraphNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Runtime")]
        public Rect Rect;
        public Vector2 RightPivot;
        public Vector2 LeftPivot;
        public Vector2 TopPivot;
        public Vector2 BottomPivot;
        public string DescriptionText;
        public RectTransform LeftLine;
        public RectTransform UpLine;

        public int Depth;
        public int Row;

        public bool IsCompleteSelf
        {
            get => m_isCompleteSelf;
            set
            {
                m_isCompleteSelf = value;
                t_dirty = true;
            }
        }
        public bool IsComplete
        {
            get => m_isComplete;
            set
            {
                m_isComplete = value;
                t_dirty = true;
            }
        }
        public string LabelText
        {
            get => m_labelText;
            set
            {
                m_labelText = value;
                t_dirty = true;
            }
        }

        public Sprite Icon
        {
            get => m_icon;
            set
            {
                m_icon = value;
                t_dirty = true;
            }
        }
        public string CheckDateTimeText
        {
            get => m_subLabelText;
            set
            {
                m_subLabelText = value;
                t_dirty = true;
            }
        }

        [SerializeField] private bool m_isCompleteSelf;
        [SerializeField] private bool m_isComplete;
        [SerializeField] private string m_labelText;
        [SerializeField] private string m_subLabelText;
        [SerializeField] private Sprite m_icon;

        private bool t_dirty = false;
        private void OnValidate()
        {
            RectTransform = GetComponent<RectTransform>();
            Rebuild();
        }
        private void Awake()
        {
            Mask.color = Color.clear;
        }
        private void Update()
        {
            if (t_dirty)
            {
                t_dirty = false;
                Rebuild();
            }

        }

        private void Rebuild()
        {
            CheckMark.SetActive(m_isCompleteSelf);
            if (m_isComplete || m_isCompleteSelf)
            {
                Image1.color = CompleteColor1;
                Image2.color = CompleteColor2;
                Label.color = CompleteTextColor;
                SubLabel.color = CompleteTextColor;
            }
            else
            {
                Image1.color = NotCompleteColor1;
                Image2.color = NotCompleteColor2;
                Label.color = NotCompleteTextColor;
                SubLabel.color = NotCompleteTextColor;
            }
            CheckMarkIcon.color = CompleteColor2;
            IconImage.sprite = m_icon;
            Label.text = m_labelText;
            SubLabel.text = m_subLabelText;
        }

        public Action<ProjectRecordShowerGraphNode> Hover;
        public Action<ProjectRecordShowerGraphNode> Click;
        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            Hover?.Invoke(this);
        }
        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            Hover?.Invoke(null);
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            Click?.Invoke(this);
        }

        [Space]
        public RectTransform RectTransform;
        public Image Image1;
        public Image Image2;
        public GameObject CheckMark;
        public Image CheckMarkIcon;
        public Image IconImage;
        public TextMeshProUGUI Label;
        public TextMeshProUGUI SubLabel;
        public TextMeshProUGUI Description;
        public Image Mask;

        public Color NotCompleteColor1;
        public Color NotCompleteColor2;
        public Color CompleteColor1;
        public Color CompleteColor2;
        public Color NotCompleteTextColor;
        public Color CompleteTextColor;
    }
}