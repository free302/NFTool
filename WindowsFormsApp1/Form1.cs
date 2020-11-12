using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public delegate void GlobalMouseClickEventHander(object sender, MouseEventArgs e);

        [Category("Action")]
        [Description("Fires when any control on the form is clicked.")]
        public event GlobalMouseClickEventHander GlobalMouseClick;

        private void BindControlMouseClicks(Control con)
        {
            con.MouseClick += (sender, e) => TriggerMouseClicked(sender, e);
            foreach (Control i in con.Controls) BindControlMouseClicks(i);
            con.ControlAdded += (sender, e) =>  BindControlMouseClicks(e.Control);
        }
        private void TriggerMouseClicked(object sender, MouseEventArgs e) => GlobalMouseClick?.Invoke(sender, e);

        public Form1()
        {
            InitializeComponent();
            BindControlMouseClicks(this);
            initUi();
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _uiLog.Focus();
        }

        RichTextBox _uiLog;
        List<NumericUpDown> _ns = new List<NumericUpDown>();
        void initUi()
        {
            //
            var s = new SplitContainer();
            s.Dock = DockStyle.Fill;
            s.Orientation = Orientation.Vertical;
            s.FixedPanel = FixedPanel.Panel1;
            s.SplitterDistance = 150;
            s.Panel1.BackColor = Color.Gray;
            s.SplitterWidth = 10;
            Controls.Add(s);
            //
            _uiLog = new RichTextBox();
            _uiLog.Dock = DockStyle.Fill;
            _uiLog.BorderStyle = BorderStyle.None;
            _uiLog.BackColor = Color.Gray;
            _uiLog.TabIndex = 0;
            s.Panel2.Controls.Add(_uiLog);
            //
            var r = new Random();
            int top = 0;
            add();
            add();
            add();
            add();
            add();
            add();
            add();
            add();
            add();
            add();
            add();
            void add()
            {
                var n = new NumericUpDown();
                _ns.Add(n);
                n.Minimum = 1;
                n.Maximum = 12;
                n.Value = r.Next((int)n.Minimum, (int)n.Maximum);
                n.Increment = 1;
                n.Width = 70;
                n.TextAlign = HorizontalAlignment.Center;
                n.MouseWheel += n_MouseWheel;
                s.Panel1.Controls.Add(n);
                n.Left = 10;
                n.Top = top;
                top = n.Top + n.Height;

                n.ForeColor = Color.White;
                n.BackColor = Color.Gray;
                n.BorderStyle = BorderStyle.None;
            }
            GlobalMouseClick += (s, e) => { foreach (var n in _ns) n.Value = r.Next((int)n.Minimum, (int)n.Maximum); };
        }

        private void n_MouseWheel(object sender, MouseEventArgs e)
        {
            var n = ((NumericUpDown)sender);
            var v = n.Value + (e.Delta > 0 ? n.Increment : -n.Increment);
            n.Value = v > n.Maximum ? n.Maximum : (v < n.Minimum ? n.Minimum : v);
            ((HandledMouseEventArgs)e).Handled = true;
        }

        
    }
}
