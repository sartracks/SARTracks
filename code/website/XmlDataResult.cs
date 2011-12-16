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
namespace SarTracks.Website
{
    using System.IO;
    using System.Text;
    using System.Web.Mvc;
    using System.Xml;
    using System.Xml.Serialization;

    public class XmlDataResult : DataActionResult
    {
        private string xmlString = null;


        public XmlDataResult(object data) : base(data)
        {
        }
        
        public override void ExecuteResult(ControllerContext context)
        {
            context.HttpContext.Response.ContentType = "application/xml";
            context.HttpContext.Response.Write(this.GetXmlString());
        }

        protected override void OnSet()
        {
            base.OnSet();
            xmlString = null;
        }

        public string GetXmlString()
        {
            if (string.IsNullOrEmpty(xmlString))
            {
                XmlSerializer ser = new XmlSerializer(this.Data.GetType());
                using (MemoryStream s = new MemoryStream())
                {
                    XmlTextWriter write = new XmlTextWriter(s, System.Text.Encoding.UTF8);
                    write.Formatting = Formatting.Indented;
                    write.Indentation = 2;
                    ser.Serialize(write, this.Data);
                    write.Close();
                    s.Close();
                    xmlString = Encoding.UTF8.GetString(s.GetBuffer()).Trim((char)0);
                }
            }
            return xmlString;

        }
    }
}
