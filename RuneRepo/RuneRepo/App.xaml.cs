using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace RuneRepo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string name = args.Name.Split(',')[0];

            Assembly assembly = Assembly.GetExecutingAssembly();
            string resource = assembly.GetManifestResourceNames().FirstOrDefault(s => s.EndsWith(name + ".dll"));

            if (string.IsNullOrEmpty(resource))
                return null;

            using (Stream stream = assembly.GetManifestResourceStream(resource))
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                return Assembly.Load(buffer);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
               
            MainWindow mainWindow = new MainWindow();
            mainWindow.LoadConfig();
            mainWindow.Show();
        }
    }
}
