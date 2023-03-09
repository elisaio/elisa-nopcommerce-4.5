using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Orders;
using Nop.Core;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Shipping.Date;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Http.Extensions;

namespace Nop.Plugin.API.ElisaIntegration.Services
{
    public partial class CustomShoppingCartService : ShoppingCartService, IShoppingCartService
    {
        #region Fields

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        #endregion

        #region Ctor
        public CustomShoppingCartService(
            CatalogSettings catalogSettings,
            IAclService aclService,
            IActionContextAccessor actionContextAccessor,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICheckoutAttributeService checkoutAttributeService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IDateRangeService dateRangeService,
            IDateTimeHelper dateTimeHelper,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IPermissionService permissionService,
            IPriceCalculationService priceCalculationService,
            IPriceFormatter priceFormatter,
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService,
            IProductService productService,
            IRepository<ShoppingCartItem> sciRepository,
            IShippingService shippingService,
            IStaticCacheManager staticCacheManager,
            IStoreContext storeContext,
            IStoreMappingService storeMappingService,
            IUrlHelperFactory urlHelperFactory,
            IUrlRecordService urlRecordService,
            IWorkContext workContext,
            OrderSettings orderSettings,
            ShoppingCartSettings shoppingCartSettings,
            IHttpContextAccessor httpContextAccessor) : base(
                catalogSettings,
                aclService,
                actionContextAccessor,
                checkoutAttributeParser,
                checkoutAttributeService,
                currencyService,
                customerService,
                dateRangeService,
                dateTimeHelper,
                genericAttributeService,
                localizationService,
                permissionService,
                priceCalculationService,
                priceFormatter,
                productAttributeParser,
                productAttributeService,
                productService,
                sciRepository,
                shippingService,
                staticCacheManager,
                storeContext,
                storeMappingService,
                urlHelperFactory,
                urlRecordService,
                workContext,
                orderSettings,
                shoppingCartSettings)
        {
            _httpContextAccessor = httpContextAccessor;
            _priceCalculationService = priceCalculationService;
            _productAttributeParser = productAttributeParser;
            _shoppingCartSettings = shoppingCartSettings;
        }
        #endregion

        #region Overriden methods
        /// <summary>
        /// Gets the shopping cart unit price (one item)
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">Customer</param>
        /// <param name="shoppingCartType">Shopping cart type</param>
        /// <param name="quantity">Quantity</param>
        /// <param name="attributesXml">Product attributes (XML format)</param>
        /// <param name="customerEnteredPrice">Customer entered price (if specified)</param>
        /// <param name="rentalStartDate">Rental start date (null for not rental products)</param>
        /// <param name="rentalEndDate">Rental end date (null for not rental products)</param>
        /// <param name="includeDiscounts">A value indicating whether include discounts or not for price computation</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shopping cart unit price (one item). Applied discount amount. Applied discounts
        /// </returns>
        public override async Task<(decimal unitPrice, decimal discountAmount, List<Discount> appliedDiscounts)> GetUnitPriceAsync(Product product,
            Customer customer,
            ShoppingCartType shoppingCartType,
            int quantity,
            string attributesXml,
            decimal customerEnteredPrice,
            DateTime? rentalStartDate, DateTime? rentalEndDate,
            bool includeDiscounts)
        {
            #region Custom code
            //Custom code by Ajay Chauhan on 17-03-2022
            //Call base class method in default behaviour and call custom logic based on elisa session existance
            //store unique elisa cart Id in session
            var elisaCsrtId = _httpContextAccessor.HttpContext.Session.Get<Guid>(ElisaPluginDefaults.ElisaCartId);
            bool isSessionExists = false;

            if (elisaCsrtId == Guid.Empty)
            {
                return await base.GetUnitPriceAsync(product, customer, shoppingCartType, quantity, attributesXml, customerEnteredPrice, rentalStartDate, rentalEndDate, includeDiscounts);
            }
            else
                isSessionExists = true;
            #endregion

            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var discountAmount = decimal.Zero;
            var appliedDiscounts = new List<Discount>();

            decimal finalPrice;

            var combination = await _productAttributeParser.FindProductAttributeCombinationAsync(product, attributesXml);
            if (combination?.OverriddenPrice.HasValue ?? false)
            {
                (_, finalPrice, discountAmount, appliedDiscounts) = await _priceCalculationService.GetFinalPriceAsync(product,
                        customer,
                        combination.OverriddenPrice.Value,
                        decimal.Zero,
                        includeDiscounts,
                        quantity,
                        product.IsRental ? rentalStartDate : null,
                        product.IsRental ? rentalEndDate : null);
            }
            else
            {
                //summarize price of all attributes
                var attributesTotalPrice = decimal.Zero;
                var attributeValues = await _productAttributeParser.ParseProductAttributeValuesAsync(attributesXml);
                if (attributeValues != null)
                {
                    foreach (var attributeValue in attributeValues)
                    {
                        attributesTotalPrice += await _priceCalculationService.GetProductAttributeValuePriceAdjustmentAsync(product, attributeValue, customer, product.CustomerEntersPrice ? (decimal?)customerEnteredPrice : null);
                    }
                }

                //get price of a product (with previously calculated price of all attributes)
                //Custom code by Ajay Chauhan on 17-03-2022
                //Call base class method in default behaviour and call custom logic based on elisa session existance
                //get price of a product (with previously calculated price of all attributes)
                if (product.CustomerEntersPrice || (isSessionExists && customerEnteredPrice > 0))
                {
                    finalPrice = customerEnteredPrice;
                }
                else
                {
                    int qty;
                    if (_shoppingCartSettings.GroupTierPricesForDistinctShoppingCartItems)
                    {
                        //the same products with distinct product attributes could be stored as distinct "ShoppingCartItem" records
                        //so let's find how many of the current products are in the cart                        
                        qty = (await GetShoppingCartAsync(customer, shoppingCartType: shoppingCartType, productId: product.Id))
                            .Sum(x => x.Quantity);

                        if (qty == 0)
                        {
                            qty = quantity;
                        }
                    }
                    else
                    {
                        qty = quantity;
                    }

                    (_, finalPrice, discountAmount, appliedDiscounts) = await _priceCalculationService.GetFinalPriceAsync(product,
                        customer,
                        attributesTotalPrice,
                        includeDiscounts,
                        qty,
                        product.IsRental ? rentalStartDate : null,
                        product.IsRental ? rentalEndDate : null);
                }
            }

            //rounding
            if (_shoppingCartSettings.RoundPricesDuringCalculation)
                finalPrice = await _priceCalculationService.RoundPriceAsync(finalPrice);

            return (finalPrice, discountAmount, appliedDiscounts);
        }
        #endregion
    }
}
