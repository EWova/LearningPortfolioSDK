using UnityEngine;

namespace EWova
{
    public static class EWova
    {
        public const string NetServiceApi = "https://dash.ewova.com/api/";
        public const string QueryPrefix = "ewova";
        
        private const string DeepLink = "ewova://";

        public static void LaunchApp()
        {
            Application.OpenURL(GetDeepLink());
        }

        public static string GetDeepLink() 
        {
            return DeepLink;
        }
    }
}
