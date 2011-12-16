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
namespace SarTracks
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Net.Mail;

    public static class Utils
    {
        public const string IErrorLogConfigKey = "ErrorLoggerType";

        public static bool CheckSimpleName(string name, bool throwOnError)
        {
            bool simple = true;

            if (string.IsNullOrEmpty(name))
            {
                simple = false;
                if (throwOnError)
                {
                    throw new ArgumentException("Cannot be empty", name);
                }
            }

            if (!Regex.IsMatch(name, @"^[\._a-z0-9\-]+$", RegexOptions.IgnoreCase))
            {
                simple = false;
                if (throwOnError)
                {
                    throw new ArgumentException("Can only contain numbers, letters, '.', '-', and '_'", name);
                }
            }

            return simple;
        }
    }
}
