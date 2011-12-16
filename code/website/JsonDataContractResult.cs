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
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Web.Mvc;

    public class JsonDataContractResult : DataActionResult
    {
        private string jsonString = null;

        public JsonDataContractResult(Object data) : base(data)
        {
        }

        public override void ExecuteResult(ControllerContext context)
        {
            context.HttpContext.Response.ContentType = "application/json";
            context.HttpContext.Response.Write(this.GetJsonString());
        }

        protected override void OnSet()
        {
            base.OnSet();
            jsonString = null;
        }

        public string GetJsonString()
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                var serializer = new DataContractJsonSerializer(this.Data.GetType());
                using (var ms = new MemoryStream())
                {
                    serializer.WriteObject(ms, this.Data);
                    jsonString = Encoding.Default.GetString(ms.ToArray());
                }
            }
            return jsonString;

        }
    }
}
