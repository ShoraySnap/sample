using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnaptrudeManagerUI.API;

namespace SnaptrudeManagerUI.UI.Helpers
{
    internal class LoginHelper
    {
        public static async void Login(object parameter)
        {
            await Task.Run(async () =>
            {
                var ps = new ProcessStartInfo(Urls.Get("snaptrudeReactUrl") + "/login?externalAuth=revit")
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            });
        }
    }
}
