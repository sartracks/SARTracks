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
namespace SarTracks.Website.Services
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using SarTracks.Website.Models;
    using System.Net.Mail;
    using System.Configuration;

    public class MailService
    {
        public static void SendMail(string to, string subject, string text)
        {
            string fromAddress = ConfigurationManager.AppSettings["FromAddress"];

            MailMessage message = new MailMessage(fromAddress, to, subject, text);

            SmtpClient client = new SmtpClient();
            client.Send(message);
        }
    }
}