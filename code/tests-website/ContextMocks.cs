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
namespace SarTracks.Tests.Website
{
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Moq;
    using System.Security.Principal;

    public class ContextMocks
    {
        public Moq.Mock<HttpContextBase> HttpContext { get; set; }
        public Moq.Mock<HttpRequestBase> Request { get; set; }
        public Moq.Mock<IPrincipal> User { get; set; }
        public RouteData RouteData { get; set; }

        public ContextMocks(Controller controller)
        {
            //define context objects
            HttpContext = new Moq.Mock<HttpContextBase>();
            Request = new Mock<HttpRequestBase>();
            User = new Mock<IPrincipal>();

            HttpContext.Setup(x => x.Request).Returns(Request.Object);
            HttpContext.Setup(x => x.User).Returns(User.Object);
            //you would setup Response, Session, etc similarly with either mocks or fakes

            //apply context to controller
            RequestContext rc = new RequestContext(HttpContext.Object, new RouteData());
            controller.ControllerContext = new ControllerContext(rc, controller);
        }
    }
}
