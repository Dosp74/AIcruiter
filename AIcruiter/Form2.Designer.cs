namespace AIcruiter
{
    partial class Answer
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
            this.AnswerNote = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // AnswerNote
            // 
            this.AnswerNote.BackColor = System.Drawing.SystemColors.Window;
            this.AnswerNote.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AnswerNote.Location = new System.Drawing.Point(0, 0);
            this.AnswerNote.Multiline = true;
            this.AnswerNote.Name = "AnswerNote";
            this.AnswerNote.ReadOnly = true;
            this.AnswerNote.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.AnswerNote.Size = new System.Drawing.Size(554, 403);
            this.AnswerNote.TabIndex = 0;
            this.AnswerNote.TabStop = false;
            // 
            // Answer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(554, 403);
            this.Controls.Add(this.AnswerNote);
            this.Name = "Answer";
            this.Text = "Answer";
            this.Load += new System.EventHandler(this.Answer_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox AnswerNote;
    }
}