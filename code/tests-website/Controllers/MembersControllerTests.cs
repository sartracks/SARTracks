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
    public class MembersControllerTests
    {
        [TestMethod]
        public void Designators()
        {
            TestStore store = ((TestStore)DevWebsiteDataInitializer.FillDefaultDevSet(new TestStore())).FixupReferences();
            DataStoreService.TestStore = store;

            var controller = new MembersController();
            var mocks = new ContextMocks(controller);
            mocks.Request.Setup(r => r.AcceptTypes).Returns(new[] {"application/json"});

            var dataResult = controller.GetDesignators(store.Members.Single(f => f.FirstName == "Robert").Id, null);
            var model = (DesignatorsViewModel[])dataResult.Data;

            Assert.AreEqual(1, model.Length, "Wrong number of designators returned");
            Assert.AreEqual("1234", model[0].Designator, "Wrong designator returned");
            Assert.AreEqual(2, model[0].ForUnits.Length, "Wrong number of units using designator");
        }

        [TestMethod]
        public void MemberDetails_PersonalInfoFiltered()
        {
            // SETUP
            var controller = new MembersController();
            
            TestStore store = ((TestStore)DevWebsiteDataInitializer.FillDefaultDevSet(new TestStore())).FixupReferences();
            
            Mock<PermissionsService> perms = new Mock<PermissionsService>();
            perms.Setup(x => x.IsUserInRole("testuser", "Administrators")).Returns(false);
            perms.Setup(x => x.Username).Returns("testuser");
            new TestPermissionsCache().SetInstance("testuser", perms.Object);

     //       var mocks = new ContextMocks(controller);
     //       mocks.Request.Setup(r => r.RequestContext.HttpContext.User).Returns(new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity("testuser"), new[] { "blahrole" }));

            DataStoreService.TestStore = store;

            Guid memberid = store.Members.Single(f => f.FirstName == "Robert").Id;
            if (store.Members.Single(f => f.Id == memberid).BirthDate == null) Assert.Inconclusive("Can't test filtering birthdate if data store contains null");

            // TEST

            ViewResult result = (ViewResult)controller.Detail(memberid);

            // VERIFY
            TestAdapter.IsInstanceOf<SarMember>(result.Model, "View Model should be a SarMember");
            SarMember model = (SarMember)result.Model;

            Assert.AreEqual(memberid, model.Id, "IDs should match");
            Assert.IsNull(model.BirthDate, "Birthdate from view model should be empty");
            Assert.IsFalse(string.IsNullOrEmpty(model.FirstName), "First name should not be filtered");
        }

        [TestMethod]
        public void Basic_AddAddressTest()
        {
            // SETUP
            var controller = new MembersController();
            var mocks = new ContextMocks(controller);
            mocks.Request.Setup(r => r.AcceptTypes).Returns(new[] { "application/json" });

            TestStore store = ((TestStore)DevWebsiteDataInitializer.FillDefaultDevSet(new TestStore())).FixupReferences();

            Mock<PermissionsService> perms = new Mock<PermissionsService>();
            perms.Setup(x => x.IsUserInRole("testuser", "Administrators")).Returns(true);
            perms.Setup(x => x.Username).Returns("testuser");
            new TestPermissionsCache().SetInstance("testuser", perms.Object);

            DataStoreService.TestStore = store;

            SarMember robert = store.Members.Single(f => f.FirstName == "Robert");            
            Assert.AreEqual(2, robert.Addresses.Count);

            // TEST
            MemberAddress addr = new MemberAddress { Address = new Address { Street = "test street", City = "somewhere", State = "CA", Zip = "12345" }, Type = 0 };

            var dataResult = controller.SaveAddress(robert.Id, addr);
            var model = (SubmitResult<MemberAddress>)dataResult.Data;

            // VERIFY
            Assert.AreEqual(0, model.Errors.Length);
            Assert.AreEqual(robert.Id, model.Result.MemberId, "Member ID should have been populated");
            Assert.AreEqual(3, robert.Addresses.Count);

            MemberAddress savedAddr = robert.Addresses.Single(f => f.Id == addr.Id);
            Assert.AreEqual(addr.Address.State, savedAddr.Address.State);
            Assert.AreEqual(addr.Address.Street, savedAddr.Address.Street);
            Assert.AreEqual(addr.Address.City, savedAddr.Address.City);
            Assert.AreEqual(addr.Address.Zip, savedAddr.Address.Zip);
            Assert.AreEqual(addr.Type, savedAddr.Type);
        }

        [TestMethod]
        public void Basic_AddContactTest()
        {
            // SETUP
            var controller = new MembersController();
            var mocks = new ContextMocks(controller);
            mocks.Request.Setup(r => r.AcceptTypes).Returns(new[] { "application/json" });

            TestStore store = ((TestStore)DevWebsiteDataInitializer.FillDefaultDevSet(new TestStore())).FixupReferences();

            Mock<PermissionsService> perms = new Mock<PermissionsService>();
            perms.Setup(x => x.IsUserInRole("testuser", "Administrators")).Returns(true);
            perms.Setup(x => x.Username).Returns("testuser");
            new TestPermissionsCache().SetInstance("testuser", perms.Object);

            DataStoreService.TestStore = store;
            
            SarMember robert = store.Members.Single(f => f.FirstName == "Robert");
            Assert.AreEqual(5, robert.ContactInfo.Count);

            // TEST
            MemberContact contact = new MemberContact { Type = ContactType.Phone, SubType = "Cell", Value = "206-555-1653" };

            var dataResult = controller.SaveContactInfo(robert.Id, contact);
            var model = (SubmitResult<MemberContact>)dataResult.Data;

            // VERIFY
            Assert.AreEqual(0, model.Errors.Length);
            Assert.AreEqual(robert.Id, model.Result.MemberId, "Member ID should have been populated");
            Assert.AreEqual(6, robert.ContactInfo.Count);

            MemberContact savedContact = robert.ContactInfo.Single(f => f.Id == contact.Id);
            Assert.AreEqual(contact.Value, savedContact.Value);
            Assert.AreEqual(contact.Type, savedContact.Type);
            Assert.AreEqual(contact.SubType, savedContact.SubType);
        }

        [TestMethod]
        public void Basic_GetContactInfoTest()
        {
            // SETUP
            var controller = new MembersController();
            var mocks = new ContextMocks(controller);
            mocks.Request.Setup(r => r.AcceptTypes).Returns(new[] { "application/json" });

            TestStore store = ((TestStore)DevWebsiteDataInitializer.FillDefaultDevSet(new TestStore())).FixupReferences();

            Mock<PermissionsService> perms = new Mock<PermissionsService>();
            perms.Setup(x => x.IsUserInRole("testuser", "Administrators")).Returns(true);
            perms.Setup(x => x.Username).Returns("testuser");
            new TestPermissionsCache().SetInstance("testuser", perms.Object);

            DataStoreService.TestStore = store;

            SarMember robert = store.Members.Single(f => f.FirstName == "Robert");
            Assert.AreEqual(5, robert.ContactInfo.Count);

            // TEST

            var dataResult = controller.GetContactInfo(robert.Id);
            var model = (MemberContact[])dataResult.Data;

            // VERIFY
            Assert.AreEqual(5, model.Length);
            Assert.IsTrue(model.All(f => f.MemberId == robert.Id), "MemberId should be set on all");
            Assert.IsTrue(model.All(f => !string.IsNullOrEmpty(f.Value)), "All should have non-empty values");
        }

        [TestMethod]
        public void Basic_GetAllMemberships()
        {
            // SETUP
            var controller = new MembersController();
            var mocks = new ContextMocks(controller);
            mocks.Request.Setup(r => r.AcceptTypes).Returns(new[] { "application/json" });

            TestStore store = ((TestStore)DevWebsiteDataInitializer.FillDefaultDevSet(new TestStore())).FixupReferences();

            Mock<PermissionsService> perms = new Mock<PermissionsService>();
            perms.Setup(x => x.IsUserInRole("testuser", "Administrators")).Returns(true);
            perms.Setup(x => x.Username).Returns("testuser");
            new TestPermissionsCache().SetInstance("testuser", perms.Object);

            DataStoreService.TestStore = store;

            SarMember robert = store.Members.Single(f => f.FirstName == "Robert");

            // TEST

            var dataResult = controller.GetAllMemberships(robert.Id);
            var model = (UnitMembership[])dataResult.Data;

            // VERIFY
            Assert.AreEqual(2, model.Length);
            Assert.IsTrue(model.All(f => f.OrganizationId != Guid.Empty), "OrganizationId should be set on all");
        }
    }
}
