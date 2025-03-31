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
            this.button3 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("나눔고딕", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label1.Location = new System.Drawing.Point(213, 103);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(385, 46);
            this.label1.TabIndex = 0;
            this.label1.Text = "Al 면접관 시뮬레이터";
            // 
            // btn1_random
            // 
            this.btn1_random.Font = new System.Drawing.Font("나눔고딕", 16.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btn1_random.Location = new System.Drawing.Point(101, 265);
            this.btn1_random.Name = "btn1_random";
            this.btn1_random.Size = new System.Drawing.Size(163, 90);
            this.btn1_random.TabIndex = 1;
            this.btn1_random.Text = "랜덤 질문";
            this.btn1_random.UseVisualStyleBackColor = true;
            this.btn1_random.Click += new System.EventHandler(this.btn1_random_Click);
            // 
            // btn2_load
            // 
            this.btn2_load.Font = new System.Drawing.Font("나눔고딕", 16.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btn2_load.Location = new System.Drawing.Point(291, 265);
            this.btn2_load.Name = "btn2_load";
            this.btn2_load.Size = new System.Drawing.Size(183, 90);
            this.btn2_load.TabIndex = 2;
            this.btn2_load.Text = "내 답변 확인";
            this.btn2_load.UseVisualStyleBackColor = true;
            this.btn2_load.Click += new System.EventHandler(this.btn2_load_Click);
            // 
            // button3
            // 
            this.button3.Font = new System.Drawing.Font("나눔고딕", 16.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.button3.Location = new System.Drawing.Point(502, 265);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(183, 90);
            this.button3.TabIndex = 3;
            this.button3.Text = "키워드 확인";
            this.button3.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.btn2_load);
            this.Controls.Add(this.btn1_random);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btn1_random;
        private System.Windows.Forms.Button btn2_load;
        private System.Windows.Forms.Button button3;
    }
}

