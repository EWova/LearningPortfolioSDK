using UnityEngine;

namespace EWova.LearningPortfolio
{
    [CreateAssetMenu(fileName = "LearningPortfolioProfile", menuName = "EWova/LearningPortfolio/Profile")]
    public class LearningPortfolioProfile : ScriptableObject
    {
        public ApiSettings APISettings;
    }
}