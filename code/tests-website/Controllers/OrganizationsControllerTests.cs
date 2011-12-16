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
    using System.Linq;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Moq;
    using SarTracks.Website;
    using SarTracks.Website.Controllers;
    using SarTracks.Website.Models;
    using SarTracks.Website.Services;

#if MS_TEST
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using NUnit.Framework;
    using TestClassAttribute = NUnit.Framework.TestFixtureAttribute;
    using TestMethodAttribute = NUnit.Framework.TestCaseAttribute;
#endif

    public class TestPermissionsCache : PermissionsServiceCache
    {
        public void SetInstance(string username, IPermissionsService service)
        {
            if (PermissionsServiceCache.cache.ContainsKey(username))
            {
                PermissionsServiceCache.cache[username] = service;
            }
            else
            {
                PermissionsServiceCache.cache.Add(username, service);
            }
        }
    }

    [TestClass]
    public class OrganizationsControllerTests
    {
        [TestMethod]
        public void List()
        {
            OrganizationsControllerTests c = new OrganizationsControllerTests();
            RequestContext context = new RequestContext();
         
   
        }

        [TestMethod]
        public void Organization_GetEditForm()
        {
            // SETUP
            var controller = new OrganizationsController();

            TestStore store = ((TestStore)DevWebsiteDataInitializer.FillDefaultDevSet(new TestStore())).FixupReferences();

            Mock<PermissionsService> perms = new Mock<PermissionsService>();
            perms.Setup(x => x.IsUserInRole("testuser", "Administrators")).Returns(false);
            perms.Setup(x => x.Username).Returns("testuser");
            new TestPermissionsCache().SetInstance("testuser", perms.Object);

            DataStoreService.TestStore = store;

            // TEST
            Organization org = store.Organizations.First();
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
            // SETUP
            var controller = new OrganizationsController();
            var mocks = new ContextMocks(controller);
            mocks.Request.Setup(r => r.AcceptTypes).Returns(new[] { "application/json" });

            TestStore store = ((TestStore)DevWebsiteDataInitializer.FillDefaultDevSet(new TestStore())).FixupReferences();

            Mock<PermissionsService> perms = new Mock<PermissionsService>();
            perms.Setup(x => x.IsUserInRole("testuser", "Administrators")).Returns(true);
            perms.Setup(x => x.Username).Returns("testuser");
            new TestPermissionsCache().SetInstance("testuser", perms.Object);

            DataStoreService.TestStore = store;
            
            Organization org = store.Organizations.First();
            Assert.AreEqual(2, org.UnitStatusTypes.Count);

            // TEST
            UnitStatusType type = new UnitStatusType { Name = "Test Type", IsMissionQualified = false, IsActive = true };

            var dataResult = controller.SaveStatus(org.Id, type);
            var model = (SubmitResult<UnitStatusType>)dataResult.Data;

            // VERIFY
            Assert.AreEqual(0, model.Errors.Length);
            //Assert.AreEqual(robert.Id, model.Result.OrganizationId, "Org ID should have been populated");
            Assert.AreEqual(3, org.UnitStatusTypes.Count);

            UnitStatusType savedType = org.UnitStatusTypes.Single(f => f.Id == type.Id);
            Assert.AreEqual(type.Name, savedType.Name);
            Assert.AreEqual(type.IsActive, savedType.IsActive);
            Assert.AreEqual(type.IsMissionQualified, savedType.IsMissionQualified);
        }
    }
}
