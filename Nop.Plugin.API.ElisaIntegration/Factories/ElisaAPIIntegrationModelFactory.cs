using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Newtonsoft.Json;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.API.ElisaIntegration.Domain;
using Nop.Plugin.API.ElisaIntegration.Dtos;
using Nop.Plugin.API.ElisaIntegration.Models;
using Nop.Plugin.API.ElisaIntegration.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Seo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nop.Plugin.API.ElisaIntegration.Dtos.APIResponseDto;
using static Nop.Plugin.API.ElisaIntegration.Dtos.ItemResponseDto.CustomProductModel;
using static Nop.Plugin.API.ElisaIntegration.Dtos.ItemResponseDto;
using NUglify.Helpers;
using Nop.Core.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Stores;

namespace Nop.Plugin.API.ElisaIntegration.Factories
{
    public class ElisaAPIIntegrationModelFactory
    {
        #region Fields
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<ProductAttributeCombination> _productAttributeCombinationRepository;
        private readonly IRepository<ShoppingCartItem> _sciRepository;
        private readonly IRepository<StockQuantityHistory> _stockQuantityHistoryRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ICustomerService _customerService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IOrderService _orderService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IPictureService _pictureService;
        private readonly IProductService _productService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IStoreContext _storeContext;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly MediaSettings _mediaSettings;
        private readonly CustomCartService _customCartService;
        private readonly CatalogSettings _catalogSettings;
        #endregion

        #region Ctor
        public ElisaAPIIntegrationModelFactory(IRepository<Product> productRepository,
            IRepository<ProductAttributeCombination> productAttributeCombinationRepository,
            IRepository<ShoppingCartItem> sciRepository,
            IRepository<StockQuantityHistory> stockQuantityHistoryRepository,
            IRepository<Order> orderRepository,
            IActionContextAccessor actionContextAccessor,
            ICustomerActivityService customerActivityService,
            ICustomerService customerService,
            IGenericAttributeService genericAttributeService,
            IHttpContextAccessor httpContextAccessor,
            ILocalizationService localizationService,
            ILogger logger,
            IOrderService orderService,
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService,
            IPictureService pictureService,
            IProductService productService,
            IShoppingCartService shoppingCartService,
            IStaticCacheManager staticCacheManager,
            IStoreContext storeContext,
            IStoreMappingService storeMappingService,
            IUrlHelperFactory urlHelperFactory,
            IUrlRecordService urlRecordService,
            IWebHelper webHelper,
            IWorkContext workContext,
            MediaSettings mediaSettings,
            CustomCartService customCartService,
            CatalogSettings catalogSettings)
        {
            _productRepository = productRepository;
            _productAttributeCombinationRepository = productAttributeCombinationRepository;
            _sciRepository = sciRepository;
            _stockQuantityHistoryRepository = stockQuantityHistoryRepository;
            _orderRepository = orderRepository;
            _actionContextAccessor = actionContextAccessor;
            _customerActivityService = customerActivityService;
            _customerService = customerService;
            _genericAttributeService = genericAttributeService;
            _httpContextAccessor = httpContextAccessor;
            _localizationService = localizationService;
            _logger = logger;
            _orderService = orderService;
            _productAttributeParser = productAttributeParser;
            _productAttributeService = productAttributeService;
            _pictureService = pictureService;
            _productService = productService;
            _shoppingCartService = shoppingCartService;
            _staticCacheManager = staticCacheManager;
            _storeContext = storeContext;
            _storeMappingService = storeMappingService;
            _urlHelperFactory = urlHelperFactory;
            _urlRecordService = urlRecordService;
            _webHelper = webHelper;
            _workContext = workContext;
            _mediaSettings = mediaSettings;
            _customCartService = customCartService;
            _catalogSettings = catalogSettings;
        }
        #endregion

