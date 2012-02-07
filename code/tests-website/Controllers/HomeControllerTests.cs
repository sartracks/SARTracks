namespace SarTracks.Tests.Website.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using SarTracks.Website.Controllers;
    using SarTracks.Website.Services;
    using Moq;
    using SarTracks.Website.Models;
    using System.Web.Mvc;
    using SarTracks.Website.ViewModels;

    [TestClass]
    public class HomeControllerTests
    {
        [TestMethod]
        public void Homepage()
        {
            TestStore store = ((TestStore)DevWebsiteDataInitializer.FillDefaultDevSet(new TestStore())).FixupReferences();
            var org = store.Organizations.First();

            // Setup
            User u = new User();

            var perms = new Mock<TestAuthIdentityService>();
            perms.Setup(f => f.UserLogin).Returns("testuser");
            perms.Setup(f => f.UserName).Returns("Test User");
            perms.Setup(f => f.IsAuthenticated).Returns(true);
            perms.Setup(f => f.HasPermission(PermissionType.SiteAdmin, null)).Returns(true);
            perms.Setup(f => f.IsUser).Returns(true);
            perms.Setup(f => f.User).Returns(u);
//            perms.Setup(f => f.GetRolesForUser("testuser", true)).Returns(new[] { "role1", "role2", "org" + org.Id.ToString() + ".foo" });
            
            var controller = new HomeController(perms.Object, new DataStoreFactory(store));
            //var mocks = new ContextMocks(controller);
            //mocks.Request.Setup(r => r.AcceptTypes).Returns(new[] { "application/json" });

            //Test
            var result = controller.Index();

            // Verify
            Assert.IsInstanceOfType(result, typeof(ViewResult));

            var view = (ViewResult)result;
            
            Assert.IsNotNull(view.Model, "Did not return model");

            Assert.IsInstanceOfType(view.Model, typeof(HomePageViewModel));
            var model = (HomePageViewModel)view.Model;

            Assert.IsTrue(model.HasAccount);
            Assert.AreEqual(Guid.Empty, model.LinkedMember);
            Assert.IsTrue(model.LoggedIn);
            Assert.IsFalse(model.MyDetails);
            Assert.IsFalse(model.MyTraining);
            Assert.IsFalse(model.MyMissions);
        }

    }
}
