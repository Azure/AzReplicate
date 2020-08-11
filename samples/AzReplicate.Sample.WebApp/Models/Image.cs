using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzReplicate.Sample.WebApp.Models
{
    /// <summary>
    /// This is a simple DTO to represent the info for an image we are storing in the database. 
    /// </summary>
    public class Image
    {
        public int ImageId { get; set; }
        public string Url { get; set; }
    }
}
