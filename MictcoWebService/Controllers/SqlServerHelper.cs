using Microsoft.AspNetCore.Mvc;
using MictcoWebService.Common;

namespace MictcoWebService.Controllers
{
    internal class SqlServerHelper : UserSqlServer
    {
        public SqlServerHelper(ControllerBase controller) : base(controller)
        {
        }
    }
}