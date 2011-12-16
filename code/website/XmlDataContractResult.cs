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
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Web.Mvc;
    using System.Xml;

    public class XmlDataContractResult : DataActionResult
    {
        private string xmlString = null;

        public XmlDataContractResult(Object data) : base(data)
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

                DataContractSerializer dataContractSerializer = new DataContractSerializer(this.Data.GetType());

                XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();

                xmlWriterSettings.Indent = true;

                xmlWriterSettings.CheckCharacters = false;

                xmlWriterSettings.NewLineHandling = NewLineHandling.Entitize;

                using (MemoryStream ms = new MemoryStream())
                {

                    XmlWriter xmlWriter = XmlWriter.Create(ms, xmlWriterSettings);

                    dataContractSerializer.WriteObject(xmlWriter, this.Data);

                    xmlWriter.Close();

                    ms.Position = 0;
                    xmlString = Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            return xmlString.Replace(" xmlns:d3p1=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"", "").Replace("d3p1:","");

        }
    }
}
