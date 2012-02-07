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
    using SarTracks.Website.ViewModels;
    using System.Web.Mvc;

    [TestClass]
    public class AccountsControllerTests
    {
        [TestMethod]
        public void GetMyRoles()
        {
            TestStore store = ((TestStore)DevWebsiteDataInitializer.FillDefaultDevSet(new TestStore())).FixupReferences();
            var org = store.Organizations.First();

            // Setup
            var perms = new Mock<TestAuthIdentityService>();
            perms.Setup(f => f.UserLogin).Returns("testuser");
            perms.Setup(f => f.GetRolesForUser("testuser", true)).Returns(new[] { "role1", "role2", string.Format("[{0}] foo", org.Name) });
            
            var controller = new AccountController(perms.Object, new DataStoreFactory(store));
            var mocks = new ContextMocks(controller);
            mocks.Request.Setup(r => r.AcceptTypes).Returns(new[] { "application/json" });

            //Test
            var result = controller.GetMyRoles();

            // Verify
            Assert.IsNotNull(result.Data);
            Assert.IsInstanceOfType(result.Data, typeof(IEnumerable<string>));
            var data = ((IEnumerable<string>)result.Data).ToArray();
            Assert.AreEqual(3, data.Length);
            Assert.IsTrue(data.Any(f => f == "[" + org.Name + "] foo"));
        }
    }
}
