using System;
using JetBrains.Annotations;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Asn1;

namespace PagedResultsControl
{
    public class LdapPagedResultsControl: LdapControl
    {
        private const string RequestOid = "1.2.840.113556.1.4.319";
        private const string DecodedNotInteger = "Decoded value is not an integer, but should be";
        private const string DecodedNotOctetString = "Decoded value is not an octet string, but should be";

        private static readonly string DecodedNotSequence = $"Failed to construct {nameof(LdapPagedResultsControl)}: " +
                                                            $"provided values might not be decoded as {nameof(Asn1Sequence)}";
        private Asn1Sequence _request;
        
        static LdapPagedResultsControl()
        {
            try
            {
                Register(RequestOid, typeof(LdapPagedResultsControl));
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Failed to bind oid <{RequestOid}> to control <{nameof(LdapPagedResultsControl)}>", ex);
            }
        }

        public LdapPagedResultsControl(int size, [CanBeNull] byte[] cookie) : base(RequestOid, true, null)
        {
            Size = size;
            Cookie = cookie ?? GetEmptyCookie;
            BuildTypedPagedRequest();
            // ReSharper disable once VirtualMemberCallInConstructor
            SetValue(_request.GetEncoding(new LberEncoder()));
        }
        
        [CLSCompliant(false), UsedImplicitly]
        public LdapPagedResultsControl(string oid, bool critical, byte[] values) : base(oid, critical, values)
        {
            var lberDecoder = new LberDecoder();
            if (lberDecoder == null) throw new InvalidOperationException($"Failed to build {nameof(LberDecoder)}");
            
            var asn1Object = lberDecoder.Decode(values);
            if (!(asn1Object is Asn1Sequence)) throw new InvalidCastException(DecodedNotSequence);
            
            var size = ((Asn1Structured) asn1Object).get_Renamed(0);
            if (!(size is Asn1Integer integerSize)) throw new InvalidOperationException(DecodedNotInteger);
            Size = integerSize.IntValue();
            
            var cookie = ((Asn1Structured) asn1Object).get_Renamed(1);
            if (!(cookie is Asn1OctetString octetCookie)) throw new InvalidOperationException(DecodedNotOctetString);
            Cookie = octetCookie.ByteValue();
        }

        /// <summary>
        /// REQUEST: An LDAP client application that needs to control the rate at which
        /// results are returned MAY specify on the searchRequest a
        /// pagedResultsControl with size set to the desired page siz
        ///
        /// RESPONSE: Each time the server returns a set of results to the client when
        /// processing a search request containing the pagedResultsControl, the
        /// server includes the pagedResultsControl control in the
        /// searchResultDone message. In the control returned to the client, the
        /// size MAY be set to the server’s estimate of the total number of
        /// entries in the entire result set. Servers that cannot provide such an
        /// estimate MAY set this size to zero (0).
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// INITIAL REQUEST: empty cookie
        ///
        /// CONSEQUENT REQUEST: cookie from previous response
        ///
        /// RESPONSE: The cookie MUST be set to an
        /// empty value if there are no more entries to return (i.e., the page of
        /// search results returned was the last), or, if there are more entries
        /// to return, to an octet string of the server’s choosing,used to resume
        /// the search.
        /// </summary>
        public byte[] Cookie { get; }

        public bool IsEmptyCookie() => Cookie == null || Cookie.Length == 0;

        public static byte[] GetEmptyCookie => new byte[] {};

        private void BuildTypedPagedRequest()
        {
            _request = new Asn1Sequence(2);
            _request.Add(new Asn1Integer(Size));
            _request.Add(new Asn1OctetString(Cookie));
        }
    }
}