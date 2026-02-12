using System;
using System.Collections.Generic;

using UnityEngine;
namespace EWova.LearningPortfolio
{
    public class ProjectRecordShowerColumn : MonoBehaviour
    {
        [SerializeField] private ProjectRecordShowerCell HeaderCellGO;
        [SerializeField] private ProjectRecordShowerCell ReadOnlyCellGO;
        [SerializeField] private ProjectRecordShowerCell CellGO;
        [SerializeField] private ProjectRecordShowerCell EmptyCellGO;
        [SerializeField] private ProjectRecordShowerCell SummaryCellGO;

        [NonSerialized] public ProjectRecordShowerCell HeaderCell;
        [NonSerialized] public readonly List<ProjectRecordShowerCell> ValueCells = new();
        [NonSerialized] public readonly List<ProjectRecordShowerCell> EmptyCells = new();
        [NonSerialized] public ProjectRecordShowerCell SummaryCell;

        public void Init()
        {
            HeaderCellGO.gameObject.SetActive(false);
            ReadOnlyCellGO.gameObject.SetActive(false);
            CellGO.gameObject.SetActive(false);
            EmptyCellGO.gameObject.SetActive(false);

            HeaderCellGO.LabelText = string.Empty;

            if (SummaryCellGO != null)
            {
                SummaryCellGO.gameObject.SetActive(false);
                SummaryCellGO.LabelText = string.Empty;
            }

            ResetCell();
        }

        public ProjectRecordShowerCell AddCell(bool isReadOnly = false)
        {
            var cellPrefab = isReadOnly ? ReadOnlyCellGO : CellGO;
            ProjectRecordShowerCell newCell = Instantiate(cellPrefab, cellPrefab.transform.parent, false);
            newCell.Init();
            newCell.gameObject.SetActive(true);

            newCell.Width = HeaderCell.Width;
            ValueCells.Add(newCell);
            if (SummaryCell != null)
                SummaryCell.transform.SetAsLastSibling();
            return newCell;
        }

        public ProjectRecordShowerCell AddEmptyCell()
        {
            ProjectRecordShowerCell newCell = Instantiate(EmptyCellGO, EmptyCellGO.transform.parent, false);
            newCell.Init();
            newCell.gameObject.SetActive(true);

            newCell.Width = HeaderCell.Width;
            EmptyCells.Add(newCell);
            if (SummaryCell != null)
                SummaryCell.transform.SetAsLastSibling();
            return newCell;
        }

        public void ResetCell()
        {
            if (HeaderCell != null)
                Destroy(HeaderCell.gameObject);

            HeaderCell = Instantiate(HeaderCellGO, HeaderCellGO.transform.parent, false);
            HeaderCell.gameObject.SetActive(true);

            if (ValueCells.Count > 0)
            {
                foreach (var item in ValueCells)
                    Destroy(item.gameObject);
                ValueCells.Clear();
            }

            if (EmptyCells.Count > 0)
            {
                foreach (var item in EmptyCells)
                    Destroy(item.gameObject);
                EmptyCells.Clear();
            }

            if (SummaryCellGO != null)
            {
                if (SummaryCell != null)
                    Destroy(SummaryCell.gameObject);

                SummaryCell = Instantiate(SummaryCellGO, SummaryCellGO.transform.parent, false);
            }
            if (SummaryCell != null)
                SummaryCell.transform.SetAsLastSibling();
        }

        public float CellsPaddingX
        {
            get
            {
                if (Application.isPlaying)
                {
                    if (HeaderCell)
                        return HeaderCell.PaddingX;
                    return -1;
                }
                else
                {
                    return HeaderCellGO.PaddingX;
                }
            }
            set
            {
                if (Application.isPlaying)
                {
                    HeaderCell.PaddingX = value;
                    foreach (var item in ValueCells)
                        item.PaddingX = value;
                    foreach (var item in EmptyCells)
                        item.PaddingX = value;
                    if (SummaryCell != null)
                        SummaryCell.PaddingX = value;
                }
                else
                {
                    HeaderCellGO.PaddingX = value;
                    ReadOnlyCellGO.PaddingX = value;
                    EmptyCellGO.PaddingX = value;
                    if (SummaryCellGO != null)
                        SummaryCellGO.PaddingX = value;
                }
            }
        }
        public float CellsPaddingY
        {
            get
            {
                if (Application.isPlaying)
                {
                    if (HeaderCell)
                        return HeaderCell.PaddingY;
                    return -1;
                }
                else
                {
                    return HeaderCellGO.PaddingY;
                }
            }
            set
            {
                if (Application.isPlaying)
                {
                    HeaderCell.PaddingY = value;
                    foreach (var item in ValueCells)
                        item.PaddingY = value;
                    foreach (var item in EmptyCells)
                        item.PaddingY = value;
                    if (SummaryCell != null)
                        SummaryCell.PaddingY = value;
                }
                else
                {
                    HeaderCellGO.PaddingY = value;
                    ReadOnlyCellGO.PaddingY = value;
                    EmptyCellGO.PaddingY = value;
                    if (SummaryCellGO != null)
                        SummaryCellGO.PaddingY = value;
                }
            }
        }
        public float CellsWidth
        {
            get
            {
                if (Application.isPlaying)
                {
                    if (HeaderCell)
                        return HeaderCell.Width;
                    return -1;
                }
                else
                {
                    return HeaderCellGO.Width;
                }
            }
            set
            {
                if (Application.isPlaying)
                {
                    HeaderCell.Width = value;
                    foreach (var item in ValueCells)
                        item.Width = value;
                    foreach (var item in EmptyCells)
                        item.Width = value;
                    if (SummaryCell != null)
                        SummaryCell.Width = value;
                }
                else
                {
                    HeaderCellGO.Width = value;
                    ReadOnlyCellGO.Width = value;
                    EmptyCellGO.Width = value;
                    if (SummaryCellGO != null)
                        SummaryCellGO.Width = value;
                }
            }
        }
        public float CellsHeight
        {
            get
            {
                if (Application.isPlaying)
                {
                    if (HeaderCell)
                        return HeaderCell.Height;
                    return -1;
                }
                else
                {
                    return HeaderCellGO.Height;
                }
            }
            set
            {
                if (Application.isPlaying)
                {
                    HeaderCell.Height = value;
                    foreach (var item in ValueCells)
                        item.Height = value;
                    foreach (var item in EmptyCells)
                        item.Height = value;
                    if (SummaryCell != null)
                        SummaryCell.Height = value;
                }
                else
                {
                    HeaderCellGO.Height = value;
                    ReadOnlyCellGO.Height = value;
                    EmptyCellGO.Height = value;
                    if (SummaryCellGO != null)
                        SummaryCellGO.Height = value;
                }
            }
        }
    }
}