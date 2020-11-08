using NFT.NavyReader.ref1;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Universe.Utility;

namespace NFT.NavyReader
{
    using KL = List<(Keys key, IntPtr wp)>;
    public partial class KeyRecorder : Form//, IMessageFilter
    {
        KeysSaver _ks;
        public KeyRecorder(IWin32Window win)
        {
            InitializeComponent();
            _win = win;
            _ks = new KeysSaver();
            KeyPreview = true;
        }
        IWin32Window _win;
        public async Task<(KL data, string text)> GetKeys()
        {
            //await Task.Delay(500);
            _ks.Start(Process.GetCurrentProcess());
            ShowDialog(_win);
            var keys = _ks.Stop();
            if (keys.Count > 0)
            {
                var last = keys.Last();
                if (last.key == Keys.Escape || last.key == Keys.Enter) keys.RemoveAt(keys.Count - 1);
            }
            return (keys, uiTb.Text);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape) Close();
            base.OnKeyDown(e);
        }

    }
}
