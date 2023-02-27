using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.API.ElisaIntegration.Models
{
    public class AuthenticationRequestModel
    {
        public string UserName { get; set; }

        public string Password { get; set; }
    }
}
