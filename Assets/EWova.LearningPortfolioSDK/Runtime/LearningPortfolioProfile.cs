using UnityEngine;

using static EWova.LearningPortfolio.LearningPortfolio;

namespace EWova.LearningPortfolio
{
    [CreateAssetMenu(fileName = "LearningPortfolioProfile", menuName = "EWova/LearningPortfolio/Profile")]
    public class LearningPortfolioProfile : ScriptableObject
    {
        public APISettings APISettings;
    }
}