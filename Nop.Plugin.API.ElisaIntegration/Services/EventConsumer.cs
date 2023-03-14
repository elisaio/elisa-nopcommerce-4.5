using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Events;
using Nop.Services.Common;
using Nop.Services.Events;
using System.Threading.Tasks;
using System;
using Nop.Core.Http.Extensions;
using Nop.Services.Logging;

namespace Nop.Plugin.API.ElisaIntegration.Services
{
    public class EventConsumer : IConsumer<EntityInsertedEvent<Order>>
    {
        #region Fields
        private readonly CustomCartService _customCartService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger _logger;
        private readonly IStoreContext _storeContext;
        #endregion

        #region Ctor
        public EventConsumer(CustomCartService customCartService,
            IGenericAttributeService genericAttributeService,
            IHttpContextAccessor httpContextAccessor,
            ILogger logger,
            IStoreContext storeContext)
        {
            _customCartService = customCartService;
            _genericAttributeService = genericAttributeService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _storeContext = storeContext;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Handle order placed event
        /// </summary>
        /// <param name="eventMessage">Event message</param>
        public async Task HandleEventAsync(EntityInsertedEvent<Order> eventMessage)
        {
            if (eventMessage.Entity == null)
                return;

            var order = eventMessage.Entity;
            var elisaCartId = _httpContextAccessor.HttpContext.Session.Get<Guid>(ElisaPluginDefaults.ElisaCartId);
            if (elisaCartId != Guid.Empty)
            {
                var customCart = await _customCartService.GetCustomCartByElisaCartId(elisaCartId);
                if (customCart != null)
                {
                    await _genericAttributeService.SaveAttributeAsync<Guid>(order, ElisaPluginDefaults.ElisaReference, customCart.ElisaCartId, (await _storeContext.GetCurrentStoreAsync()).Id);

                    await _customCartService.DeleteCustomCartAsync(customCart);

                }
            }
        }
        #endregion
    }
}
