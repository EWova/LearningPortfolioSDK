using EWova.Auth;

using System.Collections.Generic;

namespace EWova.LearningPortfolio
{
    public class LearningPortfolioEWovaAuth : AuthProvider
    {
        public static Logger InternalLogger = new Logger("[EWova]LPEWovaAuth ", LogLevel.Full);
        internal LearningPortfolioEWovaAuth()
            : base(EWovaAuthConfigFactory.Create(options =>
        {
            options.ClientId = "learning-portfolio-sdk";
            options.Scopes = new List<string> { "openid", "profile", "email", "roles", "organization", "offline_access" };
        }), InternalLogger)
        { }
    }
}
