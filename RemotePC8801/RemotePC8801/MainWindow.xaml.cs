﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.IO.Ports;

namespace RemotePC8801
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            _navi = this.MainFrame.NavigationService;
        }

        private Uri pageStart = new Uri("PageStart.xaml", UriKind.Relative);
        private Uri pageDirectCommand = new Uri("PageDirectCommand.xaml", UriKind.Relative);
        private Uri pageSector = new Uri("PageSector.xaml", UriKind.Relative);

        private NavigationService _navi;

        enum ResultStatusMarker {
            CommandEnd,
            ShowResult
        };

        private StringBuilder lastLineBuffer = new StringBuilder();
        private StringBuilder currentLineBuffer = new StringBuilder();
        private AutoResetEvent waiter = new AutoResetEvent(false);
        private ResultStatusMarker result;

        private void appendLog(string s)
        {
            TextBoxLog.Text += s;
        }

        private async void appendLog(char ch)
        {
            await this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(
                () =>
                {
                    if ((ch >= 32 && ch < 255)|| ch ==10 || ch == 13)
                    {
                        TextBoxLog.Text += new string(ch, 1);
                    }
                    else
                    {
                        TextBoxLog.Text += $"[{((int)ch).ToString("X2")}]";
                    }
                })
            );
        }

        private SerialPort port;    // current COM port
        private Task portWatcher;    // another task

        private void watcherTask()
        {
            try
            {
                for (; ; )
                {
                    if (port == null) return;
                    int ch = port.ReadChar();
                    //System.Diagnostics.Debug.Write("!");

                    if (ch == -1) return;
                    appendLog((char)ch);
                    if (ch == 13)
                    {
                        // do nothing
                    }
                    else if (ch == 10)
                    {
                        lastLineBuffer = currentLineBuffer;
                        currentLineBuffer = new StringBuilder();
                        if (lastLineBuffer.ToString().StartsWith(":::")) // result marker
                        {
                            result = ResultStatusMarker.ShowResult;
                            waiter.Set();
                        }
                        else if (lastLineBuffer.ToString() == "###")    // statement end marker
                        {
                            result = ResultStatusMarker.CommandEnd;
                            waiter.Set();
                        }
                    }
                    else
                    {
                        currentLineBuffer.Append((char)ch);
                    }
                }
            }
            catch (Exception e)
            {
                //appendLog(e.ToString());
            }
        }

        private void portOpen()
        {
            try
            {
                //　TBW
                port = new SerialPort("COM3", 9600, Parity.None, 8, StopBits.One);
                port.Open();
                port.DtrEnable = true;
                port.RtsEnable = true;
                port.Handshake = Handshake.XOnXOff;
                portWatcher = Task.Run((Action)watcherTask);
            }
            catch (Exception e)
            {
                port = null;
                appendLog(e.ToString());
            }
            updateOpenCloseStatus();
        }

        private void portClose()
        {
            if (port == null) return;
            port.Close();
            port.Dispose();
            port = null;
            updateOpenCloseStatus();
        }

        public void portOutput(string s)
        {
            try
            {
                port.Write(s);
                appendLog(s + "\n");
            }
            catch (Exception e)
            {
                appendLog(e.ToString());
            }
        }

        static private void enumVisual(Visual myVisual, Action<Visual> act)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(myVisual); i++)
            {
                // Retrieve child visual at specified index value.
                Visual childVisual = (Visual)VisualTreeHelper.GetChild(myVisual, i);

                // Do processing of the child visual object.
                act(childVisual);

                // Enumerate children of the child visual object.
                enumVisual(childVisual, act);
            }
        }

        private void setEnables(bool isOpen)
        {
            enumVisual(this, (visual) => {
                var button = visual as Button;
                if (button == null) return;
                button.IsEnabled = isOpen;
            });
            ButtonPortOpen.IsEnabled = !isOpen;
            ButtonPortClose.IsEnabled = isOpen;
        }

        private void updateOpenCloseStatus()
        {
            bool isOpen = port != null;
            TextBlockOpenClose.Text = isOpen ? "OPEN" : "CLOSE";
            TextBlockOpenClose.Foreground = new SolidColorBrush(isOpen ? Colors.Green : Colors.Red);
            BorderOpenClose.BorderBrush = TextBlockOpenClose.Foreground;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MyProgress.Visibility = Visibility.Hidden;
            setEnables(false);
            updateOpenCloseStatus();
            _navi.Navigate(pageStart);
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            portClose();
        }

        private void ButtonPortClose_Click(object sender, RoutedEventArgs e)
        {
            portClose();
            setEnables(false);
        }

        private void ButtonPortOpen_Click(object sender, RoutedEventArgs e)
        {
            portOpen();
            setEnables(true);
        }
        private int getTargetDrive() => ComboDriveSelect.SelectedIndex + 1;

        private async Task<int> sendCommand(string statement)
        {
            //portOutput("\x1b<ERR=0:PRINT \"###\"\r");
            //await waitResult();
            //portOutput("\x1b<abc\r\x1b<PRINT \"###\"\r");
            //await waitResult();
            //portOutput("\x1b<" + statement+ "\r");
            //await waitResult();
            portOutput("\x1b<" + statement + "\r");
            //await waitResult();
            portOutput("\x1b<print \":::\";ERR\r");
            var r = await waitResult();
            return r;
        }

        private async Task<int> waitResult()
        {
            return await Task.Run(() =>
            {
                waiter.Reset();
                waiter.WaitOne();
                var s = lastLineBuffer.ToString();
                if (s.Length <= 3 || result != ResultStatusMarker.ShowResult) return -1;
                int.TryParse(s.Substring(3), out var r);
                return r;
            });
        }

        private async void ButtonFiles_Click(object sender, RoutedEventArgs e)
        {
            if (port == null) return;
            await sendCommand($"files {getTargetDrive()}");
        }

        private void ButtonSectors_Click(object sender, RoutedEventArgs e)
        {
            _navi.Navigate(pageSector);
        }

        private void ButtonDirect_Click(object sender, RoutedEventArgs e)
        {
            _navi.Navigate(pageDirectCommand);
        }
    }
}
