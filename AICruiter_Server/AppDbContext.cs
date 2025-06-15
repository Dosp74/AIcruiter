using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AICruiter_Server.Models;
using Microsoft.EntityFrameworkCore;

namespace AICruiter_Server
{
    public class AppDbContext : DbContext
    {
        public DbSet<SharedAnswer> SharedAnswers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=shared.db");
            }
        }
    }
}