        #region Methods
        public async Task<string> PrepareProdutsJsonSerilization(string timeStamp, int pageNumber)
        {
            ItemResponseDto items = new ItemResponseDto();
            IList<CustomProductModel> productList = new List<CustomProductModel>();
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            DateTime? fromDate = dtDateTime.AddSeconds(Convert.ToDouble(timeStamp)).ToUniversalTime();
            var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);

            List<int> productIds = new List<int>();
            List<int?> combinationIds = new List<int?>();

            int pageSize = 1000;
            if (Convert.ToDouble(timeStamp) > 0)
            {
                var query = (from sqh in _stockQuantityHistoryRepository.Table
                             where sqh.CreatedOnUtc >= fromDate
                             select sqh).Distinct().ToList();

                productIds = query.Select(x => x.ProductId).Distinct().ToList();
                combinationIds = query.Select(x => x.CombinationId).Distinct().ToList();

                combinationIds.RemoveAll(x => x == null);
            }

            var products = (from p in _productRepository.Table
                            orderby p.DisplayOrder, p.Id
                            where (productIds.Count > 0 ? productIds.Contains(p.Id) : false) ||
                            p.UpdatedOnUtc >= fromDate.Value
                            select p);

            products = products.Where(x => x.Published && !x.Deleted);

            //apply store mapping constraints
            products = await _storeMappingService.ApplyStoreMapping(products, _storeContext.GetCurrentStore().Id);

            var updatedProducts = await products.ToPagedListAsync(pageNumber, pageSize);

