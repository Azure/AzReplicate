using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace AzReplicate.Sample.WebApp.Models
{
    /// <summary>
    /// Using EF Core to read/write data to SQL Server
    /// <see cref="https://docs.microsoft.com/en-us/ef/core/"/>
    /// </summary>
    public class ImageContext : DbContext
    {
        public ImageContext(DbContextOptions<ImageContext> options)
            : base (options)
        {
            //auto run the migrations when the app starts up. 
            Database.Migrate();
        }

        public DbSet<Image> Images { get; set; }
    }
}