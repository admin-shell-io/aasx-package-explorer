using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AdminShellNS;
using AnyUi;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlazorUI
{
    public class Program
    {
        public static AdminShellPackageEnv env = null;

        public static AnyUiStackPanel stack = new AnyUiStackPanel();

        public static string LogLine = "Started..";

        public class BlazorDisplayData : AnyUiDisplayDataBase
        {
            public Action<object> MyLambda;

            public BlazorDisplayData() { }

            public BlazorDisplayData(Action<object> lambda)
            {
                MyLambda = lambda;
            }
        }

        public static void Main(string[] args)
        {
            env = new AdminShellPackageEnv("Example_AAS_ServoDCMotor_21.aasx");

            //
            // Test for Blazor
            //
            var editMode = true;
            stack.Orientation = AnyUiOrientation.Vertical;

            if (true)
            {
                var lab = new AnyUiLabel();
                lab.Content = "Hallo";
                lab.Foreground = AnyUiBrushes.DarkBlue;
                stack.Children.Add(lab);

                //var stck2 = new AnyUiStackPanel();
                //stck2.Orientation = AnyUiOrientation.Horizontal;
                //stack.Children.Add(stck2);

                if (editMode)
                {
                    var tb = new AnyUiTextBox();
                    tb.Foreground = AnyUiBrushes.Black;
                    tb.Text = "Initial";
                    stack.Children.Add(tb);
                    //repo.RegisterControl(tb, (o) =>
                    //{
                    //    Log.Singleton.Info($"Text changed to .. {"" + o}");
                    //    return new ModifyRepo.LambdaActionNone();
                    //});

                    var btn = new AnyUiButton();
                    btn.Content = "Click me!";
                    btn.DisplayData = new BlazorDisplayData(lambda: (o) =>
                    {
                        if (o == btn)
                            Program.LogLine = "Hallo, Match zwischen Button und callback!";
                    });
                    stack.Children.Add(btn);
                    //repo.RegisterControl(btn, (o) =>
                    //{
                    //    Log.Singleton.Error("Button clicked!");
                    //    return new ModifyRepo.LambdaActionRedrawAllElements(null);
                    //});
                }
            }

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
