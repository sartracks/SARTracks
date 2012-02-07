using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using SarTracks.Website.Services;
using System.Runtime.Serialization;

namespace SarTracks.Website.Models
{
    [DataContract]
    public class Authorization : SarObject
    {
        public Authorization()
            : base()
        {
            this.IsSystem = false;
            //this.Users = new List<RoleUserMembership>();
            //this.Children = new List<Role>();
        }

        public bool IsSystem { get; set; }

        [ForeignKey("Role")]        
        public Guid? RoleId { get; set; }
        public Role Role { get; set; }

        [ForeignKey("User")]
        public string UserName { get; set; }
        public User User { get; set; }

        [DataMember]
        public PermissionType Permission { get; set; }

        [DataMember]
        [NotMapped]
        public string PermissionName
        {
            get { return this.Permission.ToString(); }
            protected set {  } 
        }

        [DataMember]
        public Guid? Scope { get; set; }
    }
    //    [Required]
    //    public string Name { get; set; }

    //    [ForeignKey("Organization")]
    //    public Guid? OrganizationId { get; set; }
    //    public Organization Organization { get; set; }

    //    public ICollection<RoleUserMembership> Users { get; set; }
        
    //    [InverseProperty("Parent")]
    //    public ICollection<Role> Children { get; set; }
        
    //    [InverseProperty("Children")]
    //    public Role Parent { get; set; }

    //    public bool SystemRole { get; set; }

    //    public IEnumerable<Role> Flatten()
    //    {
    //        yield return this;
    //        foreach (var child in Children)
    //        {
    //            foreach (var role in child.Flatten())
    //            {
    //                yield return role;
    //            }
    //        }
    //    }
    //}

    //public struct RoleKey
    //{
    //    public Guid? OrgId;
    //    public string Name;

    //    public RoleKey(string name, Guid? org)
    //    {
    //        this.Name = name;
    //        this.OrgId = org;
    //    }
    //}
}