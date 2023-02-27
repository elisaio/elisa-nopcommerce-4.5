using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.API.ElisaIntegration.Dtos
{
    /// <summary>
    /// Represents a API response dto from elisa
    /// </summary>
    public partial class APIResponseDto
    {
        #region Ctor
        public APIResponseDto()
        {
            Errors = new List<Error>();
        }

        #endregion

        #region Properties
        public bool IsSuccess { get; set; }

        public dynamic Data { get; set; }

        public IList<Error> Errors { get; set; }
        public class Error
        {
            public string Message { get; set; }
        }
        #endregion
    }
}
