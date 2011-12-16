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
    using System.Configuration;
    using System.Data.Entity;
    using SarTracks.Website.Models;
    using System.Linq;
    using System.Data.Entity.Validation;
    using System.Data.Entity.ModelConfiguration.Conventions;

    public interface IDataStoreService : IDisposable
    {
        IDbSet<SarMember> Members { get; set; }
        IDbSet<Organization> Organizations { get; set; }
        IDbSet<Mission> Missions { get; set; }
        IDbSet<Training> Trainings { get; set; }
        IDbSet<TrainingCourse> TrainingCourses { get; set; }
        IDbSet<LogEntry> Log { get; set; }
        IDbSet<ExternalReference> ExternalReferences { get; set; }

        int SaveChanges();
        IEnumerable<DbEntityValidationResult> GetValidationErrors();
    }
    
    public class DataStoreService : DbContext, IDataStoreService
    {
        public static DateTime DATE_UNKNOWN
        {
            get { return new DateTime(1800, 1, 1, 0, 0, 0, DateTimeKind.Utc); }
        }

        /// <summary>Allows for test cases to pass in a test context</summary>
        public static IDataStoreService TestStore { get; set; }
        
        /// <summary>
        /// Generates a real or test context
        /// </summary>
        /// <returns></returns>
        public static IDataStoreService Create(string username)
        {
            if (DataStoreService.TestStore == null)
            {
                return new DataStoreService(username);
            }
            return DataStoreService.TestStore;
        }

        private string username;

        public IDbSet<SarMember> Members { get; set; }
        public IDbSet<Organization> Organizations { get; set; }
        public IDbSet<Mission> Missions { get; set; }
        public IDbSet<Training> Trainings { get; set; }
        public IDbSet<TrainingCourse> TrainingCourses { get; set; }
        public IDbSet<LogEntry> Log { get; set; }
        public IDbSet<ExternalReference> ExternalReferences { get; set; }

        public DataStoreService(string username)
            : base("Data")
        {
            this.username = username;
            string initType = ConfigurationManager.AppSettings["testDataInitializerType"];
            IDatabaseInitializer<DataStoreService> initializer;
            if (string.IsNullOrWhiteSpace(initType))
            {
                initializer = new CreateDatabaseIfNotExists<DataStoreService>();
            }
            else
            {
                Type type = Type.GetType(initType);
                initializer = (IDatabaseInitializer<DataStoreService>)Activator.CreateInstance(type);

            }
            Database.SetInitializer<DataStoreService>(initializer);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges()
        {
            List<LogEntry> logs = new List<LogEntry>();
            foreach (var entry in this.ChangeTracker.Entries().Where(f => f.State != System.Data.EntityState.Unchanged))
            {                
                SarObject obj = entry.Entity as SarObject;
                if (obj == null)
                    continue;

                obj.ChangedBy = this.username;
                logs.Add(new LogEntry
                {
                    ReferenceId = obj.Id,
                    ChangedBy = this.username,
                    LastChanged = DateTime.UtcNow,
                    Action = entry.State.ToString(),
                    Type = obj.GetType().ToString()
                });
            }

            logs.ForEach(f => this.Log.Add(f));

            return base.SaveChanges();
        }
    }   
}