using Nop.Core;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nop.Plugin.API.ElisaIntegration
{
    /// <summary>
    /// Elisa API Integration plugin
    /// </summary>
    public class ElisaPluginProcessor : BasePlugin
    {
        #region Fields
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        #endregion

        #region Ctor
        public ElisaPluginProcessor(ILocalizationService localizationService,
            ISettingService settingService,
            IWebHelper webHelper)
        {
            _localizationService = localizationService;
            _settingService = settingService;
            _webHelper = webHelper;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/ElisaConfiguration/Configure";
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task InstallAsync()
        {
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugin.API.ElisaIntegration.Configuration.Token"] = "Token",
                ["Plugin.API.ElisaIntegration.Configuration.Token.Hint"] = "Token will generate automatically by cliking on generate token button",
                ["Plugin.API.ElisaIntegration.Configuration.GeneratePassword"] = "Generate token",
                ["Plugin.API.ElisaIntegration.Configuration.GenerateAPIToken"] = "Generate API Token"
            });

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task UninstallAsync()
        {
            //settings
            var setting = await _settingService.GetSettingAsync(ElisaPluginDefaults.Token);
            await _settingService.DeleteSettingAsync(setting);

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugin.API.ElisaIntegration");

            await base.UninstallAsync();
        }
        #endregion
    }
}