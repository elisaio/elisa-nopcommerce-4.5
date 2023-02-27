using Nop.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.API.ElisaIntegration.Domain
{
    /// <summary>
    /// Represents a custom cart items for elisa
    /// </summary>
    public class CustomCartItems : BaseEntity
    {
        #region Properites
        /// <summary>
        /// Gets or sets the elisa custom cart identity reference
        /// </summary>
        public Guid CustomCartId { get; set; }

        /// <summary>
        /// Gets or sets the product identity
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Gets or sets the parent product identity
        /// </summary>
        public int ParentProductId { get; set; }

        /// <summary>
        /// Gets or sets the quantity
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets product attribute combination XML
        /// </summary>
        public string AttributeXML { get; set; }
        #endregion
    }
}
