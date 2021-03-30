using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1
    {

        #region ---- log ----
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
            //File.AppendAllText("log.txt", msg);

            invoke(() =>
            {
                _uiLog.AppendText(msg);
                _uiLog.Refresh();
                _uiLog.SelectionStart = _uiLog.TextLength;
                _uiLog.SelectionLength = 0;
                _uiLog.ScrollToCaret();
            });
        }
        #endregion


        protected override void WndProc(ref Message m)
        {
            log(m);
            base.WndProc(ref m);
        }

        protected override void DefWndProc(ref Message m)
        {

            base.DefWndProc(ref m);
        }



    }
}
