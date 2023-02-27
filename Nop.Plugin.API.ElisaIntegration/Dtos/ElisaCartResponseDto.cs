using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.API.ElisaIntegration.Dtos
{
    public partial class ElisaCartResponseDto
    {
        #region Ctor
        public ElisaCartResponseDto()
        {
            Errors = new List<string>();
        }

        #endregion

        #region Properties
        [JsonProperty("elisa_cart_id")]
        public Guid ElisaCartId { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        public IList<string> Errors { get; set; }
        #endregion
    }
}
