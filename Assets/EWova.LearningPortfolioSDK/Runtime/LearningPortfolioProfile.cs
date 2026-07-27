using UnityEngine;
using UnityEngine.Serialization;

namespace EWova.LearningPortfolio
{
    [CreateAssetMenu(fileName = "LearningPortfolioProfile", menuName = "EWova/LearningPortfolio/Profile")]
    public class LearningPortfolioProfile : ScriptableObject
    {
        [FormerlySerializedAs("APISettings")]
        public ProjectSettings ProjectSettings;
    }
}