            foreach (var product in updatedProducts)
            {
                try
                {
                    var picture = (await _pictureService.GetPicturesByProductIdAsync(product.Id, 1)).FirstOrDefault();
                    var pictures = await _pictureService.GetPicturesByProductIdAsync(product.Id);
                    var productAttributeMappings = await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(product.Id);
                    //get product SEO slug name
                    var seName = await _urlRecordService.GetSeNameAsync(product);
                    IList<ProductAttributesModel> cpam = new List<ProductAttributesModel>();

                    if (productAttributeMappings != null && productAttributeMappings.Count > 0)
                    {
                        foreach (var pam in productAttributeMappings)
                        {
                            var productAttribute = await _productAttributeService.GetProductAttributeByIdAsync(pam.ProductAttributeId);
                            var productAttributeValue = await _productAttributeService.GetProductAttributeValuesAsync(pam.Id);
                            cpam.Add(new ProductAttributesModel
                            {
                                AttributeId = productAttribute.Id,
                                Name = productAttribute.Name,
                                Position = pam.DisplayOrder,
                                AttributeValues = productAttributeValue.Select(x => x.Name).ToList()
                            });
                        }
                    }

                    var customProduct = new CustomProductModel
                    {
                        Id = product.Id,
                        ParentId = product.ParentGroupedProductId,
                        AttributeCombinationId = 0,
                        Name = product.Name,
                        Sku = product.Sku,
                        Price = product.Price,
                        Description = product.ShortDescription,
                        ProductDetailUrl = urlHelper.RouteUrl("Product", new { SeName = seName }, _webHelper.GetCurrentRequestProtocol()),
                        ProductType = ((CustomProductTypes)product.ProductTypeId).ToString(),
                        Status = product.Published ? "enabled" : "disabled",
                        MainImage = picture != null ? await _pictureService.GetPictureUrlAsync(picture.Id, _mediaSettings.ProductThumbPictureSize) : string.Empty,
                        OtherImage = pictures != null && pictures.Count > 0 ? await pictures.SelectAwait(async x => await _pictureService.GetPictureUrlAsync(x.Id, _mediaSettings.AssociatedProductPictureSize, false)).ToListAsync() : null,
                        ManageStock = product.ManageInventoryMethodId == 2 ? false : true,
                        Inventory = product.ManageInventoryMethodId == 2 ? 0 : product.StockQuantity,
                        AllowOutOfStock = false,
                        //AvailableProductAttributes = product.ManageInventoryMethodId == 2 ? cpam : null
                        AvailableProductAttributes = cpam
                    };

                    productList.Add(customProduct);

                    //insert child products if available
                    var pacs = await _productAttributeService.GetAllProductAttributeCombinationsAsync(product.Id);
                    if (pacs != null && pacs.Count > 0)
                    {
                        foreach (var pac in pacs)
                        {
                            if (combinationIds.Count > 0 && !combinationIds.Contains(pac.Id))
                                continue;

                            List<AssociatedProductAttributesModel> associatedAttributes = new List<AssociatedProductAttributesModel>();
                            if (!string.IsNullOrEmpty(pac.AttributesXml))
                            {
                                var parsedProductAttributes = await _productAttributeParser.ParseProductAttributeMappingsAsync(pac.AttributesXml);
                                if (parsedProductAttributes != null && parsedProductAttributes.Count > 0)
                                {
                                    foreach (var ppa in parsedProductAttributes)
                                    {
                                        var attributeValuesStr = _productAttributeParser.ParseValues(pac.AttributesXml, ppa.Id);
                                        foreach (var av in attributeValuesStr)
                                        {
                                            associatedAttributes.Add(new AssociatedProductAttributesModel
                                            {
                                                ProductAttributeId = ppa.ProductAttributeId,
                                                ProductAttributeValue = (await _productAttributeService.GetProductAttributeValueByIdAsync(int.Parse(av)))?.Name ?? string.Empty
                                            });
                                        }
                                    }
                                }
                            }

                            var childProductPicture = await _pictureService.GetPictureByIdAsync(pac.PictureId);
                            var childProduct = new CustomProductModel
                            {
                                Id = pac.Id,
                                ParentId = product.Id,
                                AttributeCombinationId = pac.Id,
                                Name = product.Name + " " + pac.Sku,
                                Sku = product.Sku + "-" + pac.Sku,
                                Price = Convert.ToDecimal(pac.OverriddenPrice),
                                Description = product.ShortDescription,
                                ProductDetailUrl = urlHelper.RouteUrl("Product", new { SeName = seName }, _webHelper.GetCurrentRequestProtocol()),
                                ProductType = CustomProductTypes.Simple.ToString(),
                                Status = product.Published && pac.StockQuantity > 0 ? "enabled" : "disabled",
                                MainImage = childProductPicture != null ? await _pictureService.GetPictureUrlAsync(childProductPicture.Id, _mediaSettings.ProductThumbPictureSize) : await _pictureService.GetDefaultPictureUrlAsync(_mediaSettings.ProductThumbPictureSize),
                                OtherImage = pictures != null && pictures.Count > 0 ? await pictures.SelectAwait(async x => await _pictureService.GetPictureUrlAsync(x.Id, _mediaSettings.AssociatedProductPictureSize, false)).ToListAsync() : null,
                                ManageStock = true,
                                Inventory = pac.StockQuantity,
                                AllowOutOfStock = pac.AllowOutOfStockOrders,
                                AssociatedProductAttributes = associatedAttributes,
                                AttributeXML = pac.AttributesXml
                            };
                            productList.Add(childProduct);
                        }
                    }
                }
                catch (Exception ex)
                {
                    await _logger.ErrorAsync(ex.Message);
                    throw;
                }
            }

            items.Products = productList;
            items.ProductCount = productList.Count;

            var jsonString = JsonConvert.SerializeObject(items);

            return jsonString;
        }

