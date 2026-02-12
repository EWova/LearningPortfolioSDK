using System;

namespace EWova.NetService.Model
{
    [Serializable]
    public class ApiLoginModel
    {
        public string email;
        public string password;
    }
    [Serializable]
    public class ApiLoginToken
    {
        public string data;
    }

    public class UserProfile
    {
        public int id;
        public Guid guid;
        public string name;
        public string nickname;
        public UserRole auth_Group;
        public Guid schoolGuid;
        public string UserCultureInfo;
        public string AppLanguage;
        public string Flags;
    }
    public class OrganizationProfile
    {
        public Guid guid;
        public string name;
        public string qid;
        public string flags;
    }

    public enum UserRole
    {
        None = 0,
        OfficialDeveloper = 1,
        OrgAdmin = 2,
        OrgManager = 3,
        User = 4,
        OfficialManager = 5,
        LimitedUser = 6
    }
}
