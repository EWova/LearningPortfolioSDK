using EWova.Auth;
using EWova.DeepLink;

using System.Collections.Generic;

namespace EWova.LearningPortfolio
{
    public class LearningPortfolioEWovaAuth : AuthProvider
    {
        internal LearningPortfolioEWovaAuth()
            : base(EWovaAuthConfigFactory.Create(options =>
        {
            options.ClientId = "learning-portfolio-sdk";
            options.Scopes = new List<string> { "openid", "profile", "email", "roles", "organization", "offline_access" };
        }), deepLinkHandler: DeepLinkHandler.Default
        , logger: new Logger("[EWova]LPEWovaAuth ", LogLevel.Full))
        { }

        internal string ApiKey { get; set; } = null;

        public override string AppId => ProjectId;
        internal string ProjectId { get; set; }

        protected override void InternalOnAuthStateChanged(AuthState authState)
        {
            if (authState == AuthState.Unauthenticated)
                ProjectId = null;
        }
    }
}
