using UnityEngine;
using UnityEngine.EventSystems;

namespace EWova.LearningPortfolio
{
    public class ProjectRecordShowerGraph : MonoBehaviour, IScrollHandler
    {
        public Vector2 SizeDelta
        {
            get => RectTransform.sizeDelta;
            set => RectTransform.sizeDelta = value;
        }
        public float Zoom => m_zoom;

        [SerializeField] private float m_zoom = 1f;
        [SerializeField] private float m_zoomSensitivity = 0.1f;
        [SerializeField] private float m_minZoom = 0.5f;
        [SerializeField] private float m_maxZoom = 5f;

        public RectTransform RectTransform;


        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
        }

        public void OnScroll(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localCursor);
            DoZoom(eventData.scrollDelta.y, localCursor / RectTransform.sizeDelta);
        }
        public void ResetZoom()
        {
            m_zoom = 1.0f;
            RectTransform.localScale = Vector3.one;
        }
        public void DoZoom(float value, Vector2? center = null)
        {
            if (Mathf.Abs(value) < 0.01f)
                return;

            Vector2 pos;
            if (center == null)
            {
                var frame = (RectTransform)RectTransform.parent;
                Vector2 localPoint = new Vector2(
                    Mathf.Lerp(frame.rect.xMin, frame.rect.xMax, 0.5f),
                    Mathf.Lerp(frame.rect.yMin, frame.rect.yMax, 0.5f)
                );
                var wPos = frame.TransformPoint(localPoint);
                var sPos = RectTransform.InverseTransformPoint(wPos);
                pos = sPos / RectTransform.sizeDelta;
            }
            else
            {
                pos = center.Value;
            }
            pos *= RectTransform.sizeDelta;

            Vector3 worldPosBefore = RectTransform.TransformPoint(pos);

            m_zoom *= Mathf.Pow(1f + m_zoomSensitivity, value);
            m_zoom = Mathf.Clamp(m_zoom, m_minZoom, m_maxZoom);
            RectTransform.localScale = Vector3.one * m_zoom;

            Vector3 worldPosAfter = RectTransform.TransformPoint(pos);

            RectTransform.position += worldPosBefore - worldPosAfter;
        }
    }
}
