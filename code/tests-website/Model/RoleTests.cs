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
    public class RoleTests
    {
        [TestMethod]
        public void Role_Flatten()
        {
            Role a = new Role { Name = "A" };
            Role b = new Role { Name = "B" };
            Role c = new Role { Name = "C" };
            Role d = new Role { Name = "D" };

            MakeMember(a, b);
            MakeMember(b, c);
            MakeMember(a, d);
            MakeMember(c, d);
            
            var flat = a.Flatten().Distinct().ToArray();
            Assert.AreEqual(4, flat.Length, "Should have 4 roles total");
            foreach (var name in new[] { "A", "B", "C", "D" })
            {
                Assert.IsNotNull(flat.SingleOrDefault(f => f.Name == name));
            }
        }

        public static void MakeMember(Role aMemberOf, Role isAlsoAMemberOf)
        {
            RoleRoleMembership ab = new RoleRoleMembership { Parent = isAlsoAMemberOf, Child = aMemberOf };
            isAlsoAMemberOf.MemberRoles.Add(ab);
            aMemberOf.MemberOfRoles.Add(ab);
        }
    }
}
