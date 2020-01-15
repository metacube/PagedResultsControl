# PagedResultsControl
LDAP Control Extension for Simple Paged Results Manipulation (RFC 2629) support for Novell.Directory.Ldap.NETStandard

Sometimes you need to retrieve large search result set (for example, 100 000 entries). 
This case you may face some troubles:
  - there are limits set on the LDAP server as when using Microsoft Active Directory and MaxPageSize is exceeded
  - low-bandwidth connection
  - the LDAP client has limited resources

The simple paged results control can be attached to a search operation to indicate that only a subset of the results should be returned. It may be used to iterate through the search results at a time.
It is similar to virtual list view control but does not require the result set to be sorted and provides only the ability to iterate sequentially through the result set.

Test application demonstrates how to use simple paged results control with https://github.com/dsbenghe/Novell.Directory.Ldap.NETStandard.
