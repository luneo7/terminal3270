using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Open3270;
using Open3270.TN3270;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Documents;
using System.Threading;
using System.IO;
using DavyKager;
using log4net;


namespace terminal3270
{
    public class Terminal : INotifyPropertyChanged
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Terminal));

        private readonly BackgroundWorker worker = new BackgroundWorker();

        Open3270.TNEmulator emu = new TNEmulator();
        string screenText;
        bool isConnected;
        bool isConnecting;
        bool enterKeyPressed;
        bool textKeyPressed;

        public bool EnterKeyPressed
        {
            get { return enterKeyPressed; }
        }

        public bool TextKeyPressed
        {
            get { return textKeyPressed; }
        }
        
        public Terminal()
        {
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
        }


        void emu_Disconnected(TNEmulator where, string Reason)
        {
            Disconnect();
            this.IsConnected = false;
            this.IsConnecting = false;
            this.ScreenText = Reason;
        }

        public void Connect()
        {
            emu = new TNEmulator();
            emu.Disconnected += emu_Disconnected;
            emu.CursorLocationChanged += emu_CursorLocationChanged;
            emu.ScreenContentChanged += emu_ScreenContentChanged;

            //Retrieve host settings
            emu.Config.HostName = Properties.Settings.Default.Hostname;
            emu.Config.HostPort = Properties.Settings.Default.HostPort;
            emu.Config.TermType = Properties.Settings.Default.TerminalType;
            emu.Config.UseSSL = Properties.Settings.Default.UseSSL;
            if (!String.IsNullOrEmpty(Properties.Settings.Default.LUName))
            {
                emu.Config.HostLU = Properties.Settings.Default.LUName;
            }
            emu.Config.ThrowExceptionOnLockedScreen = false;
            emu.Config.FastScreenMode = true;

            //Begin the connection process asynchomously
            this.IsConnecting = true;
            //ConnectToHost();
            worker.RunWorkerAsync();
            /*Task.Factory.StartNew(ConnectToHost).ContinueWith((t) =>
                {
                    //Update the display when we are finished connecting
                    this.IsConnecting = false;
                    this.IsConnected = emu.IsConnected;
                    //callRedraw();
                    //this.ScreenText = emu.CurrentScreenObject.Dump();
                    //Refresh();
                });*/
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            ConnectToHost();
        }

        private void worker_RunWorkerCompleted(object sender,
                                               RunWorkerCompletedEventArgs e)
        {
            this.IsConnecting = false;
            this.IsConnected = emu.IsConnected;
            if (this.IsConnected)
            {
                Tolk.Speak("Conectado");
            }
        }

        private void ConnectToHost()
        {
            try
            {
                emu.Connect();
            }
            catch (TNHostException tnHostException)
            {
                log.Warn("Exceção TNHostException em terminal.ConnectToHost", tnHostException);
                emu_Disconnected(emu, tnHostException.Message + " - " + tnHostException.Reason);
            }
            catch (Exception e)
            {
                log.Error("Exceção não esperada em terminal.ConnectToHost", e);
                emu_Disconnected(emu, e.Message);
            }
        }

        public bool IsConnecting
        {
            get
            {
                return this.isConnecting;
            }
            set
            {
                this.isConnecting = value;
                this.OnPropertyChanged("IsConnecting");
            }
        }


        public bool IsTerminalConnected
        {
            get
            {
                return this.emu.IsConnected;
            }
        }

        /// <summary>
        /// Indicates when the terminal is connected to the host.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return this.isConnected;
            }
            set
            {
                this.isConnected = value;
                this.OnPropertyChanged("IsConnected");
            }
        }


        /// <summary>
        /// This is the text buffer to display.
        /// </summary>
        public string ScreenText
        {
            get
            {
                return this.screenText;
            }
            set
            {
                this.screenText = value;
                this.OnPropertyChanged("ScreenText");
            }
        }



        int caretIndex;

        public int CaretIndex
        {
            get
            {
                return this.caretIndex;
            }
            set
            {
                this.caretIndex = value;
                this.OnPropertyChanged("CaretIndex");
            }
        }




        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion INotifyPropertyChanged



        /// <summary>
        /// Sends text to the terminal.
        /// This is used for typical alphanumeric text entry.
        /// </summary>
        /// <param name="text">The text to send</param>
        internal void SendText(string text, bool automated = false)
        {
            try
            {
                if (this.emu.IsConnected)
                {
                    enterKeyPressed = false;
                    textKeyPressed = true;
                    if (!automated)
                    {
                        ScreenField xf = GetCurrentField();
                        if (xf.Attributes.FieldType == "Hidden" && xf.Attributes.Protected == false)
                        {
                            Tolk.Silence();
                            Tolk.Speak("", true);
                        }
                    }
                    this.emu.SetText(text);
                    if (!automated)
                    {
                        Refresh();
                        textKeyPressed = false;
                    }
                }
            }
            catch (TNHostException tnHostException)
            {
                log.Warn("Exceção TNHostException em terminal.SendText", tnHostException);
            }
            catch (Exception e)
            {
                log.Error("Exceção não esperada em terminal.Sendtext", e);
                emu_Disconnected(emu, e.Message);
            }
        }

        public void SpeekCurrentFieldLabel()
        {
            if (this.emu.IsConnected)
            {
                ScreenField xf = null;
                xf = GetCurrentField();
                if (xf != null)
                {
                    ScreenField xf1 = GetPreviousField(xf);
                    if (xf1 != null && !String.IsNullOrEmpty(xf1.Text.Trim()) && xf1.Location.top == xf.Location.top)
                    {
                        Tolk.Speak(xf1.Text.Trim().Replace("|", ""));
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(ScreenText))
                        {
                            ScreenField xf2 = GetNextUnprotectedField(xf);
                            if (xf2 != null && xf2.Location.top == xf.Location.top)
                            {
                                Tolk.Speak(ScreenText.Substring(xf.Location.position + xf.Location.top + xf.Location.length, (xf2.Location.position + xf2.Location.top) - (xf.Location.position + xf.Location.top + xf.Location.length)).Replace("|", ""));
                            }
                            else
                            {
                                Tolk.Speak(ScreenText.Substring(xf.Location.position + xf.Location.top + xf.Location.length, 80 - (xf.Location.left + xf.Location.length)).Trim().Replace("|", ""));
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Sends a character to the terminal.
        /// This is used for special characters like F1, Tab, et cetera.
        /// </summary>
        /// <param name="key">The key to send.</param>
        public void SendKey(TnKey key)
        {
            try
            {
                if (this.emu.IsConnected)
                {
                    enterKeyPressed = key == TnKey.Enter;
                    textKeyPressed = false;

                    this.emu.SendKey(true, key, 2000);

                    if (key == TnKey.Tab || key == TnKey.BackTab)
                    {
                        UpdateCaretIndex();
                        //	this.Refresh();
                    }
                    else if (key == TnKey.Erase || key == TnKey.Delete)
                    {
                        this.Refresh();
                    }
                }
            }
            catch (TNHostException tnHostException)
            {
                log.Warn("Exceção TNHostException em terminal.SendKey", tnHostException);
            }
            catch (Exception e)
            {
                log.Error("Exceção não esperada em terminal.SendKey", e);
                emu_Disconnected(emu, e.Message);
            }
        }

        private static TextPointer GetPoint(FlowDocument document, int x)
        {
            TextPointer ret = document.ContentStart;
            int i = 0;

            while (ret != null && i < x)
            {
                if (ret.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    String textRun = ret.GetTextInRun(LogicalDirection.Forward);
                    i += textRun.Length;
                    ret = ret.GetPositionAtOffset(textRun.Length);
                }
                else
                {
                    ret = ret.GetNextInsertionPosition(LogicalDirection.Forward);
                    i++;
                }
            }
            return ret == null ? document.ContentEnd : ret;
        }

        public void callRedraw()
        {
            /*  Thread thread = new Thread(Redraw);
              thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
              thread.Start();
              thread.Join();*/
        }

        public FlowDocument Reredraw()
        {
            string[] lines = this.emu.CurrentScreenObject.Dump().Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            FlowDocument flwd = new FlowDocument();
            flwd.Foreground = Brushes.LimeGreen;

            int j = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                List<Run> rs = new List<Run>();
                rs.Add(new Run(lines[i]));
                if (this.emu.CurrentScreenObject.Fields != null && this.emu.CurrentScreenObject.Fields.Count > 0)
                {
                    while (j < this.emu.CurrentScreenObject.Fields.Count && ((this.emu.CurrentScreenObject.Fields[j].Location.position + this.emu.CurrentScreenObject.Fields[j].Location.top) <= ((i + 1) * 80)))
                    {
                        if (emu.CurrentScreenObject.Fields[j].Text != null)
                        {
                            int index = rs[rs.Count - 1].Text.IndexOf(emu.CurrentScreenObject.Fields[j].Text);
                            if (index >= 0)
                            {
                                Brush clr = Brushes.Lime;
                                if (emu.CurrentScreenObject.Fields[j].Attributes.FieldType == "High" && emu.CurrentScreenObject.Fields[j].Attributes.Protected)
                                    clr = Brushes.White;
                                else if (emu.CurrentScreenObject.Fields[j].Attributes.FieldType == "High")
                                    clr = Brushes.Red;
                                else if (emu.CurrentScreenObject.Fields[j].Attributes.Protected)
                                    clr = Brushes.RoyalBlue;
                                else if (emu.CurrentScreenObject.Fields[j].Attributes.FieldType == "Hidden")
                                    clr = Brushes.Black;

                                string before = null;
                                string after = null;
                                if (index > 0)
                                    before = rs[rs.Count - 1].Text.Substring(0, index);
                                if (index + emu.CurrentScreenObject.Fields[j].Location.length < 80)
                                    after = rs[rs.Count - 1].Text.Substring(index + this.emu.CurrentScreenObject.Fields[j].Location.length);
                                string text = rs[rs.Count - 1].Text.Substring(index, this.emu.CurrentScreenObject.Fields[j].Location.length);

                                if (before != null)
                                {
                                    rs[rs.Count - 1].Text = before;
                                    Run t = new Run(text);
                                    t.Foreground = clr;
                                    rs.Add(t);
                                }
                                else
                                {
                                    rs[rs.Count - 1].Text = text;
                                    rs[rs.Count - 1].Foreground = clr;
                                }

                                if (after != null)
                                {
                                    rs.Add(new Run(after));
                                }
                            }
                        }
                        j++;
                    };

                }
                Paragraph p = new Paragraph();

                for (int k = 0; k < rs.Count; k++)
                {
                    p.Inlines.Add(rs[k]);
                }

                flwd.Blocks.Add(p);
            }

            return flwd;
        }

        /// <summary>
        /// Forces a refresh and updates the screen display
        /// </summary>
        public void Refresh()
        {
            this.Refresh(100);
        }

        public void Disconnect()
        {
            this.emu.Dispose();
        }


        /// <summary>
        /// Forces a refresh and updates the screen display
        /// </summary>
        /// <param name="screenCheckInterval">This is the speed in milliseconds at which the library will poll 
        /// to determine if we have a valid screen of data to display.</param>
        public void Refresh(int screenCheckInterval)
        {
            try
            {
                //This line keeps checking to see when we've received a valid screen of data from the mainframe.
                //It loops until the TNEmulator.Refresh() method indicates that waiting for the screen did not time out.
                //This helps prevent blank screens, etc.
                this.emu.Refresh(true, screenCheckInterval);

                string content = this.emu.CurrentScreenObject.Dump();
                if (content != null && content != this.ScreenText)
                {
                    this.ScreenText = this.emu.CurrentScreenObject.Dump();
                }

                //callRedraw();
            }
            catch (TNHostException tnHostException)
            {
                log.Warn("Exceção TNHostException em terminal.Refresh", tnHostException);
            }
            catch (Exception e)
            {
                log.Error("Exceção não esperada em terminal.Refresh", e);
                emu_Disconnected(emu, e.Message);
            }
        }

        public ScreenField GetPreviousUnprotectedField(ScreenField xf)
        {
            if (this.emu.CurrentScreenObject.Fields != null)
            {
                for (int i = emu.CurrentScreenObject.Fields.IndexOf(xf) - 1; i > 0; i--)
                {
                    if (emu.CurrentScreenObject.Fields[i].Attributes.Protected == false && (emu.CurrentScreenObject.Fields[i].Location.position + emu.CurrentScreenObject.Fields[i].Location.top) < (xf.Location.position + xf.Location.top))
                    {
                        return emu.CurrentScreenObject.Fields[i];
                    }
                }
            }
            return null;
        }

        public ScreenField GetNextUnprotectedField(ScreenField xf)
        {
            if (this.emu.CurrentScreenObject.Fields != null)
            {
                for (int i = emu.CurrentScreenObject.Fields.IndexOf(xf) + 1; i < emu.CurrentScreenObject.Fields.Count; i++)
                {
                    if (emu.CurrentScreenObject.Fields[i].Attributes.Protected == false && (emu.CurrentScreenObject.Fields[i].Location.position + emu.CurrentScreenObject.Fields[i].Location.top) > (xf.Location.position + xf.Location.top))
                    {
                        return emu.CurrentScreenObject.Fields[i];
                    }
                }
            }
            return null;
        }


        public ScreenField GetPreviousField(ScreenField xf)
        {
            if (this.emu.CurrentScreenObject.Fields != null)
            {
                for (int i = emu.CurrentScreenObject.Fields.IndexOf(xf) - 1; i > 0; i--)
                {
                    if (emu.CurrentScreenObject.Fields[i].Attributes.Protected == true && emu.CurrentScreenObject.Fields[i].Text != null && (emu.CurrentScreenObject.Fields[i].Location.position + emu.CurrentScreenObject.Fields[i].Location.top) < (xf.Location.position + xf.Location.top))
                    {
                        return emu.CurrentScreenObject.Fields[i];
                    }
                }
            }
            return null;
        }

        public ScreenField GetNextField(ScreenField xf)
        {
            if (this.emu.CurrentScreenObject.Fields != null)
            {
                for (int i = emu.CurrentScreenObject.Fields.IndexOf(xf) + 1; i < emu.CurrentScreenObject.Fields.Count; i++)
                {
                    if (emu.CurrentScreenObject.Fields[i].Text != null && emu.CurrentScreenObject.Fields[i].Attributes.Protected == true && (emu.CurrentScreenObject.Fields[i].Location.position + emu.CurrentScreenObject.Fields[i].Location.top) > (xf.Location.position + xf.Location.top))
                    {
                        return emu.CurrentScreenObject.Fields[i];
                    }

                }
            }
            return null;
        }

        public ScreenField GetCurrentField()
        {
            if (this.emu.CurrentScreenObject.Fields != null)
            {
                int caretIndex = GetCaretIndex();
                return this.emu.CurrentScreenObject.Fields.Where(c => caretIndex >= (c.Location.position + c.Location.top) && caretIndex <= (c.Location.position + c.Location.top + c.Location.length)).FirstOrDefault<ScreenField>();

            }
            return null;
        }

        public int GetCaretIndex()
        {
            if (emu.IsConnected)
                return this.emu.CursorY * 81 + this.emu.CursorX;

            return 0;
        }

        public void UpdateCaretIndex()
        {
            this.CaretIndex = this.emu.CursorY * 81 + this.emu.CursorX;
            SpeekCurrentFieldLabel();
        }

        void emu_CursorLocationChanged(object sender, EventArgs e)
        {
            //this.UpdateCaretIndex();
        }

        void emu_ScreenContentChanged(object sender, EventArgs e)
        {
            this.Refresh();
        }

        /// <summary>
        /// Sends field information to the debug console.
        /// This can be used to define well-known field positions in your application.
        /// </summary>
        internal void DumpFillableFields()
        {
            string output = string.Empty;

            ScreenField field;

            for (int i = 0; i < this.emu.CurrentScreenObject.Fields.Count; i++)
            {
                field = this.emu.CurrentScreenObject.Fields[i];
                if (!field.Attributes.Protected)
                {
                    Debug.WriteLine(string.Format("public static int fieldName = {0};   //{1},{2}  Length:{3}   {4}", i, field.Location.top + 1, field.Location.left + 1, field.Location.length, field.Text));
                }
            }
        }
    }
}
