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
namespace SarTracks.Tests.Website.Controllers
{
    using System;
    using System.Linq;
    using System.Web.Mvc;
    using Moq;
    using SarTracks.Website;
    using SarTracks.Website.Controllers;
    using SarTracks.Website.Models;
    using SarTracks.Website.Services;
    using SarTracks.Website.ViewModels;

#if MS_TEST
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using NUnit.Framework;
    using TestClassAttribute = NUnit.Framework.TestFixtureAttribute;
    using TestMethodAttribute = NUnit.Framework.TestCaseAttribute;
#endif

    [TestClass]
    public class OrganizationsControllerTests
    {
        [TestMethod]
        public void Organization_CreateApproveDelete()
        {
            string orgName = "TestOrg";

            //var perms = new Mock<AuthIdentityService>();
            //perms.Setup(f => f.HasPermission(PermissionType.SiteAdmin, null)).Returns(true);
            User admin;
            var perms = new Mock<AuthIdentityService>();

            using (DataStoreService store = new DataStoreService("admin"))
            {
                admin = store.Users.Single(f => f.Username == "admin");
                perms.Setup(f => f.User).Returns(admin);
                perms.Setup(f => f.UserLogin).Returns(admin.Username);

                var org = store.Organizations.SingleOrDefault(f => f.Name == orgName);
                using (var controller = new OrganizationsController(perms.Object, new DataStoreFactory()))
                {
                    var mocks = new ContextMocks(controller);
                    mocks.Request.Setup(r => r.AcceptTypes).Returns(new[] { "application/json" });

                    if (org != null)
                    {
                        var removeResult = controller.DoDeleteOrganization(org.Id);
                    }
                }
            }


            using (var controller = new OrganizationsController(perms.Object, new DataStoreFactory()))
            {
                var mocks = new ContextMocks(controller);
                mocks.Request.Setup(r => r.AcceptTypes).Returns(new[] { "application/json" });

                var action = controller.Create();
                var view = (ViewResult)action;
                Assert.AreEqual(2, view.ViewData.Count, "ViewData should contain 2 items");
                Assert.IsTrue(view.ViewData.ContainsKey(OrganizationsController.VIEWDATA_LIST_TIMEZONES), "ViewData should have timezone select list");
                Assert.IsTrue(view.ViewData.ContainsKey(OrganizationsController.VIEWDATA_LIST_VISIBILITY), "ViewData should have visibility select list");

                action = controller.Create(new NewOrganizationViewModel { Org = new Organization { Name = orgName, LongName = "Long Test Org Name", TimeZone = "Pacific Standard Time" }, Visibility = "Users" });
                var redirect = (RedirectToRouteResult)action;
                Assert.AreEqual(2, redirect.RouteValues.Count);
                Assert.AreEqual(false, redirect.Permanent);
                Assert.AreEqual(string.Empty, redirect.RouteName);

                Guid newOrgId = Guid.Empty;
                using (var store = new DataStoreService(admin.Username))
                {
                    var newOrg = store.Organizations.SingleOrDefault(f => f.Name == orgName);
                    Assert.IsNotNull(newOrg, "New organization was not created");
                    newOrgId = newOrg.Id;

                    Assert.AreEqual(false, newOrg.IsApproved, "Org should start out as not approved");
                }

                controller.SetOrganizationApproved(newOrgId, true);
                using (var store = new DataStoreService(admin.Username))
                {
                    var newOrg = store.Organizations.SingleOrDefault(f => f.Name == orgName);
                    Assert.AreEqual(true, newOrg.IsApproved, "Org should now be approved");
                }

                controller.DoDeleteOrganization(newOrgId);
                using (var store = new DataStoreService(admin.Username))
                {
                    var newOrg = store.Organizations.SingleOrDefault(f => f.Name == orgName);
                    Assert.IsNull(newOrg, "New organization was not removed");
                }

            }
        }

        //[TestMethod]
        //public void List()
        //{
        //    OrganizationsControllerTests c = new OrganizationsControllerTests();
        //    RequestContext context = new RequestContext();
        //}

        [TestMethod]
        public void Organization_GetEditForm()
        {
            // SETUP
            TestStore store = ((TestStore)DevWebsiteDataInitializer.FillDefaultDevSet(new TestStore())).FixupReferences();
            Organization org = store.Organizations.First();

            var perms = new Mock<AuthIdentityService>();
            perms.Setup(f => f.HasPermission(PermissionType.AdminOrganization, org.Id)).Returns(true);

            var controller = new OrganizationsController(perms.Object, new DataStoreFactory(store));

            var mocks = new ContextMocks(controller);
            mocks.Request.Setup(r => r.Url).Returns(new Uri("https://test.local"));
            mocks.Request.Setup(r => r.AcceptTypes).Returns(new[] { "application/json" });

            // TEST
            UnitStatusType type = org.UnitStatusTypes.First();

            PartialViewResult result = (PartialViewResult)controller.EditStatus(org.Id, type.Id);

            // VERIFY
            TestAdapter.IsInstanceOf<UnitStatusType>(result.Model, "View Model should be a UnitStatusType");
            UnitStatusType model = (UnitStatusType)result.Model;

            Assert.AreEqual(type.Id, model.Id, "IDs should match");
            Assert.AreEqual(type.Name, model.Name);
            Assert.AreEqual(type.IsActive, model.IsActive);
            Assert.AreEqual(type.IsMissionQualified, model.IsMissionQualified);
            Assert.AreEqual(type.Organization.Id, model.Organization.Id);
        }

        [TestMethod]
        public void Basic_AddStatusType()
        {
            TestStore store = ((TestStore)DevWebsiteDataInitializer.FillDefaultDevSet(new TestStore())).FixupReferences();
            Organization org = store.Organizations.First();

            Mock<AuthIdentityService> perms = new Mock<AuthIdentityService>();
            perms.Setup(f => f.HasPermission(PermissionType.AdminOrganization, org.Id)).Returns(true);

            // SETUP
            var controller = new OrganizationsController(perms.Object, new DataStoreFactory(store));
            var mocks = new ContextMocks(controller);
            mocks.Request.Setup(r => r.AcceptTypes).Returns(new[] { "application/json" });

            int beginCount = org.UnitStatusTypes.Count;
            
            // TEST
            UnitStatusType type = new UnitStatusType { Name = "Test Type", IsMissionQualified = false, IsActive = true };

            var dataResult = controller.SaveStatus(org.Id, type);
            var model = (SubmitResult<UnitStatusType>)dataResult.Data;

            // VERIFY
            Assert.AreEqual(0, model.Errors.Length);
            //Assert.AreEqual(robert.Id, model.Result.OrganizationId, "Org ID should have been populated");
            Assert.AreEqual(beginCount + 1, org.UnitStatusTypes.Count);

            UnitStatusType savedType = org.UnitStatusTypes.Single(f => f.Id == type.Id);
            Assert.AreEqual(type.Name, savedType.Name);
            Assert.AreEqual(type.IsActive, savedType.IsActive);
            Assert.AreEqual(type.IsMissionQualified, savedType.IsMissionQualified);
        }
    }
}