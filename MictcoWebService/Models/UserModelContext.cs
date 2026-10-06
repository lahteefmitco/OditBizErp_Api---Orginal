using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MictcoWebService.Models
{
    public class UserModelContext : DbContext
    {
        public UserModelContext(DbContextOptions<UserModelContext> options)
            : base(options)
        {
        }

        public DbSet<UserModel> TodoItems { get; set; }
    }
}
