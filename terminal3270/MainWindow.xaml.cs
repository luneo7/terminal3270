using DavyKager;
using FirstFloor.ModernUI.Windows.Controls;
using Open3270;
using Open3270.TN3270;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace terminal3270
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {
        public Terminal Terminal
        {
            get { return (this.Resources["term"] as Terminal); }
        }

        public LoginAcesso Login
        {
            get { return (this.Resources["logacesso"] as LoginAcesso); }
        }

        public MainWindow()
        {
            InitializeComponent();
            //This odd event handler is needed because the TextBox control eats that spacebar, so we have to intercept an already-handled event.
            //this.Console.AddHandler(TextBox.KeyDownEvent, new KeyEventHandler(Console_KeyDown), true);
            this.Console.AddHandler(TextBox.PreviewKeyDownEvent, new KeyEventHandler(Console_KeyDown), true);
            Tolk.Load();
            Tolk.TrySAPI(false);
            Tolk.DetectScreenReader();
            Tolk.Speak("Bem-vindo ao terminal 3270!", true);
            Tolk.Speak("Pressione Alt + E para conectar, ou Alt + Q para sair.");

        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Tolk.Unload();
            }
            catch { }
        }

        #region Login

        public void DoLogin()
        {
            String usuario = "";
            String senha = "";
            // create the dialog content
            TextBox content = new TextBox();
            content.Text = "";

            // create the ModernUI dialog component with the buttons
            ModernDialog dlg = new ModernDialog
            {
                Title = "Nome de Usuário",
                Content = content,
                MinHeight = 0,
                MinWidth = 0,
                MaxHeight = 480,
                MaxWidth = 640,
            };
            dlg.OkButton.Content = "Avançar";
            dlg.CancelButton.Content = "Cancelar";
            dlg.Buttons = new Button[] { dlg.OkButton, dlg.CancelButton };

            dlg.OkButton.Click += (object sender, RoutedEventArgs e) =>
            {
                usuario = content.Text;
            };

            dlg.Activated += (object sender, EventArgs e) =>
            {
                content.Focus();
            };

            bool? resultado = dlg.ShowDialog();
            if (resultado.HasValue && resultado.Value)
            {
                PasswordBox pwdBox = new PasswordBox();
                dlg = new ModernDialog
                {
                    Title = "Senha",
                    Content = pwdBox,
                    MinHeight = 0,
                    MinWidth = 0,
                    MaxHeight = 480,
                    MaxWidth = 640,
                };
                dlg.OkButton.Content = "Avançar";
                dlg.CancelButton.Content = "Cancelar";
                dlg.Buttons = new Button[] { dlg.OkButton, dlg.CancelButton };

                dlg.Activated += (object sender, EventArgs e) =>
                {
                    pwdBox.Focus();
                };

                dlg.OkButton.Click += (object sender, RoutedEventArgs e) =>
                {
                    senha = pwdBox.Password;
                };
                resultado = dlg.ShowDialog();

                if (resultado.HasValue && resultado.Value)
                {
                    RetornoLogin ret = Login.RealizarLogin(usuario, senha);
                    if (ret.Sucesso)
                    {
                        Properties.Settings.Default.LUName = ret.LU;                        
                        this.Terminal.Connect();
                        this.Console.Focus();
                    }
                    else
                    {
                        MessageBoxResult retorno = ModernDialog.ShowMessage(ret.Mensagem, "Atenção", MessageBoxButton.OK);                        
                        DoLogin();
                    }
                }
            }
        }

        #endregion

        #region SendText Command

        public static RoutedUICommand SendText = new RoutedUICommand();

        void CanExecuteSendText(object sender, CanExecuteRoutedEventArgs args)
        {
            if (true)
            {
                args.CanExecute = true;
            }
            else
            {
                //args.CanExecute = false;
            }
        }


        void ExecuteSendText(object sender, ExecutedRoutedEventArgs args)
        {

        }

        #endregion SendText Command

        #region SpeakCommand Command

        public static RoutedUICommand SpeakCommand = new RoutedUICommand();

        void CanExecuteSpeakCommand(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = this.Terminal.IsConnected;
        }


        void ExecuteSpeakCommand(object sender, ExecutedRoutedEventArgs args)
        {
            ScreenField xf = null;

            switch ((String)args.Parameter)
            {
                case "A":
                    Tolk.Speak(Console.Text.Trim(), true);
                    break;
                case "S":
                    Tolk.Silence();
                    break;
                case "D":
                    Tolk.Speak(Console.Text.Substring((Terminal.GetCaretIndex() / 81) * 81, 80).Trim().Replace("|", ""));
                    break;
                case "X":
                    xf = Terminal.GetCurrentField();
                    if (xf != null)
                    {
                        Tolk.Speak(Console.Text.Substring(xf.Location.position + xf.Location.top, xf.Location.length).Trim().Replace("|", ""));
                    }
                    break;
                case "Z":
                    xf = Terminal.GetCurrentField();
                    if (xf != null)
                    {
                        ScreenField xf1 = Terminal.GetPreviousField(xf);
                        if (xf1 != null && xf1.Text != null && xf1.Location.top == xf.Location.top)
                        {
                            Tolk.Speak(xf1.Text.Trim().Replace("|", ""));
                        }
                        else
                        {
                            ScreenField xf2 = Terminal.GetNextUnprotectedField(xf);
                            if (xf2 != null && xf2.Location.top == xf.Location.top)
                            {
                                Tolk.Speak(Console.Text.Substring(xf.Location.position + xf.Location.top + xf.Location.length, (xf2.Location.position + xf2.Location.top) - (xf.Location.position + xf.Location.top + xf.Location.length)).Replace("|", ""));
                            }
                            else
                            {
                                Tolk.Speak(Console.Text.Substring(xf.Location.position + xf.Location.top + xf.Location.length, 80 - (xf.Location.left + xf.Location.length)).Trim().Replace("|", ""));
                            }
                        }
                    }
                    break;
            };
        }


        #endregion

        #region GeneralCommand Commnand

        public static RoutedUICommand GeneralCommand = new RoutedUICommand();

        void CanExecuteGeneralCommand(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
        }


        void ExecuteGeneralCommand(object sender, ExecutedRoutedEventArgs args)
        {
            if (args.Parameter.GetType() == typeof(String))
            {
                if ((String)args.Parameter == "Q")
                {
                    this.Terminal.Disconnect();
                    this.Close();
                    return;
                }
                else if ((String)args.Parameter == "E")
                {
                    if (!this.Terminal.IsConnected && !this.Terminal.IsConnecting)
                    {
                        Tolk.Speak("Conectando");
                        DoLogin();
                    }
                    else
                    {
                        Tolk.Speak("Já conectado");
                    }
                }
            }
           
        }

        #endregion

        #region SendCommand Command

        public static RoutedUICommand SendCommand = new RoutedUICommand();

        void CanExecuteSendCommand(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = this.Terminal.IsConnected;
        }


        void ExecuteSendCommand(object sender, ExecutedRoutedEventArgs args)
        {     
            if ((TnKey)args.Parameter == TnKey.Enter || (TnKey)args.Parameter == TnKey.F3)
            {
                Console.CaretIndex = 0;
            }
            this.Terminal.SendKey((TnKey)args.Parameter);
        }

        #endregion SendCommand Command


        #region Connect Command

        public static RoutedUICommand Connect = new RoutedUICommand();

        void CanExecuteConnect(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = !this.Terminal.IsConnected && !this.Terminal.IsConnecting;
        }


        void ExecuteConnect(object sender, ExecutedRoutedEventArgs args)
        {
            DoLogin();
        }

        #endregion Connect Command


        #region Refresh Command

        public static RoutedUICommand Refresh = new RoutedUICommand();

        void CanExecuteRefresh(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = this.Terminal.IsConnected;
        }


        void ExecuteRefresh(object sender, ExecutedRoutedEventArgs args)
        {
            this.Terminal.Refresh();
        }

        #endregion Refresh Command


        #region DumpFields Command

        public static RoutedUICommand DumpFields = new RoutedUICommand();

        void CanExecuteDumpFields(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = this.Terminal.IsConnected;
        }


        void ExecuteDumpFields(object sender, ExecutedRoutedEventArgs args)
        {
            this.Terminal.DumpFillableFields();
        }

        #endregion DumpFields Command


        #region OpenSettings Command

        public static RoutedUICommand OpenSettings = new RoutedUICommand();

        void CanExecuteOpenSettings(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
        }


        void ExecuteOpenSettings(object sender, ExecutedRoutedEventArgs args)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();            
        }

        #endregion OpenSettings Command


        private void Window_KeyDown(object sender, KeyEventArgs e)
        {

        }


        private void Window_TextInput(object sender, TextCompositionEventArgs e)
        {
            if (this.Terminal.IsConnected)
            {
                this.Terminal.SendText(e.Text);
            }
        }


        private void Console_KeyDown(object sender, KeyEventArgs e)
        {
            //The textbox eats several keystrokes, so we can't handle them from keybindings/commands.
            if (this.Terminal.IsConnected)
            {
                switch (e.Key)
                {
                    case Key.Space:
                        {
                            this.Terminal.SendText(" ");
                            break;
                        }
                    case Key.Left:
                        {
                            //this.Terminal.SendKey(TnKey.Left);
                            e.Handled = true;
                            break;
                        }
                    case Key.Right:
                        {
                            //this.Terminal.SendKey(TnKey.Right);
                            e.Handled = true;
                            break;
                        }
                    case Key.Up:
                        {
                            //this.Terminal.SendKey(TnKey.Up);
                            e.Handled = true;
                            break;
                        }
                    case Key.Down:
                        {
                            //this.Terminal.SendKey(TnKey.Down);
                            e.Handled = true;
                            break;
                        }
                    case Key.Back:
                        {
                            this.Terminal.SendKey(TnKey.Erase);
                            break;
                        }
                    case Key.Delete:
                        {
                            this.Terminal.SendKey(TnKey.Delete);
                            break;
                        }
                    default:
                        break;
                }
            }
        }
    }
}
