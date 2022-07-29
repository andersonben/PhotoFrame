using System;
using Microsoft.EntityFrameworkCore;
using PhotoFrame.Data.Entities;

namespace PhotoFrame.Data
{

    public class PhotoFrameDbContext : DbContext
    {
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Setting> Settings {get;set;}


        private string DbPath { get;}

        public PhotoFrameDbContext()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DbPath = System.IO.Path.Join(path, "photos.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");

        public PhotoFrameDbContext(DbContextOptions<PhotoFrameDbContext> options)
        : base(options)
        {
        }
    }
}

