using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Uptime.Services;

namespace Uptime
{
    public class Post
    {
        public static async Task Status(HttpContext ctx)
        {
            using var body = new MemoryStream();
            await ctx.Request.Body.CopyToAsync(body);
            body.Seek(0, SeekOrigin.Begin);
            var rawData = Encoding.UTF8.GetString(body.ToArray());
            var data = JObject.Parse(rawData);

            var statusService = ctx.RequestServices.GetRequiredService<StatusService>();
            statusService.AddService(new ServiceMetadata
            {
                ServiceName = data["name"].Value<string>(),
                Hostname = ctx.Request.Host.Host + data["port"].Value<string>(),
            });
        }
    }
}
