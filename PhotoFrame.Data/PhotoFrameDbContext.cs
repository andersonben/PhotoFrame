using System;
using Microsoft.EntityFrameworkCore;

namespace PhotoFrame.Data
{

    public class PhotoFrameDbContext : DbContext
    {
        public DbSet<Photo> Photos {get; set;}


        public PhotoFrameDbContext(DbContextOptions<PhotoFrameDbContext> options)
        : base(options)
        {
        }
    }
}

