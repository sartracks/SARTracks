
namespace SarTracks.Website.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.ComponentModel.DataAnnotations;
    using System.Xml;
    using System.Xml.Serialization;
    using System.IO;

    public class ChangeRequest : SarObject
    {
        public string Data { get; protected set; }
        public Guid? AuthKey { get; set; }
        public string OfferedTo { get; set; }
        public string OfferedBy { get; set; }
        public string Type { get; set; }

        public ChangeRequest LoadData<T>(T data)
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));
            using (var writer = new StringWriter())
            {

                using (var stream = new MemoryStream())
                {
                    ser.Serialize(stream, data);
                }
                this.Data = writer.ToString();
            }
            
            this.Type = typeof(T).Name;
            return this;
        }

        public T GetData<T>()
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));
            using (var reader = new StringReader(this.Data))
            {
                return (T)ser.Deserialize(reader);
            }
        }
    }    
}