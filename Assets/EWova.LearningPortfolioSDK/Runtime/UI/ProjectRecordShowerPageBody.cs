using System;
using System.Collections.Generic;

using UnityEngine;
namespace EWova.LearningPortfolio
{
    public class ProjectRecordShowerPageBody : MonoBehaviour
    {
        [SerializeField] private ProjectRecordShowerColumn CellIndexColumnGO;
        [SerializeField] private ProjectRecordShowerColumn ColumnGO;

        [NonSerialized] public readonly List<ProjectRecordShowerColumn> Columns = new();

        public ProjectRecordShowerColumn CellIndexColumn;

        public bool IsShowCellIndexColumn
        {
            get => CellIndexColumn != null && CellIndexColumn.gameObject.activeSelf;
            set
            {
                if (CellIndexColumn != null)
                    CellIndexColumn.gameObject.SetActive(value);
            }
        }
        public string CellIndexColumnTitle
        {
            get => CellIndexColumn != null ? CellIndexColumn.HeaderCell.LabelText : string.Empty;
            set
            {
                if (CellIndexColumn != null)
                    CellIndexColumn.HeaderCell.LabelText = value;
            }
        }
        public void SetCellIndexCount(string headerLabel, int start, int count, bool hasSummary)
        {
            if (hasSummary)
                count += 1; // Summary cell

            CellIndexColumn.ResetCell();
            CellIndexColumn.HeaderCell.LabelText = headerLabel;

            for (int i = 0; i < count; i++)
            {
                var cell = CellIndexColumn.AddCell(isReadOnly: true);
                cell.LabelText = (start + i).ToString();
                if (i == count - 1)
                    cell.LabelText = string.Empty;
            }
        }

        public void Init()
        {
            ColumnGO.gameObject.SetActive(false);
            CellIndexColumnGO.gameObject.SetActive(false);

            CellIndexColumn = Instantiate(CellIndexColumnGO, CellIndexColumnGO.transform.parent, false);
            CellIndexColumn.Init();
            Columns.Add(CellIndexColumn);
        }
        public ProjectRecordShowerColumn AddColumn()
        {
            ProjectRecordShowerColumn newObj = Instantiate(ColumnGO, ColumnGO.transform.parent, false);
            newObj.Init();
            newObj.gameObject.SetActive(true);

            Columns.Add(newObj);
            return newObj;
        }
    }
}
