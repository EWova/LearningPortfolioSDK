using Cysharp.Threading.Tasks;

using EWova.NetService.Model;

using System;
using System.Threading;

namespace EWova.NetService
{
    public partial class Client
    {
        public UniTask<UserProfile> GetUser(Guid guid, CancellationToken cancellationToken = default)
        {
            return Get<UserProfile>($"user/{guid}", cancellationToken: cancellationToken);
        }
        public UniTask<OrganizationProfile> GetOrganization(Guid guid, CancellationToken cancellationToken = default)
        {
            return Get<OrganizationProfile>($"school/{guid}", cancellationToken: cancellationToken);
        }
    }
}
