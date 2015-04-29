using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SignalR_example.Startup))]
namespace SignalR_example
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Important MapSignalR
            app.MapSignalR();
        }
    }
}
