using DavyKager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace terminal3270
{
    class AttachedProperties
    {
        #region Caret Location

        public static readonly DependencyProperty CaretLocationProperty =
            DependencyProperty.RegisterAttached("CaretLocation", typeof(int), typeof(AttachedProperties), new PropertyMetadata(new PropertyChangedCallback(CaretChanged)));

        public static int GetCaretLocation(DependencyObject obj)
        {
            return (int)obj.GetValue(CaretLocationProperty);
        }

        public static void SetCaretLocation(DependencyObject obj, int value)
        {
            obj.SetValue(CaretLocationProperty, value);
        }

        private static void CaretChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //We dispatch here in order to let the textbox finish its processing before we try to change the cursor position
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                TextBox tb = d as TextBox;
                if (tb != null)
                {
                    tb.CaretIndex = (int)e.NewValue;
                    tb.SelectionStart = (int)e.NewValue;
                    tb.SelectionLength = 0;
                }
            }));

        }

        #endregion

        #region Non Intrusive Text

        private static bool ShouldOnlyExecuteOnceExecuted = false;
        private static readonly object Locker = new object();

        public static string GetNonIntrusiveText(DependencyObject obj)
        {
            return (string)obj.GetValue(NonIntrusiveTextProperty);
        }
        public static void SetNonIntrusiveText(DependencyObject obj, string value)
        {
            obj.SetValue(NonIntrusiveTextProperty, value);
        }
        public static readonly DependencyProperty NonIntrusiveTextProperty =
        DependencyProperty.RegisterAttached(
                        "NonIntrusiveText",
                        typeof(string),
                        typeof(AttachedProperties),
        new FrameworkPropertyMetadata(
                            null,
                            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                            NonIntrusiveTextChanged));


        public static void NonIntrusiveTextChanged(
                     object sender,
                     DependencyPropertyChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;
            Terminal t = (Terminal)App.Current.MainWindow.Resources["term"];
            LoginAcesso ta = (LoginAcesso)App.Current.MainWindow.Resources["logacesso"];

            int oldCaretIndex = textBox.CaretIndex;

            string newValue = (string)e.NewValue;

            if (newValue.Contains("TELA 001") && newValue.Contains("CODIGO") && newValue.Contains("SENHA"))
            {
                if (!ShouldOnlyExecuteOnceExecuted)
                {
                    lock (Locker)
                    {
                        if (!ShouldOnlyExecuteOnceExecuted)
                        {
                            if (t.IsTerminalConnected)
                            {
                                t.SendText(ta.Usuario, true);
                                t.SendText(ta.Senha, true);
                                t.SendKey(Open3270.TnKey.Enter);
                                ShouldOnlyExecuteOnceExecuted = true;
                            }
                        }
                    }

                }
                return;
            }

            if (newValue.Contains("desconect"))
            {
                ShouldOnlyExecuteOnceExecuted = false;
            }

            if (!String.IsNullOrEmpty(newValue.Trim()))
            {
                if (!String.IsNullOrEmpty(textBox.Text))
                {
                    if (newValue.Length >= 24 * 80 && textBox.Text.Length >= 24 * 80)
                    {
                        for (int i = 0; i < 24; i++)
                        {
                            String tAnterior = textBox.Text.Substring(i * 81, 80);
                            String tNova = newValue.Substring(i * 81, 80);
                            if (!tAnterior.Equals(tNova))
                            {
                                int maxindex = tAnterior.Length == 0 || tNova.Length == 0 ? 0 : (tAnterior.Length < tNova.Length ? tAnterior.Length : tNova.Length) - 1;
                                int inicio = 0;
                                while (tAnterior[inicio] == tNova[inicio])
                                {
                                    inicio++;
                                }

                                int fim = maxindex;
                                while (tAnterior[fim] == tNova[fim])
                                {
                                    fim--;
                                }

                                string novoConteudo = (tNova.Substring(inicio, fim + 1 - inicio)).Replace("|", "").Trim();
                                if (!String.IsNullOrEmpty(novoConteudo) && !t.TextKeyPressed)
                                {
                                    if (!String.IsNullOrEmpty(novoConteudo.Replace("O", "").Trim()))
                                        Tolk.Speak(novoConteudo);
                                }
                            }
                        }


                        string avisoNovo = newValue.Substring(22 * 81, 80).Trim();
                        string avisoAntigo = textBox.Text.Substring(22 * 81, 80).Trim();
                        if (!String.IsNullOrEmpty(avisoNovo) && avisoNovo == avisoAntigo && t.EnterKeyPressed)
                        {
                            Tolk.Speak(avisoAntigo);
                        }
                    }
                    else
                    {
                        Tolk.Speak(newValue);
                    }
                }
                else
                {
                    Tolk.Speak(newValue);
                }
            }
            textBox.Text = (string)e.NewValue;
            textBox.CaretIndex = t.GetCaretIndex();
            if (oldCaretIndex / 81 != textBox.CaretIndex / 81)
            {
                t.SpeekCurrentFieldLabel();
            }

        }

        #endregion
    }
}
