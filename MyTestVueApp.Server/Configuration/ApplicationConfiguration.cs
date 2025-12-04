namespace MyTestVueApp.Server.Configuration
{
    /// <summary>
    /// MUST match appsettings.json
    /// </summary>
    public class ApplicationConfiguration
    {
        public string ConnectionString { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUrl { get; set; }

    }
}
