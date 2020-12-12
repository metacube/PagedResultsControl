using System;
using JetBrains.Annotations;
using Novell.Directory.Ldap;

namespace PagedResultsControl
{
    public class IntegrationOptions
    {
        // ToDo: for testing reasons only. Should be removed for later usage.
        public static readonly IntegrationOptions DefaultIntegrationOptions = new IntegrationOptions
        (
            "192.168.1.208",
            LdapConnection.DefaultPort,
            "yulyankin@its",
            "123QWEasd",
            "CN=users, DC=its, DC=local",
            "(&(memberof=CN=ITS Developers,CN=Users,DC=its,DC=local))",
            LdapConnection.LdapV3,
            5,
            new[] {"sAMAccountName", "givenName", "sn", "initials", "department"}
        );

        public readonly string Host;
        public readonly int Port;
        public readonly string Login;
        public readonly string Password;
        public readonly string SearchBase;
        public readonly string Filter;
        public readonly int ProtocolVersion;
        public readonly int ResultPageSize;
        public readonly string[] TargetAttributes;

        public IntegrationOptions([NotNull] string host, int port, [NotNull] string login, [NotNull] string password, [NotNull] string searchBase,
            [NotNull] string filter, int protocolVersion, int resultPageSize, [NotNull] string[] targetAttribute)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Port = port;
            Login = login ?? throw new ArgumentNullException(nameof(login));
            Password = password ?? throw new ArgumentNullException(nameof(password));
            SearchBase = searchBase ?? throw new ArgumentNullException(nameof(searchBase));
            Filter = filter ?? throw new ArgumentNullException(nameof(filter));
            ProtocolVersion = protocolVersion;
            ResultPageSize = resultPageSize;
            TargetAttributes = targetAttribute ?? throw new ArgumentNullException(nameof(targetAttribute));
        }
    }
}