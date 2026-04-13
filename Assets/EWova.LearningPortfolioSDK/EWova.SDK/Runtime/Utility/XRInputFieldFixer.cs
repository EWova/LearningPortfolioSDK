using System.Collections.Generic;

using TMPro;

using UnityEngine;

namespace EWova.XR
{
    /// <summary>
    /// 修復在 一體機 XR 啟動中若 TMP_InputField 的 Soft Keyboard 是使用中的，會導致 Caret 無法正確顯示
    /// </summary>
    public class XRInputFieldFixer : MonoBehaviour
    {
        private Dictionary<TMP_InputField, InputFieldData> inputFieldData = new Dictionary<TMP_InputField, InputFieldData>();
        struct InputFieldData
        {
            public bool OriginalShouldHideSoftKeyboard;
        }
        public bool ActivateFixOnStart = true;

        private void Start()
        {
            if (ActivateFixOnStart)
            {
                Setup();
            }
        }

        private void Setup()
        {
            bool isInAllInOne = UnityEngine.XR.XRSettings.isDeviceActive && Application.isMobilePlatform;

            var inputFields = GetComponentsInChildren<TMP_InputField>(true);

            foreach (var inputField in inputFields)
            {
                if (!inputFieldData.ContainsKey(inputField))
                {
                    inputFieldData.Add(inputField, new InputFieldData
                    {
                        OriginalShouldHideSoftKeyboard = inputField.shouldHideSoftKeyboard
                    });
                }

                if (isInAllInOne)
                {
                    // 一體機 XR 啟動中，強制隱藏軟鍵盤
                    inputField.shouldHideSoftKeyboard = true;
                }
                else
                {
                    inputField.shouldHideSoftKeyboard = inputFieldData[inputField].OriginalShouldHideSoftKeyboard;
                }
            }
        }
    }
}
