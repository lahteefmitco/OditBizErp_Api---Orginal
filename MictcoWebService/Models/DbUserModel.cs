using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MictcoWebService.Models
{
    public class DbUserModel
    {
        //databases
        public int d_id { get; set; }
        public string d_database { get; set; }
        public string d_host { get; set; }
        public string d_username { get; set; }
        public string d_password { get; set; }
        public string d_encread { get; set; }
        public string d_alias { get; set; }
        //db_to_user
        public int du_id { get; set; }
        public int du_user { get; set; }
        public int du_database { get; set; }
        //main_user
        public int status { get; set; }
        public int id { get; set; }
        public string  username{get;set;}
        public string password { get; set; }
        public string role { get; set; }
        //user
        public int u_id { get; set; }
        public string u_username { get; set; }
        public string u_password { get; set; }
        public string statement { get; set; }
        //common
        public string value { get; set; }
    }
}
 