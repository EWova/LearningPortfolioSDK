using System.Collections;
using System.Collections.Generic;
using System.Linq;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace EWova.LearningPortfolio
{
    public class ProjectRecordShower : MonoBehaviour
    {
        public struct GraphContent
        {
            public Node Root;
            public struct Node
            {
                public Sprite Icon;
                public string LabelText;
                public string DescriptionText;
                public string CheckDateTimeText;
                public bool IsCompleteSelf;
                public bool IsComplete;
                public IReadOnlyList<Node> Children;
            }
        }
        public struct ChartContent
        {
            public struct Cell
            {
                public static implicit operator string(Cell c) => c.LabelText;
                public static implicit operator Cell(string labelText) => new Cell { IsReadOnly = false, LabelText = labelText };
                public static Cell ReadOnly(string labelText)
                {
                    return new Cell { IsReadOnly = true, LabelText = labelText };
                }
                public bool IsReadOnly;
                public string LabelText;
                public TextAlignmentOptions? OverrideAlignment;
            }
            public struct Column
            {
                public bool IsReadOnly;
                public string Label;
                public IReadOnlyList<Cell> Cells;

                public string CellsSummaryLabel;
            }
            public IReadOnlyList<Column> Columns;
        }
        public static ProjectRecordShower InstantiatePlane(RectTransform root)
        {
            ProjectRecordShower obj = Instantiate(Resources.Load<ProjectRecordShower>("EWova/LearningPortfolio/EWovaProjectRecordShower"));
            obj.transform.SetParent(root, false);
            return obj;
        }
        public void Close()
        {
            Destroy(gameObject);
        }

        public void MarkChartDirty() { IsChartDirty = true; }
        public void MarkGraphDirty() { IsGraphDirty = true; }
        private bool IsChartDirty;
        private bool IsGraphDirty;

        public readonly List<(ProjectRecordShowerPage Page, ProjectRecordShowerPageBody Body)> CurrentPages = new();

        public BinderButton CloseButton;
        public Image BackButtonImage;
        public TextMeshProUGUI BackButtonText;
        public bool DisableOriginCloseButtonBehaviour;
        [Space]
        public BinderButton SwitchChartBTN;
        public TextMeshProUGUI SwitchChartText;
        public GameObject SwitchChartPlane;
        public BinderButton SwitchProgressBTN;
        public TextMeshProUGUI SwitchProgressText;
        public GameObject SwitchProgressPlane;
        [Space]
        public ScrollRect GraphScrollRect;
        [Space]
        public ProjectRecordShowerGraphNode GraphNodeGO;
        public ProjectRecordShowerGraph GraphContentFrame;
        public RectTransform GraphLineGO;
        public RectTransform GraphNodeDes;
        public TextMeshProUGUI GraphNodeDesText;
        public Image GraphBGMask;
        public Button GraphRepositionBTN;
        public Button GraphZoomInBTN;
        public Button GraphZoomOutBTN;
        [Space]
        public ScrollRect PageScrollRect;
        public ScrollRect BodyScrollRect;
        [Space]
        public TextMeshProUGUI TitleTextTMP;
        public RectTransform PageFrame;
        public RectTransform PageViewPortFrame;
        public RectTransform BodyFrame;
        public TextMeshProUGUI Footer;
        public GameObject LoadingCover;
        public TextMeshProUGUI LoadingCoverTextTMP;
        [Space]
        public ProjectRecordShowerPage PageGO;
        public ProjectRecordShowerPageBody PageBodyGO;
        [Space]
        [SerializeField] private string m_titleText = "學習歷程";
        [SerializeField, TextArea] private string m_loadingCoverText = "資料儲存中\n請勿關閉軟體";
        [SerializeField] private string m_backText = "返回";
        [Space]
        [SerializeField] private bool m_showEmptyCell = true;
        [SerializeField] private bool m_showCellIndexColumn = true;
        [SerializeField] private int m_showCellIndexStart = 0;
        [SerializeField] private string m_showCellIndexColumnTitle = "序列";
        [Space]
        [SerializeField] private int m_pageFontSize = 36;
        [SerializeField] private int m_pageMinWidth = 140;
        [SerializeField] private int m_pageMinHeight = 60;
        [SerializeField] private int m_pagePaddingX = 20;
        [SerializeField] private int m_pagePaddingY = 20;
        [Space]
        [SerializeField] private int m_bodyFontSize = 32;
        [SerializeField] private int m_bodyMinWidth = 140;
        [SerializeField] private int m_bodyMinHeight = 60;
        [SerializeField] private int m_bodyPaddingX = 40;
        [SerializeField] private int m_bodyPaddingY = 40;
        [Space]
        [SerializeField] private Vector2 m_graphNodeSpacing = new(16, 10);
        [SerializeField] private int m_graphDepth = -1;
        [SerializeField] private Vector2 m_graphPadding = new(64, 64);
        [SerializeField] private bool m_graphBlackBG = false;
        [SerializeField] private float m_blackValueMax = 0.95f;
        [SerializeField] private float m_blackTime = 2f;
        [SerializeField] private float m_blackPower = 0.5f;

        [Space]
        public Color SecondaryNormalColor;
        public Color SecondaryHighlightedColor;
        public Color SecondaryDisabledColor;

        private bool m_viewingProgress = true;
        private float m_graphNodeDesSizeDeltaX;
#if !UNITY_6000_0_OR_NEWER
        private float m_graphNodeDesSizeDeltaY;
#endif
        private bool t_nextFrameUpdate;

        public bool IsShowCloseButton
        {
            get => CloseButton.gameObject.activeSelf;
            set => CloseButton.gameObject.SetActive(value);
        }
        public int PageFontSize
        {
            get => m_pageFontSize;
            set
            {
                MarkChartDirty();
                m_pageFontSize = value;
            }
        }
        public int PageMinWidth
        {
            get => m_pageMinWidth;
            set
            {
                MarkChartDirty();
                m_pageMinWidth = value;
            }
        }
        public string TitleText
        {
            get => m_titleText;
            set
            {
                MarkChartDirty();
                m_titleText = value;
            }
        }
        public string LoadingCoverText
        {
            get => m_loadingCoverText;
            set
            {
                MarkChartDirty();
                m_loadingCoverText = value;
            }
        }
        public string BackText
        {
            get => m_backText;
            set
            {
                MarkChartDirty();
                m_backText = value;
            }
        }
        public int PageMinHeight
        {
            get => m_pageMinHeight;
            set
            {
                MarkChartDirty();
                m_pageMinHeight = value;
            }
        }
        public int PagePaddingX
        {
            get => m_pagePaddingX;
            set
            {
                MarkChartDirty();
                m_pagePaddingX = value;
            }
        }
        public int PagePaddingY
        {
            get => m_pagePaddingY;
            set
            {
                MarkChartDirty();
                m_pagePaddingY = value;
            }
        }
        public int BodyFontSize
        {
            get => m_bodyFontSize;
            set
            {
                MarkChartDirty();
                m_bodyFontSize = value;
            }
        }
        public int BodyMinWidth
        {
            get => m_bodyMinWidth;
            set
            {
                MarkChartDirty();
                m_bodyMinWidth = value;
            }
        }
        public int BodyMinHeight
        {
            get => m_bodyMinHeight;
            set
            {
                MarkChartDirty();
                m_bodyMinHeight = value;
            }
        }
        public int BodyPaddingX
        {
            get => m_bodyPaddingX;
            set
            {
                MarkChartDirty();
                m_bodyPaddingX = value;
            }
        }
        public int BodyPaddingY
        {
            get => m_bodyPaddingY;
            set
            {
                MarkChartDirty();
                m_bodyPaddingY = value;
            }
        }
        public bool ShowEmptyCell
        {
            get => m_showEmptyCell;
            set
            {
                MarkChartDirty();
                m_showEmptyCell = value;
            }
        }
        public bool ShowRowIndex
        {
            get => m_showCellIndexColumn;
            set
            {
                MarkChartDirty();
                m_showCellIndexColumn = value;
            }
        }
        public string RowIndexTitle
        {
            get => m_showCellIndexColumnTitle;
            set
            {
                MarkChartDirty();
                m_showCellIndexColumnTitle = value;
            }
        }
        public bool ShowLoadingCover
        {
            get => LoadingCover.activeSelf;
            set
            {
                LoadingCover.SetActive(value);
                if (value)
                {
                    PageScrollRect.velocity = Vector2.zero;
                    BodyScrollRect.velocity = Vector2.zero;
                }
            }
        }

        public Vector2 GraphNodeSpacing
        {
            get => m_graphNodeSpacing;
            set
            {
                MarkGraphDirty();
                m_graphNodeSpacing = value;
            }
        }
        public int GraphDepth
        {
            get => m_graphDepth;
            set
            {
                MarkGraphDirty();
                m_graphDepth = value;
            }
        }
        public Vector2 GraphPadding
        {
            get => m_graphPadding;
            set
            {
                MarkGraphDirty();
                m_graphPadding = value;
            }
        }

        public bool IsViewingGraphProgress
        {
            get => m_viewingProgress;
            set
            {
                if (m_viewingProgress == value)
                    return;
                m_viewingProgress = value;
                t_blackValue = 0.0f;
            }
        }

        private void Awake()
        {
            PageGO.gameObject.SetActive(false);
            PageBodyGO.gameObject.SetActive(false);
            GraphNodeGO.gameObject.SetActive(false);
            GraphLineGO.gameObject.SetActive(false);
            GraphNodeDes.gameObject.SetActive(false);
            m_graphNodeDesSizeDeltaX = GraphNodeDes.sizeDelta.x;
#if !UNITY_6000_0_OR_NEWER
            m_graphNodeDesSizeDeltaY = GraphNodeDes.sizeDelta.y;
#endif
            GraphBGMask.color = new Color(0, 0, 0, t_blackValue);
            PageBodyGO.Init();
            LoadingCover.SetActive(false);

            SwitchChartBTN.onClick.AddListener(() =>
            {
                IsViewingGraphProgress = false;
            });
            SwitchProgressBTN.onClick.AddListener(() =>
            {
                IsViewingGraphProgress = true;
                RepositionGraphFrame();
            });
            GraphRepositionBTN.onClick.AddListener(() =>
            {
                GraphScrollRect.velocity = Vector2.zero;
                RepositionGraphFrame();
            });
            GraphZoomInBTN.onClick.AddListener(() =>
            {
                GraphContentFrame.DoZoom(10);
            });
            GraphZoomOutBTN.onClick.AddListener(() =>
            {
                GraphContentFrame.DoZoom(-10);
            });

            CloseButton.onClick.AddListener(() =>
            {
                if (DisableOriginCloseButtonBehaviour)
                    return;

                Close();
            });
        }
        private IEnumerator Start()
        {
            CloseButton.BindingState(BackButtonText
                , SecondaryNormalColor
                , SecondaryHighlightedColor
                , SecondaryNormalColor
                , SecondaryNormalColor
                , SecondaryDisabledColor);

            CloseButton.BindingState(BackButtonImage
                , SecondaryNormalColor
                , SecondaryHighlightedColor
                , SecondaryNormalColor
                , SecondaryNormalColor
                , SecondaryDisabledColor);

            SwitchChartBTN.BindingState(SwitchChartText
                , SecondaryNormalColor
                , SecondaryHighlightedColor
                , SecondaryNormalColor
                , SecondaryNormalColor
                , SecondaryDisabledColor);

            SwitchProgressBTN.BindingState(SwitchProgressText
                , SecondaryNormalColor
                , SecondaryHighlightedColor
                , SecondaryNormalColor
                , SecondaryNormalColor
                , SecondaryDisabledColor);

            yield return null;
            yield return null;
            IsViewingGraphProgress = true;
        }
        private float t_blackValue = 0.00f;
        private HashSet<GraphNodeInstance> t_notBlackNode = new();
        private void Update()
        {
            if (t_nextFrameUpdate)
            {
                t_nextFrameUpdate = false;
                NextFrameUpdateLayout();
            }
            if (IsViewingGraphProgress)
            {
                SwitchChartBTN.interactable = true;
                SwitchProgressBTN.interactable = false;
                SwitchChartPlane.SetActive(false);
                SwitchProgressPlane.SetActive(true);

                t_blackValue += (m_graphBlackBG ? Time.deltaTime : -Time.deltaTime) / m_blackTime;
                t_blackValue = Mathf.Clamp(t_blackValue, 0.0f, 1.2f);

                Color applyValue = new Color(0, 0, 0, Mathf.Clamp01(Mathf.Pow(t_blackValue, m_blackPower)) * m_blackValueMax);

                GraphBGMask.color = applyValue;
                foreach (var node in m_nodeMapping.Values)
                {
                    if (t_notBlackNode.Contains(node))
                    {
                        node.Object.Mask.color = Color.clear;
                        continue;
                    }
                    node.Object.Mask.color = applyValue;
                }

                if (m_hovingGraphNode != null)
                {
                    if (t_hovingGraphNode != m_hovingGraphNode)
                    {
                        t_notBlackNode.Clear();

                        t_hovingGraphNode = m_hovingGraphNode;

#if UNITY_6000_0_OR_NEWER
                        GraphNodeDesText.textWrappingMode = TextWrappingModes.Normal;
#else
                        GraphNodeDesText.enableWordWrapping = true;
#endif
                        GraphNodeDesText.text = t_hovingGraphNode.DescriptionText;
                        GraphNodeDes.sizeDelta = GraphNodeDesText.GetPreferredValues(GraphNodeDesText.text, m_graphNodeDesSizeDeltaX, 0);
#if !UNITY_6000_0_OR_NEWER
                        if (GraphNodeDes.sizeDelta.y > m_graphNodeDesSizeDeltaY)
                            GraphNodeDes.sizeDelta = new Vector2(m_graphNodeDesSizeDeltaX, GraphNodeDes.sizeDelta.y + GraphNodeDesText.margin.y + GraphNodeDesText.margin.w);
#endif
                        GraphNodeDes.gameObject.SetActive(true);
                        m_graphBlackBG = true;

                        t_notBlackNode.Add(m_nodeMapping[t_hovingGraphNode]);

                        foreach (var item in m_nodeMapping[t_hovingGraphNode].AllParents)
                            t_notBlackNode.Add(item);
                    }
                    GraphNodeDes.anchoredPosition = t_hovingGraphNode.RightPivot + new Vector2(-16, -8);
                }
                else
                {
                    if (t_hovingGraphNode != null)
                    {
                        GraphNodeDes.gameObject.SetActive(false);
                        m_graphBlackBG = false;
                    }
                    t_hovingGraphNode = null;
                }

                if (IsGraphDirty)
                {
                    IsGraphDirty = false;

                    GraphRebuildLayout();
                }
            }
            else
            {
                SwitchChartBTN.interactable = false;
                SwitchProgressBTN.interactable = true;
                SwitchChartPlane.SetActive(true);
                SwitchProgressPlane.SetActive(false);

                if (IsChartDirty)
                {
                    IsChartDirty = false;

                    TitleTextTMP.text = m_titleText;
                    LoadingCoverTextTMP.text = m_loadingCoverText;
                    BackButtonText.text = m_backText;

                    ChartRebuildLayout();
                }
            }

        }
        private void OnValidate()
        {
            MarkChartDirty();
            MarkGraphDirty();
        }

        public void SelectPage(int index)
        {
            if (index < 0 || index >= CurrentPages.Count)
                throw new System.ArgumentOutOfRangeException(nameof(index), "Index is out of range");

            BodyScrollRect.velocity = Vector2.zero;

            for (int i = 0; i < CurrentPages.Count; i++)
            {
                bool enable = i == index;
                CurrentPages[i].Page.IsSelected = enable;
                CurrentPages[i].Body.gameObject.SetActive(enable);
            }
        }
        public int AddPage(string pageLabel, ChartContent contents)
        {
            int currentIndex = CurrentPages.Count;

            ProjectRecordShowerPage newPage = Instantiate(PageGO, PageGO.transform.parent, false);
            newPage.gameObject.SetActive(true);

            ProjectRecordShowerPageBody newBody = Instantiate(PageBodyGO, PageBodyGO.transform.parent, false);
            newBody.Init();
            newPage.LabelText = pageLabel;
            newPage.OnClick = () => { SelectPage(currentIndex); };
            CurrentPages.Add((newPage, newBody));

            if (contents.Columns != null && contents.Columns.Count != 0)
            {
                int maxCellCount = contents.Columns
                    .Where(x => x.Cells?.Count > 0)
                    .Select(x => x.Cells.Count)
                    .DefaultIfEmpty(0)
                    .Max();

                List<ProjectRecordShowerCell> SummaryCells = new();
                bool hasSummary = false;
                foreach (ChartContent.Column column in contents.Columns)
                {
                    if (!hasSummary)
                        hasSummary = column.CellsSummaryLabel != null;

                    ProjectRecordShowerColumn newColumn = newBody.AddColumn();
                    newColumn.HeaderCell.LabelText = column.Label;

                    int cellCount = column.Cells == null || column.Cells.Count == 0 ? 0 : column.Cells.Count;
                    for (int i = 0; i < cellCount; i++)
                    {
                        ChartContent.Cell cell = column.Cells[i];
                        ProjectRecordShowerCell newCell = newColumn.AddCell(cell.IsReadOnly);
                        if (cell.OverrideAlignment != null)
                            newCell.LabelTextAlignment = cell.OverrideAlignment.Value;
                        newCell.LabelText = cell.LabelText;
                    }

                    int emptyCellCount = maxCellCount - cellCount;
                    if (emptyCellCount > 0)
                    {
                        for (int i = 0; i < emptyCellCount; i++)
                            newColumn.AddEmptyCell();
                    }

                    SummaryCells.Add(newColumn.SummaryCell);
                    newColumn.SummaryCell.LabelText = column.CellsSummaryLabel;
                }

                foreach (var summaryCells in SummaryCells)
                    summaryCells.gameObject.SetActive(hasSummary);

                newBody.IsShowCellIndexColumn = m_showCellIndexColumn;
                newBody.SetCellIndexCount(m_showCellIndexColumnTitle, m_showCellIndexStart, maxCellCount, hasSummary);

                if (currentIndex == 0)
                    SelectPage(0);
            }

            MarkChartDirty();
            return currentIndex;
        }
        public void Clear()
        {
            if (CurrentPages.Count > 0)
            {
                foreach (var (Page, Body) in CurrentPages)
                {
                    Destroy(Page.gameObject);
                    Destroy(Body.gameObject);
                }
                CurrentPages.Clear();
            }
            if (m_nodeMapping.Count > 0)
            {
                foreach (var node in m_nodeMapping.Keys)
                {
                    Destroy(node.gameObject);
                }
                m_nodeMapping.Clear();
                t_notBlackNode.Clear();
                m_resultGraph = null;
                m_hovingGraphNode = null;
                t_hovingGraphNode = null;
                GraphNodeDes.gameObject.SetActive(false);
            }
        }

        public class GraphNodeInstance
        {
            public GraphNodeInstance Root;
            public ProjectRecordShowerGraphNode Object;
            public IEnumerable<GraphNodeInstance> Nodes
            {
                get
                {
                    yield return this;
                    foreach (var child in Children)
                    {
                        foreach (var grandChild in child.Nodes)
                            yield return grandChild;
                    }
                }
            }
            public IEnumerable<GraphNodeInstance> AllParents
            {
                get
                {
                    var p = Root;
                    while (p != null)
                    {
                        yield return p;
                        p = p.Root;
                    }
                }
            }
            public List<GraphNodeInstance> Children = new();
        }
        private Dictionary<ProjectRecordShowerGraphNode, GraphNodeInstance> m_nodeMapping = new();
        private GraphNodeInstance m_resultGraph;
        private ProjectRecordShowerGraphNode m_hovingGraphNode;
        private ProjectRecordShowerGraphNode t_hovingGraphNode;

        public void SetGraph(GraphContent graphContent)
        {
            GraphNodeInstance CreateNode(GraphContent.Node dataNode, GraphNodeInstance parent)
            {
                ProjectRecordShowerGraphNode newNode = Instantiate(GraphNodeGO, GraphNodeGO.transform.parent, false);
                newNode.gameObject.SetActive(true);

                var nodeStruct = new GraphNodeInstance
                {
                    Object = newNode,
                    Root = parent,
                };
                nodeStruct.Object.LeftLine = Instantiate(GraphLineGO, GraphLineGO.transform.parent, false);
                nodeStruct.Object.UpLine = Instantiate(GraphLineGO, GraphLineGO.transform.parent, false);
                m_nodeMapping.Add(newNode, nodeStruct);
                nodeStruct.Object.Hover += (node) =>
                {
                    m_hovingGraphNode = node;
                };
                nodeStruct.Object.Click += (node) =>
                {
                    //m_hovingGraphNode = node;
                };

                newNode.Icon = dataNode.Icon;
                newNode.IsCompleteSelf = dataNode.IsCompleteSelf;
                newNode.IsComplete = dataNode.IsComplete;
                newNode.LabelText = dataNode.LabelText;
                newNode.DescriptionText = dataNode.DescriptionText;
                newNode.CheckDateTimeText = dataNode.CheckDateTimeText;

                if (dataNode.Children != null)
                {
                    foreach (var child in dataNode.Children)
                    {
                        var childStruct = CreateNode(child, nodeStruct);
                        nodeStruct.Children.Add(childStruct);
                    }
                }

                return nodeStruct;
            }

            m_resultGraph = CreateNode(graphContent.Root, null);

            GraphRebuildLayout();
        }
        private void RepositionGraphFrame()
        {
            GraphContentFrame.ResetZoom();
            GraphContentFrame.RectTransform.anchoredPosition = GraphContentFrame.Zoom * new Vector2(-m_graphPadding.x, m_graphPadding.y);
        }
        private void GraphRebuildLayout()
        {
            Vector2 elementSize = GraphNodeGO.RectTransform.sizeDelta;

            if (m_resultGraph == null)
                return;

            Rect frameRect = new Rect(0, 0, 0, 0);

            int Place(GraphNodeInstance node, int depth, int row)
            {
                if (m_graphDepth >= 0 && depth > m_graphDepth)
                {
                    node.Object.UpLine.gameObject.SetActive(false);
                    node.Object.LeftLine.gameObject.SetActive(false);
                    node.Object.gameObject.SetActive(false);
                    foreach (var child in node.Children)
                        Place(child, depth + 1, row);
                    return 0;
                }
                else
                {
                    node.Object.UpLine.gameObject.SetActive(true);
                    node.Object.LeftLine.gameObject.SetActive(true);
                    node.Object.gameObject.SetActive(true);
                }
                node.Object.Row = row;
                node.Object.Depth = depth;

                node.Object.Rect = new Rect(
                   m_graphPadding.x + depth * (elementSize.x + m_graphNodeSpacing.x),
                    -m_graphPadding.y + -row * (elementSize.y + m_graphNodeSpacing.y),
                    elementSize.x,
                    elementSize.y
                );
                node.Object.RightPivot = node.Object.Rect.position + new Vector2(node.Object.Rect.width, -node.Object.Rect.height / 2);
                node.Object.LeftPivot = node.Object.Rect.position + new Vector2(0, -node.Object.Rect.height / 2);
                node.Object.TopPivot = node.Object.Rect.position + new Vector2(node.Object.Rect.width / 2, 0);
                node.Object.BottomPivot = node.Object.Rect.position + new Vector2(node.Object.Rect.width / 2, -node.Object.Rect.height);


                node.Object.RectTransform.anchoredPosition = node.Object.Rect.position;
                if (frameRect.width == 0 && frameRect.height == 0)
                {
                    frameRect = node.Object.Rect;
                }
                else
                {
                    frameRect = Rect.MinMaxRect(
                        Mathf.Min(frameRect.xMin, node.Object.Rect.xMin),
                        Mathf.Min(frameRect.yMin, node.Object.Rect.yMin),
                        Mathf.Max(frameRect.xMax, node.Object.Rect.xMax),
                        Mathf.Max(frameRect.yMax, node.Object.Rect.yMax)
                    );
                }

                int usedRows = 1;

                if (node.Children.Count > 0)
                {
                    bool first = true;
                    foreach (var child in node.Children)
                    {
                        if (first && false)
                        {
                            usedRows = Mathf.Max(usedRows, Place(child, depth + 1, row));
                            first = false;
                        }
                        else
                        {
                            int childRow = row + usedRows;
                            usedRows += Place(child, depth + 1, childRow);
                        }
                    }
                }
                return usedRows;
            }

            Place(m_resultGraph, 0, 0);
            GraphContentFrame.SizeDelta = frameRect.size + m_graphPadding * 2;

            RepositionGraphFrame();

            //Line
            foreach (var node in m_resultGraph.Nodes)
            {
                node.Object.LeftLine.gameObject.SetActive(false);
                node.Object.UpLine.gameObject.SetActive(false);

                if (!node.Object.gameObject.activeSelf)
                    continue;
                if (node.Root == null) // IsTopRoot
                    continue;

                var rootFirstChild = node.Root.Children[0];
                bool isFirstChildSelf = rootFirstChild == node;


                node.Object.LeftLine.gameObject.SetActive(true);
                node.Object.UpLine.gameObject.SetActive(true);

                // C
                // |
                // B－A
                Vector2 xStart = node.Object.LeftPivot;
                Vector2 xEnd = new(node.Root.Object.BottomPivot.x, xStart.y);

                float xDis = Mathf.Abs(xEnd.x - xStart.x);

                node.Object.LeftLine.pivot = new Vector2(1.0f, 0.5f);
                node.Object.LeftLine.anchoredPosition = xStart;
                node.Object.LeftLine.sizeDelta = new Vector2(xDis, node.Object.LeftLine.sizeDelta.y);

                Vector2 yStart = xEnd;
                Vector2 yEnd = new(xEnd.x, node.Root.Object.BottomPivot.y);

                float yDis = Mathf.Abs(yEnd.y - yStart.y);

                node.Object.UpLine.pivot = new Vector2(0.5f, 0.0f);
                node.Object.UpLine.anchoredPosition = yStart;
                node.Object.UpLine.sizeDelta = new Vector2(node.Object.UpLine.sizeDelta.x, yDis);
            }
        }

        private void ChartRebuildLayout()
        {
            if (CurrentPages.Count <= 0)
                return;

            foreach (var (Page, Body) in CurrentPages)
            {
                Vector2 rect;
                Page.LabelTextSize = m_pageFontSize;
                rect = Page.Label.GetPreferredValues(Page.LabelText);
                Page.Width = Mathf.Max(rect.x, m_pageMinWidth);
                Page.Height = Mathf.Max(rect.y, m_pageMinHeight);
                Page.PaddingX = m_pagePaddingX;
                Page.PaddingY = m_pagePaddingY;

                foreach (ProjectRecordShowerColumn col in Body.Columns)
                {
                    col.HeaderCell.Label.fontSize = m_bodyFontSize;
                    foreach (ProjectRecordShowerCell Cell in col.ValueCells)
                        Cell.Label.fontSize = m_bodyFontSize;
                }

                float maxHeight = rect.y;
                //共用同一欄寬度
                foreach (ProjectRecordShowerColumn col in Body.Columns)
                {
                    rect = col.HeaderCell.Label.GetPreferredValues(col.HeaderCell.LabelText);
                    float maxWidthEachColumn = rect.x;

                    foreach (ProjectRecordShowerCell cell in col.ValueCells)
                    {
                        rect = cell.Label.GetPreferredValues(cell.LabelText);
                        if (rect.x > maxWidthEachColumn)
                            maxWidthEachColumn = rect.x;
                        if (rect.y > maxHeight)
                            maxHeight = rect.y;
                    }

                    foreach (ProjectRecordShowerCell cell in col.EmptyCells)
                    {
                        cell.gameObject.SetActive(m_showEmptyCell);
                    }

                    if (col.SummaryCell != null && col.SummaryCell.gameObject.activeSelf)
                    {
                        rect = col.SummaryCell.Label.GetPreferredValues(col.SummaryCell.LabelText);
                        if (rect.x > maxWidthEachColumn)
                            maxWidthEachColumn = rect.x;
                        if (rect.y > maxHeight)
                            maxHeight = rect.y;
                    }

                    col.CellsWidth = Mathf.Max(maxWidthEachColumn, m_bodyMinWidth);
                    col.CellsPaddingX = m_bodyPaddingX;
                }

                //全部共用高度
                foreach (ProjectRecordShowerColumn col in Body.Columns)
                {
                    col.CellsHeight = Mathf.Max(maxHeight, m_bodyMinHeight);
                    col.CellsPaddingY = m_bodyPaddingY;
                }
            }
            t_nextFrameUpdate = true;
        }
        private void NextFrameUpdateLayout()
        {
            PageFrame.sizeDelta = new Vector2(PageFrame.sizeDelta.x, PageViewPortFrame.sizeDelta.y);
            const int ForScrollBarMarginY = 30;
            BodyFrame.offsetMax = new Vector2(BodyFrame.offsetMax.x, -PageViewPortFrame.sizeDelta.y - ForScrollBarMarginY);
        }

        [ContextMenu("載入範本")]
        public void LoadTemplate()
        {
            var Columns = new List<ChartContent.Column>
            {
                new (){ CellsSummaryLabel = "總計", Label = "關卡", Cells = new List<ChartContent.Cell> { ChartContent.Cell.ReadOnly("第一關"), ChartContent.Cell.ReadOnly("第二關"), ChartContent.Cell.ReadOnly("第三關"), ChartContent.Cell.ReadOnly("第四關") } },
                new (){ Label = "遊玩時間", Cells = new List<ChartContent.Cell> { "120", "95", "150", "80" } },
                new (){ Label = "分數", Cells = new List<ChartContent.Cell> { "123", "456", "789", "321" } },
                new (){ Label = "評價", Cells = new List<ChartContent.Cell> { "xStart", "xEnd", "S", "yEnd" } },
                new (){ Label = "無Cell測試" }
            };
            AddPage("總覽", new() { Columns = Columns });

            AddPage("第一關", new()
            {
                Columns = new List<ChartContent.Column>
                {
                    new (){ Label = "項目", Cells = new List<ChartContent.Cell> { "敵人數", "寶箱", "隱藏路線" } },
                    new (){ Label = "數量", Cells = new List<ChartContent.Cell> { "10", "2", "1" } },
                    new (){ Label = "完成", Cells = new List<ChartContent.Cell> { "是", "否", "是" } }
                }
            });

            AddPage("第二關", new()
            {
                Columns = new List<ChartContent.Column>
                {
                    new (){ Label = "項目", Cells = new List<ChartContent.Cell> { "敵人數", "寶箱" } },
                    new (){ Label = "數量", Cells = new List<ChartContent.Cell> { "20", "3" } },
                    new (){ Label = "完成", Cells = new List<ChartContent.Cell> { "否", "是" } },
                    new (){ Label = "備註", Cells = new List<ChartContent.Cell> { "有隱藏BOSS" } }
                }
            });

            AddPage("第三關", new()
            {
                Columns = new List<ChartContent.Column>
                {
                    new (){ Label = "項目", Cells = new List<ChartContent.Cell> { "敵人數", "寶箱", "隱藏路線", "特殊事件" } },
                    new (){ Label = "數量", Cells = new List<ChartContent.Cell> { "15", "1", "0", "2" } },
                    new (){ Label = "完成", Cells = new List<ChartContent.Cell> { "是", "是", "否", "是" } }
                }
            });

            AddPage("第四關", new()
            {
                Columns = new List<ChartContent.Column>
                {
                    new (){ Label = "項目", Cells = new List<ChartContent.Cell> { "敵人數" } },
                    new (){ Label = "數量", Cells = new List<ChartContent.Cell> { "30" } },
                    new (){ Label = "完成", Cells = new List<ChartContent.Cell> { "否" } }
                }
            });
        }
    }
}