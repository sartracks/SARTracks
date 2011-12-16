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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using SarTracks.Website.Services;

    public static class EntityExtensions
    {
        public static IQueryable<T> IncludePaths<T>(this IQueryable<T> set, params string[] includes) where T : class
        {
            DbQuery<T> cast = set as DbQuery<T>;
            if (cast != null)
            {
                foreach (var include in includes)
                {
                    cast = cast.Include(include);
                }
                set = cast;
            }
            return set;
        }

        public static void Delete(this IDataStoreService context, object entityObject)
        {
            DbContext ctx = context as DbContext;
            if (ctx != null)
            {
                ((IObjectContextAdapter)ctx).ObjectContext.DeleteObject(entityObject);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}