using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace SarTracks.Website.Models
{
    public class User
    {
        public User()
            : base()
        {
            this.Roles = new List<RoleUserMembership>();
        }


        [Required]
        [Key]
        public string Username { get; set; }

        [Required]
        public string Name { get; set; }

        [ForeignKey("Member")]
        public Guid? MemberId { get; set; }

        public SarMember Member { get; set; }


        [Required]
        public string Password { get; set; }

        [Required]
        public string PasswordSalt { get; set; }

        [Required]
        public string Email { get; set; }

        public Guid? ValidationKey { get; set; }

        public UserState State { get; set; }

        public ICollection<RoleUserMembership> Roles { get; set; }
    }
}