using NFT.NavyReader.ref1;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows;

namespace NFT.NavyReader
{
    public partial class MainForm : Form
    {
        AppLogic _app;
        KeyboardHotkey _hook;
        public MainForm()
        {
            InitializeComponent();            
            try
            {
                _app = new AppLogic(log);

                initHotkey();
                initNames();
                initSels();
            }
            catch (Exception ex)
            {
                log(ex);
            }
        }
        protected override async void OnFormClosing(FormClosingEventArgs e)
        {
            Properties.Settings.Default.Save();
            if (_app != null)
            {
                await _app.Toggle(false);
                await Task.Run(() => { while (_invoking) Thread.Sleep(1000); });
            }
            base.OnFormClosing(e);
        }

        #region ---- Hotkey ----

        void initHotkey()
        {
            _hook = new KeyboardHotkey();
            _hook.KeyPressed += new EventHandler<KeyPressedEventArgs>(HotKeyPressed);
            _hook.RegisterHotKey(_ModifierKeys.Control, Keys.F1);
            _hook.RegisterHotKey(_ModifierKeys.Control, Keys.F2);
            _hook.RegisterHotKey(_ModifierKeys.Control, Keys.F3);
            _hook.RegisterHotKey(_ModifierKeys.Control, Keys.F4);//TEST
            _hook.RegisterHotKey(_ModifierKeys.Control, Keys.F5);//TEST
            _hook.RegisterHotKey(_ModifierKeys.Control, Keys.F6);//TEST
            _hook.RegisterHotKey(_ModifierKeys.Control, Keys.F12);//TEST
        }
        async void HotKeyPressed(object sender, KeyPressedEventArgs e)
        {
            try
            {
                log($"Hotkey: {e.Modifier} + {e.Key}");
                if (e.Key == Keys.F1)
                {
                    splitContainer2.Panel2Collapsed = true;
                    uiLog.Focus();
                    saveSels();
                    await _app.Toggle();
                }
                if (e.Key == Keys.F2)
                {
                    splitContainer2.Panel2Collapsed = false;
                    splitContainer2.SplitterDistance = 600;
                    Height = 1200;
                    _app.TestOcr(uiPicture, uiPicture2);
                }
                if (e.Key == Keys.F3)
                {
                    splitContainer2.Panel2Collapsed = false;
                    splitContainer2.SplitterDistance = 600;
                    Height = 1200;
                    var fd = new OpenFileDialog();
                    fd.Filter = "Iamge|*.bmp;*.png;*.jpg";
                    if (fd.ShowDialog() == DialogResult.OK)
                    {
                        log($"Loading file: {fd.FileName}");
                        _app.TestOcr2(fd.FileName, uiPicture, uiPicture2);
                    }
                }
                if (e.Key == Keys.F4)
                {
                    //var f = new KeyRecorder(this);
                    //var keys = await f.GetKeys();
                    //uiName1.Text = keys.text;
                    //uiLog.Focus();
                    //Thread.Sleep(100);
                    //var kp = new KeysPlayer(keys.data);
                    //kp.Start();
                    _app.SetOrigin();
                    log($"cursor={Cursor.Position}");
                    log($"size={this.Size}");
                }
                if (e.Key == Keys.F5)
                {
                    //saveSels();
                    //_app.Test();
                    _app.testSearch(uiPicture, uiPicture2);
                }
                if (e.Key == Keys.F6)
                {
                    _app.testFilter(uiPicture, uiPicture2);
                }
                if (e.Key == Keys.F2)
                {
                    _app.testFilter(uiPicture, uiPicture2);
                }
            }
            catch (Exception ex)
            {
                log(ex);
            }
        }

        #endregion


        #region ---- Names ----

        TextBox[] _names;
        void initNames()
        {
            _names = new[] { uiName0, uiName1, uiName2, uiName3, uiName4, uiName5, uiName6, uiName7, uiName8, uiName9, uiName10 };
            for (int i = 0; i < _names.Length; i++)
            {
                _names[i].TabIndex = 1 + i;
                _names[i].Tag = (Groth)i;
                _names[i].MouseClick += uiName_MouseClick;
                _names[i].Text = _app.GetNameText((Groth)i);
            }
        }
        private async void uiName_MouseClick(object sender, MouseEventArgs e)
        {
            var tb = (TextBox)sender;
            log(tb.Name);
            try
            {
                //if (e.Button == MouseButtons.Left && (ModifierKeys & Keys.Control) == Keys.Control)
                Thread.Sleep(100);
                var f = new KeyRecorder(tb);
                var keys = await f.GetKeys();
                tb.Text = keys.text;

                _app.AddNameKey((Groth)tb.Tag, keys.text, keys.data);
            }
            catch (Exception ex)
            {
                log(ex);
            }
            finally
            {
            }
        }

        CheckBox[] _sels;
        void initSels()
        {
            _sels = new[] { uiSel0, uiSel1, uiSel2, uiSel3, uiSel4, uiSel5, uiSel6, uiSel7, uiSel8, uiSel9, uiSel10 };
            for (int i = 0; i < _names.Length; i++)
            {
                _sels[i].TabIndex = 13 + i;
                _sels[i].Tag = (Groth)i;
                _sels[i].Checked = _app.GetSel((Groth)i);
            }
        }
        void saveSels()
        {
            for (int i = 0; i < _names.Length; i++) _app.AddSel((Groth)i, _sels[i].Checked);
        }

        #endregion


        #region ---- UI API ----

        volatile bool _invoking;
        void invoke(Action action)
        {
            try
            {
                _invoking = true;
                if (IsDisposed) return;
                if (!InvokeRequired) action();
                else Invoke(action);
            }
            catch { }
            finally
            {
                _invoking = false;
            }
        }

        void log(object message = null)
        {
            var msg = $"[{DateTime.Now:HHmmss.f}] {message}\n";
            File.AppendAllText("log.txt", msg);

            invoke(() =>
            {
                uiLog.AppendText(msg);
                uiLog.Refresh();
                uiLog.SelectionStart = uiLog.TextLength;
                uiLog.SelectionLength = 0;
                uiLog.ScrollToCaret();
            });
        }

        #endregion


        #region --- TEST ----

        PictureBoxSizeMode _pbSize = PictureBoxSizeMode.AutoSize;
        private void uiPicture_Click(object sender, EventArgs e)
        {
            _pbSize = (PictureBoxSizeMode)((1 + (int)_pbSize) % 5);
            uiPicture.SizeMode = _pbSize;
            log($"sizemode= {_pbSize}");
        }

        #endregion
    
    }
}
