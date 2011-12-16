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
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Runtime.Serialization;
    using System.Data.Entity.Validation;

    [DataContract]
    public class SubmitResult<T>
    {
        [DataMember]
        public T Result { get; set; }

        [DataMember]
        public SubmitError[] Errors { get; set; }

        public SubmitResult()
        {
            this.Errors = new SubmitError[0];
        }
    }

    [DataContract]
    public class SubmitError
    {
        [DataMember]
        public string Property { get; set; }

        [DataMember]
        public string Error { get; set; }

        [DataMember]
        public Guid[] Id { get; set; }

        public static List<SubmitError> FromValidationResults(IEnumerable<DbEntityValidationResult> results)
        {
            List<SubmitError> errors = new List<SubmitError>();
            foreach (var prop in results)
            {
                IIdObject idObject = prop.Entry.Entity as IIdObject;
                Guid[] ids = (idObject == null) ? new Guid[0] : new[] { idObject.Id };
                
                foreach (var err in prop.ValidationErrors)
                {
                    errors.Add(new SubmitError { Error = err.ErrorMessage, Property = err.PropertyName, Id = ids });
                }
            }
            return errors;
        }
    }
}
