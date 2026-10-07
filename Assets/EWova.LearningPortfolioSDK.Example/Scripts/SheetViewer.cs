using EWova.LearningPortfolio;
using EWova.LearningPortfolio.BasicAssets;

using UnityEngine;

public class SheetViewer : MonoBehaviour
{
    public RectTransform Frame;

    private ProjectRecordShower window;
    [Button]
    public void Open()
    {
        if (window != null)
            Close();

        window = LearningPortfolio.CreateUserProjectSheetShower(Frame);
    }
    [Button]
    public void Close()
    {
        if (window == null)
            return;

        window.Close();
    }
}