        public async Task<ElisaCartResponseDto> PrepareElisaCustomCart(ElisaCartDto cartItems)
        {
            if (cartItems == null || cartItems.Items.Count <= 0)
                return null;

            //create custom cart
            var customCart = new CustomCart
            {
                ElisaReference = cartItems.ElisaReference
            };

            await _customCartService.InsertCustomCartAsync(customCart);

            //create elisa cart items
            foreach (var item in cartItems.Items)
            {
                var selectedItems = new CustomCartItems
                {
                    CustomCartId = customCart.ElisaCartId,
                    ProductId = item.Id,
                    ParentProductId = item.ParentId,
                    Quantity = item.Quantity,
                    Price = item.Price > 0 ? item.Price : Convert.ToDecimal((await _productService.GetProductByIdAsync(item.Id))?.Price),
                    AttributeXML = item.AttributeXML
                };

                await _customCartService.InsertCustomCartItemsAsync(selectedItems);
            }

            var response = new ElisaCartResponseDto
            {
                ElisaCartId = customCart.ElisaCartId,
                Url = $"{_webHelper.GetStoreLocation()}elisa/load/" + customCart.ElisaCartId
            };

            return response;
        }

        public async Task<APIResponseDto> LoadItemsIntoShoppingCart(Guid elisaReferenceId)
        {
            APIResponseDto response = new APIResponseDto();
            IList<CustomCartItems> cartItems = new List<CustomCartItems>();
            if (elisaReferenceId != Guid.Empty)
            {
                var sessionId = _httpContextAccessor.HttpContext.Session.Get<Guid>(ElisaPluginDefaults.ElisaCartId);
                if (sessionId == elisaReferenceId)
                {
                    response.IsSuccess = false;
                    await _logger.ErrorAsync("Item already exists in cart...!");
                    response.Errors.Add(new Error { Message = "Item already exists in cart...!" });
                    return response;
                }

                //●	Load all the data that has been store in the DB on the ID of the cart (DDJJSJGJJJFJDJJS)
                var customCart = await _customCartService.GetCustomCartByElisaCartId(elisaReferenceId);
                cartItems = _customCartService.GetCustomCartItemsByCartId(elisaReferenceId);

                if (cartItems.Count == 0)
                {
                    response.IsSuccess = false;
                    await _logger.ErrorAsync(await _localizationService.GetResourceAsync("ShoppingCart.CartIsEmpty"));
                    response.Errors.Add(new Error { Message = await _localizationService.GetResourceAsync("ShoppingCart.CartIsEmpty") });
                    return response;
                }

                if (customCart != null && cartItems != null)
                {
                    //store unique elisa cart Id in session
                    _httpContextAccessor.HttpContext.Session.Set(ElisaPluginDefaults.ElisaCartId, elisaReferenceId);

                    //●	Delete all current products in the customer basket, if any
                    var cart = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), ShoppingCartType.ShoppingCart, _storeContext.GetCurrentStore().Id);
                    cart.ForEach(async x => await _shoppingCartService.DeleteShoppingCartItemAsync(x));
                    //cart.Clear();

                    foreach (var item in cartItems)
                    {
                        var product = await _productService.GetProductByIdAsync(item.ParentProductId > 0 ? item.ParentProductId : item.ProductId);

                        if (product != null)
                        {
                            string attrXml = string.Empty;

                            if (item.ParentProductId > 0)
                            {
                                var productAttributeCombination = (from pac in _productAttributeCombinationRepository.Table
                                                                   where !string.IsNullOrEmpty(item.AttributeXML) ? pac.AttributesXml == item.AttributeXML : pac.Id == item.ProductId
                                                                   select pac).FirstOrDefault();
                                attrXml = productAttributeCombination.AttributesXml;
                            }

                            //now let's try adding product to the cart (now including product attribute validation, etc)
                            var addToCartWarnings = await _shoppingCartService.AddToCartAsync(customer: await _workContext.GetCurrentCustomerAsync(),
                                product: product,
                                shoppingCartType: ShoppingCartType.ShoppingCart,
                                storeId: _storeContext.GetCurrentStore().Id,
                                attributesXml: attrXml,
                                customerEnteredPrice: item.Price > 0 ? item.Price : product.Price,
                                quantity: item.Quantity);

                            if (addToCartWarnings.Any())
                            {
                                //remove custom cart
                                await _customCartService.DeleteCustomCartAsync(customCart);

                                //remove custom cart items
                                await _customCartService.DeleteCustomCartItemsAsync(cartItems);

                                //remove items from shopping cart item table if any
                                var partialCart = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), ShoppingCartType.ShoppingCart, _storeContext.GetCurrentStore().Id);
                                partialCart.ForEach(async x => await _sciRepository.DeleteAsync(x));

                                await _staticCacheManager.ClearAsync();

                                response.IsSuccess = false;
                                addToCartWarnings.ForEach(async x => await _logger.ErrorAsync(x));
                                addToCartWarnings.ForEach(x => response.Errors.Add(new Error { Message = x }));
                                response.Data = product;

                                break;
                            }
                            else
                            {
                                //activity log
                                await _customerActivityService.InsertActivityAsync("PublicStore.AddToShoppingCart",
                                    string.Format(await _localizationService.GetResourceAsync("ActivityLog.PublicStore.AddToShoppingCart"), product.Name), product);

                                response.IsSuccess = true;
                            }
                        }
                        else
                        {
                            response.IsSuccess = false;
                            await _logger.ErrorAsync("Product not found...!");
                            response.Errors.Add(new Error { Message = "Product not found...!" });
                            break;
                        }
                    }
                }
            }
            else
            {
                response.IsSuccess = false;
                response.Errors.Add(new Error { Message = "invalid elisa reference id...!" });
            }

            if (response.IsSuccess)
            {
                if (cartItems.Count > 0)
                    await _customCartService.DeleteCustomCartItemsAsync(cartItems);

                var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);
                response.Data = urlHelper.RouteUrl("ShoppingCart");
            }

            return response;
        }

        public async Task<string> PreapreElisaOrders(string timeStamp)
        {
            if (string.IsNullOrEmpty(timeStamp))
                return null;

            IList<OrderResponseDto> elisaOrders = new List<OrderResponseDto>();
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            DateTime? fromDate = dtDateTime.AddSeconds(Convert.ToDouble(timeStamp)).ToUniversalTime();

            var orders = (from o in _orderRepository.Table
                          where Convert.ToDecimal(timeStamp) > 0 ? o.CreatedOnUtc > fromDate.Value : true &&
                          !_catalogSettings.IgnoreStoreLimitations ? _storeContext.GetCurrentStore().Id == o.StoreId : true
                          select o).ToList();

            foreach (var order in orders)
            {
                var elisaReferenceId = await _genericAttributeService.GetAttributeAsync<Guid>(order, ElisaPluginDefaults.ElisaReference, _storeContext.GetCurrentStore().Id);

                var customCartItems = _customCartService.GetCustomCartItemsByCartId(elisaReferenceId);

                var orderItems = await _orderService.GetOrderItemsAsync(order.Id);

                if (elisaReferenceId != Guid.Empty)
                {
                    var elisaOrder = new OrderResponseDto
                    {
                        OrderId = order.Id,
                        ElisaReference = elisaReferenceId,
                        TimeStemp = timeStamp,
                        OrderAmount = order.OrderTotal,
                        OrderItems = orderItems.Select(oi =>
                        {
                            var pacombination = (from pac in _productAttributeCombinationRepository.Table
                                                 where pac.ProductId == oi.ProductId &&
                                                 pac.AttributesXml == oi.AttributesXml
                                                 select pac).FirstOrDefault();
                            var orderItem = new OrderResponseDto.Products
                            {
                                Id = oi.Id,
                                ProductId = oi.ProductId,
                                AttributeId = pacombination != null ? pacombination.Id : 0,
                                Quantity = oi.Quantity,
                                Price = oi.UnitPriceInclTax,
                            };

                            return orderItem;

                        }).ToList()
                    };

                    elisaOrders.Add(elisaOrder);
                }
            }

            var response = JsonConvert.SerializeObject(elisaOrders);

            return response;
        }
        #endregion
    }
}
