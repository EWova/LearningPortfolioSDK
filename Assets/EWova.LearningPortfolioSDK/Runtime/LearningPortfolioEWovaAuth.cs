using EWova.Auth;

using System.Collections.Generic;

using UnityEngine;

namespace EWova.LearningPortfolio
{
    public class LearningPortfolioEWovaAuth : AuthProvider
    {
        internal LearningPortfolioEWovaAuth()
            : base(EWovaAuthConfigFactory.Create(options =>
        {
            options.ClientId = "learning-portfolio-sdk";
            options.Scopes = new List<string> { "openid", "profile", "email", "roles", "organization", "offline_access" };
        }), new Logger("[EWova]LPEWovaAuth ", LogLevel.Full))
        { }

        internal string ApiKey { get; set; } = null;
    }
}
