namespace NFT.NavyReader
{
    partial class KeyRecorder
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.uiTb = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // uiTb
            // 
            this.uiTb.BackColor = System.Drawing.Color.Silver;
            this.uiTb.Font = new System.Drawing.Font("Noto Sans KR Regular", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.uiTb.ForeColor = System.Drawing.Color.OrangeRed;
            this.uiTb.Location = new System.Drawing.Point(23, 118);
            this.uiTb.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.uiTb.Name = "uiTb";
            this.uiTb.Size = new System.Drawing.Size(383, 43);
            this.uiTb.TabIndex = 0;
            this.uiTb.Text = "NoName";
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(17, 18);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(288, 85);
            this.label1.TabIndex = 1;
            this.label1.Text = "이름 입력후 엔터를 치세요. 키보드 한영전환 금지";
            // 
            // KeyRecorder
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(14F, 36F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(429, 189);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.uiTb);
            this.Font = new System.Drawing.Font("Noto Sans KR Regular", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.Name = "KeyRecorder";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "KeyRecorder";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox uiTb;
        private System.Windows.Forms.Label label1;
    }
}