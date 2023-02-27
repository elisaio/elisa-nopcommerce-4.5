using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.API.ElisaIntegration.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using System.Threading.Tasks;

namespace Nop.Plugin.API.ElisaIntegration.Controllers
{
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    public class ElisaConfigurationController : BasePluginController
    {
        #region Fields

        private readonly IEncryptionService _encryptionService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;

        #endregion

        #region Ctor
        public ElisaConfigurationController(IEncryptionService encryptionService,
            ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStoreContext storeContext)
        {
            _encryptionService = encryptionService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _settingService = settingService;
            _storeContext = storeContext;
        }

        #endregion

        #region Methods
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var token = await _settingService.GetSettingByKeyAsync<string>(ElisaPluginDefaults.Token);

            var model = new ConfigurationModel()
            {
                Token = token
            };

            return View("~/Plugins/API.ElisaIntegration/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            if (string.IsNullOrEmpty(model.Token))
            {
                model.Token = _encryptionService.EncryptText(CommonHelper.GenerateRandomDigitCode(10));
            }

            //load settings for a chosen store scope
            await _settingService.SetSettingAsync<string>(ElisaPluginDefaults.Token, model.Token);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }
        #endregion
    }
}
