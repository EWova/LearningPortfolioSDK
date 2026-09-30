using EWova.LearningPortfolio;

using System;

using UnityEngine;

public class GameLogic : MonoBehaviour
{
    public EWovaLoginPlane loginPlane;

    private void Awake()
    {
        TryGetComponent(out loginPlane);
        loginPlane.OnGameStartWithUserData += LoginPlane_OnGameStartWithUserData;
    }

    private void LoginPlane_OnGameStartWithUserData(LearningPortfolio.UserData obj)
    {
        if (obj == null)
            Debug.Log("未登入，已訪客的身分開始遊戲");
        else
            Debug.Log($"已登入，已 {obj.Nickname} 的身分開始遊戲");
    }
}
