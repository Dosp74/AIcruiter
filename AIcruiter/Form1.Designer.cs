namespace AIcruiter
{
    partial class Form1
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.btn1_random = new System.Windows.Forms.Button();
            this.btn2_load = new System.Windows.Forms.Button();
            this.btnAnswer = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label1.Location = new System.Drawing.Point(217, 37);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(260, 37);
            this.label1.TabIndex = 0;
            this.label1.Text = "가상 면접 시뮬레이터";
            // 
            // btn1_random
            // 
            this.btn1_random.Font = new System.Drawing.Font("Microsoft Sans Serif", 16.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btn1_random.Location = new System.Drawing.Point(108, 138);
            this.btn1_random.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btn1_random.Name = "btn1_random";
            this.btn1_random.Size = new System.Drawing.Size(143, 72);
            this.btn1_random.TabIndex = 1;
            this.btn1_random.Text = "랜덤 질문";
            this.btn1_random.UseVisualStyleBackColor = true;
            this.btn1_random.Click += new System.EventHandler(this.btn1_random_Click);
            // 
            // btn2_load
            // 
            this.btn2_load.Font = new System.Drawing.Font("Microsoft Sans Serif", 16.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btn2_load.Location = new System.Drawing.Point(273, 138);
            this.btn2_load.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btn2_load.Name = "btn2_load";
            this.btn2_load.Size = new System.Drawing.Size(160, 72);
            this.btn2_load.TabIndex = 2;
            this.btn2_load.Text = "내 답변 확인";
            this.btn2_load.UseVisualStyleBackColor = true;
            this.btn2_load.Click += new System.EventHandler(this.btn2_load_Click);
            // 
            // btnAnswer
            // 
            this.btnAnswer.Font = new System.Drawing.Font("Microsoft Sans Serif", 16.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnAnswer.Location = new System.Drawing.Point(460, 138);
            this.btnAnswer.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnAnswer.Name = "btnAnswer";
            this.btnAnswer.Size = new System.Drawing.Size(160, 72);
            this.btnAnswer.TabIndex = 3;
            this.btnAnswer.Text = "모범 답안 확인";
            this.btnAnswer.UseVisualStyleBackColor = true;
            this.btnAnswer.Click += new System.EventHandler(this.btnAnswer_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(123, 99);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(457, 12);
            this.label2.TabIndex = 5;
            this.label2.Text = "이 프로그램은 기술 면접과 인성 면접을 위한 랜덤 질문 생성 및 채점 시스템입니다. ";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(735, 258);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnAnswer);
            this.Controls.Add(this.btn2_load);
            this.Controls.Add(this.btn1_random);
            this.Controls.Add(this.label1);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btn1_random;
        private System.Windows.Forms.Button btn2_load;
        private System.Windows.Forms.Button btnAnswer;
        private System.Windows.Forms.Label label2;
    }
}

