/* Copyright 2011 Matt Cosand and others (see AUTHORS.TXT)
 *
 * This file is part of SARTracks.
 *
 *  SARTracks is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  SARTracks is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with SARTracks.  If not, see <http://www.gnu.org/licenses/>.
 */
namespace SarTracks.Tests.Website.Services
{
    using System;
    using System.Linq;
    using SarTracks.Website.Models;
    using SarTracks.Website.Services;
    using System.Collections.Generic;

#if MS_TEST
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using NUnit.Framework;
    using TestClassAttribute = NUnit.Framework.TestFixtureAttribute;
    using TestMethodAttribute = NUnit.Framework.TestCaseAttribute;
#endif

    public class AuthIdentityTestService : AuthIdentityService
    {
        public AuthIdentityTestService(string username, IDataStoreService store)
            : base(username, new DataStoreFactory(store))
        {
        }

        public List<Role> Test_GetUsersRolesRecursive(IDataStoreService ctx, string username, Dictionary<RoleKey, List<Role>> roleMemberLookup)
        {
            return this.GetUsersRolesRecursive(ctx, username, roleMemberLookup);
        }

        public Dictionary<RoleKey, List<Role>> Test_GetRoleMemberLookup(IDataStoreService store)
        {
            return this.GetRoleMemberLookup(store);
        }

        public List<Authorization> Test_GetUsersAuthentications(IDataStoreService store, string username, List<Role> usersRoles)
        {
            return this.GetUsersAuthentications(store, username, usersRoles);
        }
    }

    [TestClass]
    public class AuthIdentityTests 
    {
        [TestMethod]
        public void AuthIdentity_GetRoleMemberLookup()
        {
            TestStore store = ((TestStore)DevWebsiteDataInitializer.FillDefaultDevSet(new TestStore())).FixupReferences();
            User user = PopulateAuthData(store);

            AuthIdentityTestService service = new AuthIdentityTestService(user.Username, store);

            var lookup = service.Test_GetRoleMemberLookup(store);

            Assert.AreEqual(store.Roles.Count(), lookup.Count, "Lookup should have 1:1 mapping with existing roles");

            foreach (var entry in lookup)
            {
                var flatten = store.Roles.SingleOrDefault(f => entry.Key.Name == f.Name && entry.Key.OrgId == f.OrganizationId).Flatten().Distinct().ToList();
                Assert.AreEqual(flatten.Count, entry.Value.Count, "Should have same count when flattening {0} {1}", entry.Key.Name, entry.Key.OrgId);
                foreach (var role in flatten)
                {
                    Assert.IsNotNull(entry.Value.SingleOrDefault(f => f.Id == role.Id), "Couldn't find role {0} {1}", role.Name, role.OrganizationId);
                }
            }
        }

        [TestMethod]
        public void AuthIdentity_GetUsersRolesRecursive()
        {
            TestStore store = ((TestStore)DevWebsiteDataInitializer.FillDefaultDevSet(new TestStore())).FixupReferences();

            User user = PopulateAuthData(store);
            AuthIdentityTestService service = new AuthIdentityTestService(user.Username, store);

            var lookup = service.Test_GetRoleMemberLookup(store);

            var roles = service.Test_GetUsersRolesRecursive(store, user.Username, lookup);

            Assert.AreNotEqual(store.Roles.Count(), roles.Count, "Should have gotten all the roles");
            Assert.AreEqual(2, roles.Count, "Should have gotten 2 roles");
            Assert.IsNotNull(roles.SingleOrDefault(f => f.Name == "Parent"));
            Assert.IsNotNull(roles.SingleOrDefault(f => f.Name == "Child"));
        }

        private User PopulateAuthData(TestStore store)
        {
            User user = new User { Username = "test" };
            store.Users.Add(user);

            Role parent = new Role { Name = "Parent" };
            Role child = new Role { Name = "Child" };
            store.Roles.Add(parent);
            store.Roles.Add(child);
            RoleTests.MakeMember(parent, child);

            RoleUserMembership ru = new RoleUserMembership { User = user, Role = parent };
            parent.Users.Add(ru);
            user.Roles.Add(ru);

            Authorization auth = new Authorization { Permission = PermissionType.EditMember, Role = child, RoleId = child.Id };
            store.Authorization.Add(auth);

            return user;
        }

        [TestMethod]
        public void AuthIdentity_GetUsersAuthentications()
        {
            TestStore store = ((TestStore)DevWebsiteDataInitializer.FillDefaultDevSet(new TestStore())).FixupReferences();

            User user = PopulateAuthData(store);
            AuthIdentityTestService service = new AuthIdentityTestService(user.Username, store);

            var lookup = service.Test_GetRoleMemberLookup(store);
            var roles = service.Test_GetUsersRolesRecursive(store, user.Username, lookup);

            var authzs = service.Test_GetUsersAuthentications(store, user.Username, roles);
        }

        [TestMethod]
        public void AuthIdentity_HasPermission_SiteAdmin()
        {
            TestStore store = ((TestStore)DevWebsiteDataInitializer.FillDefaultDevSet(new TestStore())).FixupReferences();
            AuthIdentityTestService service = new AuthIdentityTestService("admin", store);

            foreach (var perm in Enum.GetValues(typeof(PermissionType)).Cast<PermissionType>())
            {
                Assert.IsTrue(service.HasPermission(perm, null));
                Assert.IsTrue(service.HasPermission(perm, Guid.NewGuid()));
            }
        }

        [TestMethod]
        public void AuthIdentity_HasPermission_UnknownUser()
        {
            TestStore store = ((TestStore)DevWebsiteDataInitializer.FillDefaultDevSet(new TestStore())).FixupReferences();
            AuthIdentityTestService service = new AuthIdentityTestService("not_a_user", store);
            User user = PopulateAuthData(store);

            foreach (var perm in Enum.GetValues(typeof(PermissionType)).Cast<PermissionType>())
            {
                Assert.IsFalse(service.HasPermission(perm, null));
                Assert.IsFalse(service.HasPermission(perm, Guid.NewGuid()));
            }
        }

        [TestMethod]
        public void AuthIdentity_HasPermission_1()
        {
            TestStore store = ((TestStore)DevWebsiteDataInitializer.FillDefaultDevSet(new TestStore())).FixupReferences();
            User user = PopulateAuthData(store);

            // Find a member and their organization
            SarMember member = store.Members.First(f => f.Memberships.Count > 0);
            var orgId = member.Memberships.First().OrganizationId;

            // Create a new role and put our test user in that role
            Role testRole = new Role { Name = "testrole" };
            RoleUserMembership ru = new RoleUserMembership { Role = testRole, User = user };
            testRole.Users.Add(ru);
            user.Roles.Add(ru);
            store.Roles.Add(testRole);

            var tmp = member.Memberships.Select(f => f.OrganizationId).ToArray();

            // Give the role permissions to edit members in org
            Authorization auth = new Authorization { Role = testRole, RoleId = testRole.Id, Permission = PermissionType.EditMember, Scope = orgId };            
            store.Authorization.Add(auth);

            AuthIdentityTestService service = new AuthIdentityTestService(user.Username, store);

            Assert.IsTrue(service.HasPermission(PermissionType.EditMember, member.Id));
        }
    }
}