using UnityEngine;

using EWova.LearningPortfolio;
using System.Linq;
using System;
using System.Collections.Generic;

public class Template : MonoBehaviour
{
    [Tooltip("EWova 主要登入邏輯介面")]
    public EWovaLoginPlane loginPlane;

    private void Awake()
    {
        // 你可以在這裡設定 EWova 的 Debug 等級，如果想要看到完整的除錯訊息，可以調整等級到 EWova.Debug.Level.Full
        LearningPortfolio.Debug.PrintLevel = EWova.Debug.Level.Error;
    }

    public void Start()
    {
        // 使用紀錄寫入裝置為 Unknown
        LearningPortfolio.UsingDeviceList UsingDevice = LearningPortfolio.UsingDeviceList.Unknown;
#if UNITY_EDITOR
        // 在 Unity 編輯器中使用 DeviceList.Editor 
        UsingDevice = LearningPortfolio.UsingDeviceList.Editor;
#endif

        // 當遊戲開始邏輯觸發 例如:登入後開始、跳過開始
        loginPlane.OnGameStart.AddListener(OnStart);

        // 你可以這樣子清除記憶資料
        //loginPlane.ClearAllSavedData();

        // 當然 你可以不透過UI介面登入，而是直接使用程式碼登入
        //loginPlane.Login("帳號", "密碼");

        // 提供了一些事件
        LearningPortfolio.OnUserLogin += (userData) =>
        {
            Debug.Log($"使用者登入 {userData}");
        };
        LearningPortfolio.OnUserLogout += () =>
        {
            Debug.Log("使用者登出");
        };
        LearningPortfolio.OnUserProjectRecordUpdated += (sheet) =>
        {
            // 請注意，若你有登入登出更改使用者，這個是檢表單可能是為不同使用者表單
            // 可以透過 sheet.Owner 來確認是誰的資料
            Debug.Log($"使用者資料更新 {sheet}, 使用者: {sheet.Owner}");
        };

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
