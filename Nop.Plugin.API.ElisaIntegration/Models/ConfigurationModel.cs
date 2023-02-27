using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.API.ElisaIntegration.Models
{
    public class ConfigurationModel
    {
        [NopResourceDisplayName("Plugin.API.ElisaIntegration.Configuration.Token")]
        public string Token { get; set; }
    }
}
