using System;
using System.Windows.Forms;

namespace NFT.NavyReader
{
    static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var mf = new MainForm();
            Application.Run(mf);
        }
    }
}
