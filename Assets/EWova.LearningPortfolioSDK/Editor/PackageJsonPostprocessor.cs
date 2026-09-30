using UnityEditor;
namespace EWova.LearningPortfolio.Editor
{
    public class PackageJsonPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                if (path.EndsWith("package.json"))
                {
                    PkgVerGen.Generate();
                    return;
                }
            }
        }
    }
}
