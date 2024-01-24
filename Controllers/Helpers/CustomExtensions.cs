using System.Reflection;
using System.Text.Json;

namespace SimpleKeybindProxy.Controllers.Helpers
{
    public static partial class CustomExtensions
    {

        // Determines the correct type for the request json string
        public static object? FromJson(this object RequestToProcess)
        {
            if (RequestToProcess == null)
            {
                return null;
            }

            JsonElement Request = (JsonElement)RequestToProcess;

            object ret = new object();
            var t = Request.EnumerateObject();

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(a => a.Namespace.Equals("SimpleKeybindProxy.Models.SocketRequest.Commands") && a.Name.EndsWith("Request")))
            {
                if (t.Any(a => a.Name.ToLower().Equals("command") && a.Value.GetString().ToLower().Equals(type.Name.ToLower().Replace("request", ""))))
                {
                    // This type matches the command requested
                    var ds = JsonSerializer.Deserialize(Request, type);
                    return ds;
                }
            }
            return ret;
        }
    }
}
