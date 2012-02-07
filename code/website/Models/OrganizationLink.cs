using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace SarTracks.Website.Models
{
    public class OrganizationLink : SarObject
    {
        [ForeignKey("FromOrganization")]
        public Guid FromId { get; set; }

        public Organization FromOrganization { get; set; }

        [ForeignKey("ToOrganization")]
        public Guid ToId { get; set; }
        public Organization ToOrganization { get; set; }

        public OrgLinkType LinkType { get; set; }

        public static void Link(Organization from, Organization to, OrgLinkType linkType)
        {
            OrganizationLink link = new OrganizationLink { FromOrganization = from, ToOrganization = to, LinkType = linkType };
            from.LinksToOrgs.Add(link);
            to.LinksFromOrgs.Add(link);
        }
    }

    [Flags]
    public enum OrgLinkType
    {
        /// <summary>
        /// To org regularly works within From org's jurisdiction.
        /// From org has visibility into ToOrg
        /// </summary>
        Jurisdiction = 1,

        /// <summary>
        /// From org is gov't or other body that gives authorization for To org.
        /// To org may get worker numbers from From org, From org has visibility into ToOrg
        /// </summary>
        Authorization = 3,

        /// <summary>From organization issues worker numbers to To org.</summary>
        WorkerNumbers = 7,

        /// <summary>
        /// From org is an umbrella group organization member To orgs.
        /// </summary>
        Administrative = 8
    }
}