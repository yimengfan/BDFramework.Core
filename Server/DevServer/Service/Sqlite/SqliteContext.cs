using System.IO;
using DevServer.Model;
using Microsoft.EntityFrameworkCore;

namespace DevServer.Service.Sqlite
{
    public class SqliteContext : DbContext
    {
        
        //Path
        static string sqlpath = "Data Source=" + Directory.GetCurrentDirectory() + "/wwwroot/DB/Local.sqlite";
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlite(sqlpath);
        }
        
        
        public DbSet<AssetBundleData> AssetBundleDatas { get; set; }

    }
}