using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Novell.Directory.Ldap;

namespace PagedResultsControl
{
    public class PagedResultsControlHandler<T>
    {
        private readonly Func<LdapEntry, T> _converter;

        public PagedResultsControlHandler([NotNull] Func<LdapEntry, T> converter)
        {
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        }

        public List<T> LoadAllPagedResults([NotNull] IntegrationOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            using var ldapConnection = Connect(options);

            var searchResult = new List<T>();
            var isNextPageAvailable = PrepareForNextPage(ldapConnection, null, options.ResultPageSize, true);
            while (isNextPageAvailable)
            {
                var employeesPage = RetrievePage(ldapConnection, options, out var pageResponseControls);
                searchResult.AddRange(employeesPage);
                isNextPageAvailable = PrepareForNextPage(ldapConnection, pageResponseControls, options.ResultPageSize, false);
            }

            return searchResult;
        }

        private static LdapConnection Connect([NotNull] IntegrationOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var ldapConnection = new LdapConnection {SecureSocketLayer = false};
            ldapConnection.Connect(options.Host, options.Port);
            ldapConnection.Bind(options.ProtocolVersion, options.Login, options.Password);
            Debug.WriteLine($@"Connected to <{options.Host}:{options.Port}> as <{options.Login}>");
            return ldapConnection;
        }

        private static bool PrepareForNextPage([NotNull] LdapConnection ldapConnection, [CanBeNull] LdapControl[] pageResponseControls, int pageSize,
            bool isInitialCall)
        {
            if (ldapConnection == null) throw new ArgumentNullException(nameof(ldapConnection));
            if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

            var cookie = LdapPagedResultsControl.GetEmptyCookie;
            if (!isInitialCall)
            {
                var pagedResultsControl = (LdapPagedResultsControl) pageResponseControls?.FirstOrDefault(x => x is LdapPagedResultsControl);
                if (pagedResultsControl == null)
                {
                    Debug.WriteLine($"Failed to find <{nameof(LdapPagedResultsControl)}>. Searching is abruptly stopped");
                    return false;
                }

                // server signaled end of result set
                if (pagedResultsControl.IsEmptyCookie()) return false;
                cookie = pagedResultsControl.Cookie;
            }

            ApplyPagedResultsControl(ldapConnection, pageSize, cookie);
            return true;
        }
        
        private static void ApplyPagedResultsControl([NotNull] LdapConnection connection, int pageSize, [CanBeNull] sbyte[] cookie)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            var ldapPagedControl = new LdapPagedResultsControl(pageSize, cookie);
            var searchConstraints = connection.SearchConstraints;
            searchConstraints.BatchSize = 0;
            searchConstraints.setControls(ldapPagedControl);
            connection.Constraints = searchConstraints;
        }
        
        private List<T> RetrievePage([NotNull] LdapConnection ldapConnection, [NotNull] IntegrationOptions options, out LdapControl[] responseControls)
        {
            if (ldapConnection == null) throw new ArgumentNullException(nameof(ldapConnection));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var mappedPageResults = new List<T>();
            var searchResults = ldapConnection.Search
            (
                options.SearchBase,
                LdapConnection.SCOPE_SUB,
                options.Filter,
                options.TargetAttributes,
                false,
                (LdapSearchConstraints) null
            );

            while (searchResults.hasMore())
            {
                try
                {
                    var nextEntry = searchResults.next();
                    var mappedEntry = _converter.Invoke(nextEntry);
                    mappedPageResults.Add(mappedEntry);
                }
                catch (LdapException ex)
                {
                    // you may want to turn referral chasing on
                    if (ex is LdapReferralException) continue;
                    throw new InvalidOperationException("Failed to proceed to the next search result", ex);
                }
            }

            responseControls = searchResults.ResponseControls;
            return mappedPageResults;
        }
    }
}