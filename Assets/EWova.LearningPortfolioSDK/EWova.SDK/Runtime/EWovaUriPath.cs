using System;

namespace EWova.NetService
{
    public sealed class EWovaUriPath
    {
        public static class Query
        {
            /// <summary>
            /// 使用者 User GUID 
            /// </summary>
            public readonly static Parameter Token = Parameter.Static("token");
            /// <summary>
            /// 世界/課程 World GUID
            /// </summary>
            public readonly static Parameter WorldID = Parameter.Static("wid");
            /// <summary>
            /// 空間 Space GUID
            /// </summary>
            public readonly static Parameter SpaceID = Parameter.Static("sid");
        }

        public enum ParameterType
        {
            Static,
            Dynamic,
        }
        public readonly struct Parameter
        {
            public string Key { get; }
            public string Value => Type == ParameterType.Static ? _value : _getValueFunc().Trim();
            public readonly ParameterType Type;

            private readonly Func<string> _getValueFunc;
            private readonly string _value;
            private Parameter(string suffix, Func<string> getValueFunc)
            {
                Type = ParameterType.Dynamic;
                Key = (EWova.QueryPrefix + suffix).ToLower();
                _getValueFunc = getValueFunc;
                _value = null;
            }
            private Parameter(string suffix, string staticValue)
            {
                Type = ParameterType.Static;
                Key = (EWova.QueryPrefix + suffix).ToLower();
                _getValueFunc = null;
                _value = staticValue?.Trim();
            }
            public static Parameter Static(string key, string staticValue = null) => new(key, staticValue);
            public static Parameter Dynamic(string key, Func<string> getValueFunc) => new(key, getValueFunc);
        }

        private readonly UriBuilder uriBuilder;
        private readonly System.Collections.Specialized.NameValueCollection query;

        private EWovaUriPath(string baseString)
        {
            uriBuilder = new UriBuilder(baseString);
            query = HttpUtility.ParseQueryString(uriBuilder.Query);
        }
        public string this[Parameter param]
        {
            get
            {
                return query[param.Key];
            }
        }
        public static EWovaUriPath Parse(string baseString)
        {
            EWovaUriPath @obj = new(baseString);
            return @obj;
        }
        public EWovaUriPath AddOrSetQueue(Parameter param, string overrideValue = null)
        {

            string applyValue = overrideValue ?? param.Value;
            lock (query)
            {
                query[param.Key] = string.IsNullOrWhiteSpace(applyValue) ? null : applyValue;
            }
            return this;
        }
        public Uri GetResult()
        {
            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri;
        }
    }
}
