using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Controllers;
using MictcoWebService.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
namespace MictcoWebService.Common
{
    public class DbUserServer
    {
        public string userId = "0";
       
        public string username = "";
        public string password = "";
        public string user_role = "";
        public string status = "";
        public int ret=0;
        //public string connetionString = "";
        public SqlConnection shop;
        public SqlDataAdapter da;

        ControllerBase controller;
        public DbUserServer(ControllerBase controller)
        {
            ClientSqlServer csqlr = null;
            this.controller = controller;
            var handler = new JwtSecurityTokenHandler();
            string authHeader = controller.Request.Headers["Authorization"];
            authHeader = authHeader.Replace("Bearer ", "");
            var jsonToken = handler.ReadToken(authHeader);
            var tokenS = handler.ReadToken(authHeader) as JwtSecurityToken;

            var jti = tokenS.Claims.First(claim => claim.Type == "jti").Value;
            var id = tokenS.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;

            this.userId = tokenS.Claims.First(claim => claim.Type == ClaimTypes.Sid).Value;

          
            this.username = tokenS.Claims.First(claim => claim.Type == ClaimTypes.Name).Value;
          
           
            this.user_role = tokenS.Claims.First(claim => claim.Type == ClaimTypes.Role).Value;

            this.status = tokenS.Claims.First(claim => claim.Type == ClaimTypes.Gender).Value;
            csqlr = new ClientSqlServer();
            String sql = "";
            string userID = CommonHelper.tokenDecrypt(userId);
            sql = "select * from main_user  where  username = '" +username + "' and id = " + userID + " and status = 1";
            DataTable userList = csqlr.dbReaderFill(sql);
            if(userList.Rows[0]["username"].ToString()== username && Convert.ToInt32(CommonHelper.tokenDecrypt(status))==1)
            {
                ret = 1;
            }

        }
    }
}
