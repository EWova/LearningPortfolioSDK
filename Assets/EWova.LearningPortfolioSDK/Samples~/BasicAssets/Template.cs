using UnityEngine;

using EWova.LearningPortfolio;

public class Template : MonoBehaviour
{
    [Tooltip("EWova 主要登入邏輯介面")]
    public EWovaLoginPlane loginPlane;

    private void Awake()
    {
        // 如果需要更完整的除錯資訊，可以將 Logger 的 PrintLevel 設定為 Full，這樣會輸出更多的日誌細節，有助於開發和除錯。
        LearningPortfolio.LoggerLevel = EWova.LogLevel.Full;
    }

    public void OnEnable()
    {
        loginPlane.OnGameStart.AddListener(OnStart);

        LearningPortfolio.OnUserLogin += HandleUserLogin;
        LearningPortfolio.OnUserLogout += HandleUserLogout;
        LearningPortfolio.OnUserProjectRecordUpdated += HandleUserProjectRecordUpdated;
    }

    public void OnDisable()
    {
        loginPlane.OnGameStart.RemoveListener(OnStart);

        LearningPortfolio.OnUserLogin -= HandleUserLogin;
        LearningPortfolio.OnUserLogout -= HandleUserLogout;
        LearningPortfolio.OnUserProjectRecordUpdated -= HandleUserProjectRecordUpdated;
    }


    private void HandleUserLogin(LearningPortfolio.UserData userData)
    {
        Debug.Log($"使用者登入 {userData}");
    }

    private void HandleUserLogout()
    {
        Debug.Log("使用者登出");
    }

    private void HandleUserProjectRecordUpdated(LearningPortfolio.UserProjectRecordSheet sheet)
    {
        Debug.Log($"使用者資料更新 {sheet}, 使用者: {sheet.Owner}");
    }

    // 這裡是遊戲開始的邏輯
    // isLogin TRUE/FALSE 代表是否有使用者登入
    private void OnStart(bool isLogin)
    {
        if (isLogin)
        {
            // 如果有登入 使用者資料會在 LearningPortfolio.Instance.LoginUserData 中
            LearningPortfolio.UserData loginUserData = LearningPortfolio.LoginUserData;

            Debug.Log($"登入開始 使用者身分:{LearningPortfolio.LoginUserData}");
        }
        else
        {
            Debug.Log("不登入開始");
        }

        // 這裡可以載入你的遊戲場景
        // 目前已 YourGame 為例子，請到 YourGame.cs 中參考後續處理
        UnityEngine.SceneManagement.SceneManager.LoadScene("YourGame");
    }
}
