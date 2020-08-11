using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace AzReplicate.Sample.WebAppCompleter
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
        }

        public DbSet<Image> Images { get; set; }
    }
}