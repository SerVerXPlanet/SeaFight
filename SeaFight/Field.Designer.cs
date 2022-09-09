namespace SeaFight
{
    partial class Field
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // Field
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.DoubleBuffered = true;
            this.Name = "Field";
            this.Size = new System.Drawing.Size(275, 275);
            this.Load += new System.EventHandler(this.Field_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Field_Paint);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.Field_MouseClick);
            this.MouseLeave += new System.EventHandler(this.Field_MouseLeave);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Field_MouseMove);
            this.Resize += new System.EventHandler(this.Field_Resize);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
