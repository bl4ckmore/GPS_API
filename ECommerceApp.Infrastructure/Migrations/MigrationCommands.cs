using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceApp.Infrastructure.Migrations
{
    internal class MigrationCommands
    {
    }
}



/*
 * 
 * 
 * 
DROP SCHEMA public CASCADE;
CREATE SCHEMA public;


dotnet ef migrations add InitialMaster --project ECommerceApp.Infrastructure --startup-project ECommerceApp.API



dotnet ef database update --project ECommerceApp.Infrastructure --startup-project ECommerceApp.API


*/
