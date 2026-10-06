using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MictcoWebService.Common;


namespace MictcoWebService.Models
{
    

    public class ClientLoginContext
    {
        

        public DbSet<ClientLoginModel> ClientLoginModels { get; set; }


        public DataTable authLogin(ClientLoginModel client,ClientSqlServer csqlsr)
        {
            string query = "EXEC [dbo].[Sp_downloaddatabase] @username = N'"+client.ClientId+"',@password = N'"+client.Secret+"'";
            DataTable dt = csqlsr.dbReaderFill(query);
            return dt;
        }

        public DataTable checkItem(ClientSqlServer csqlsr)
        {
            string query = "select * from acc_subhead where as_id=1";
            DataTable dt = csqlsr.dbReaderFill(query);
            return dt;
        }



    }
}
