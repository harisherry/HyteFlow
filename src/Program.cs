using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace HyteFlow
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
            try
            {
                RunApplication(args);
            }
            catch (Exception ex)
            {
                try
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hyteflow_error.log"), ex.ToString());
                }
                catch { }
            }
        }

        private static void RunApplication(string[] args)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(baseDir, "hyteflow_config.json");

            bool startMinimized = args != null && args.Any(a => string.Equals(a, "--minimized", StringComparison.OrdinalIgnoreCase));
            string screenshotPath = null;
            if (args != null)
            {
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (string.Equals(args[i], "--screenshot", StringComparison.OrdinalIgnoreCase))
                        screenshotPath = args[i + 1];
                }
            }

            var engine = new HardwareEngine(configPath);
            engine.Initialize();

            var app = new Application();
            var win = new MainWindow(engine, startMinimized);

            if (!string.IsNullOrEmpty(screenshotPath))
            {
                win.Loaded += (s, e) =>
                {
                    var captureTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
                    captureTimer.Tick += (ts, te) =>
                    {
                        captureTimer.Stop();
                        try
                        {
                            int w = (int)Math.Max(1360, win.ActualWidth);
                            int h = (int)Math.Max(860, win.ActualHeight);
                            var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                            rtb.Render(win);
                            var enc = new PngBitmapEncoder();
                            enc.Frames.Add(BitmapFrame.Create(rtb));
                            using (var fs = File.Create(screenshotPath))
                            {
                                enc.Save(fs);
                            }
                        }
                        catch (Exception ex)
                        {
                            File.WriteAllText(Path.Combine(baseDir, "hyteflow_error.log"), ex.ToString());
                        }
                        win.Close();
                    };
                    captureTimer.Start();
                };
            }

            app.Run(win);
        }

        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            string name = new AssemblyName(args.Name).Name;
            if (name == "FanControl.CorsairLink.merged")
                name = "FanControl.CorsairLink";

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string libCandidate = Path.Combine(baseDir, "lib", name + ".dll");
            if (File.Exists(libCandidate))
                return Assembly.LoadFrom(libCandidate);

            string rootCandidate = Path.Combine(baseDir, name + ".dll");
            if (File.Exists(rootCandidate))
                return Assembly.LoadFrom(rootCandidate);

            return null;
        }
    }
}
