using FirstFloor.ModernUI.Windows.Controls;
using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;


[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace terminal3270
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(App));
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {            
            log.Error("Ocorreu um erro inesperado na aplicação que ocasionou seu fechamento", e.Exception);
            ModernDialog.ShowMessage("Ocorreu um erro inesperado, a aplicação se encerrará!", "Atenção", MessageBoxButton.OK);
            e.Handled = true;            
            Application.Current.Shutdown();
        }
    }
}
