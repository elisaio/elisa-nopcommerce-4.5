using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.API.ElisaIntegration.Factories;
using Nop.Services.Messages;
using Nop.Services.Seo;
using Nop.Web.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.API.ElisaIntegration.Controllers
{
    public class ElisaController : BasePluginController
    {
        #region Fields
        private readonly ElisaAPIIntegrationModelFactory _elisaAPIIntegrationModelFactory;
        private readonly INotificationService _notificationService;
        private readonly IUrlRecordService _urlRecordService;

        #endregion

        #region Ctor
        public ElisaController(ElisaAPIIntegrationModelFactory elisaAPIIntegrationModelFactory,
            INotificationService notificationService,
            IUrlRecordService urlRecordService)
        {
            _elisaAPIIntegrationModelFactory = elisaAPIIntegrationModelFactory;
            _notificationService = notificationService;
            _urlRecordService = urlRecordService;
        }

        #endregion

        #region Methods
        [HttpGet]
        public async Task<IActionResult> Load(Guid id)
        {
            var response = await _elisaAPIIntegrationModelFactory.LoadItemsIntoShoppingCart(id);

            if (!response.IsSuccess)
            {
                _notificationService.ErrorNotification(string.Join(", ", response.Errors.Select(x => x.Message)));
                var product = response.Data as Product;
                if (product == null)
                    return RedirectToRoute("Homepage");
                return RedirectToRoute("Product", new { SeName = await _urlRecordService.GetSeNameAsync(product) });

            }

            return Redirect(response.Data);
        }
        #endregion
    }
}
