using System.IO;
using System.Xml;

using UnityEditor.Android;

using UnityEngine;

namespace EWova.DeepLink.Editor
{
    public class InjectDeepLinkPostProcessor : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 100;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError("AndroidManifest.xml not found: " + manifestPath);
                return;
            }

            var doc = new XmlDocument();
            doc.Load(manifestPath);

            XmlNamespaceManager nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("android", "http://schemas.android.com/apk/res/android");

            AddDeepLinkScheme(doc, Config.MyAppScheme);

            doc.Save(manifestPath);

            Debug.Log($"已完成將你的 DeepLink {Config.MyAppScheme}:// 加入到 AndroidManifest.xml");
        }

        public static void AddDeepLinkScheme(XmlDocument doc, string scheme)
        {
            if (string.IsNullOrWhiteSpace(scheme))
            {
                Debug.LogError("DeepLink scheme cannot be null or empty.");
                return;
            }

            XmlNode manifestNode = doc.SelectSingleNode("/manifest");
            XmlNode applicationNode = manifestNode.SelectSingleNode("application");
            XmlNode activityNode = applicationNode.SelectSingleNode("activity");

            XmlElement intentFilter = doc.CreateElement("intent-filter");

            XmlElement action = doc.CreateElement("action");
            action.SetAttribute("name", "http://schemas.android.com/apk/res/android", "android.intent.action.VIEW");
            intentFilter.AppendChild(action);

            XmlElement categoryDefault = doc.CreateElement("category");
            categoryDefault.SetAttribute("name", "http://schemas.android.com/apk/res/android", "android.intent.category.DEFAULT");
            intentFilter.AppendChild(categoryDefault);

            XmlElement categoryBrowsable = doc.CreateElement("category");
            categoryBrowsable.SetAttribute("name", "http://schemas.android.com/apk/res/android", "android.intent.category.BROWSABLE");
            intentFilter.AppendChild(categoryBrowsable);

            XmlElement data = doc.CreateElement("data");
            data.SetAttribute("scheme", "http://schemas.android.com/apk/res/android", scheme);
            intentFilter.AppendChild(data);

            activityNode.AppendChild(intentFilter);
        }
    }
}