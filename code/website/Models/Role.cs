using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace SarTracks.Website.Models
{
    public class RoleUserMembership : SarObject
    {
        [Required]
        public User User { get; set; }

        [Required]
        public Role Role { get; set; }

        public bool IsSystem { get; set; }
    }

    [DataContract]
    public class RoleRoleMembership : SarObject
    {
        [Required]
        public Role Parent { get; set; }

        [Required]
        [DataMember]
        public Role Child { get; set; }

        public bool IsSystem { get; set; }
    }

    [DataContract]
    public class Role : SarObject
    {
        public Role() : base()
        {
            this.Users = new List<RoleUserMembership>();
            this.MemberRoles = new List<RoleRoleMembership>();
            this.MemberOfRoles = new List<RoleRoleMembership>();
        }

        [Required]
        [DataMember]
        public string Name { get; set; }

        [ForeignKey("Organization")]
        public Guid? OrganizationId { get; set; }
        [DataMember]
        public Organization Organization { get; set; }

        public ICollection<RoleUserMembership> Users { get; set; }
        
        [InverseProperty("Parent")]
        public ICollection<RoleRoleMembership> MemberRoles { get; set; }
        
        [InverseProperty("Child")]
        public ICollection<RoleRoleMembership> MemberOfRoles { get; set; }

        public bool SystemRole { get; set; }

        public IEnumerable<Role> Flatten()
        {
            yield return this;
            foreach (var container in MemberOfRoles.Select(f => f.Parent))
            {
                foreach (var role in container.Flatten())
                {
                    yield return role;
                }
            }
        }
    }

    public struct RoleKey
    {
        public Guid? OrgId;
        public string Name;

        public RoleKey(string name, Guid? org)
        {
            this.Name = name;
            this.OrgId = org;
        }
    }
}