using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AIcruiter
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void btn1_random_Click(object sender, EventArgs e)
        {
            // 질문 내용 (하나만 하드코딩으로 설정//추후 txt파일로 대체)
            string question = "랜덤질문 생성";

            // 모달창 생성
            Form modalForm = new Form();
            modalForm.Text = "면접 질문";
            modalForm.Size = new System.Drawing.Size(400, 300);
            modalForm.StartPosition = FormStartPosition.CenterParent;

            // 질문을 보여줄 라벨
            Label questionLabel = new Label();
            questionLabel.Text = question;
            questionLabel.AutoSize = true;
            questionLabel.Location = new System.Drawing.Point(20, 20);
            questionLabel.MaximumSize = new System.Drawing.Size(350, 0); // 줄바꿈 설정
            modalForm.Controls.Add(questionLabel);

            // 텍스트박스 (사용자 답변 입력)
            TextBox answerBox = new TextBox();
            answerBox.Multiline = true;
            answerBox.Size = new System.Drawing.Size(350, 100);
            answerBox.Location = new System.Drawing.Point(20, 80);
            modalForm.Controls.Add(answerBox);

            // 저장 버튼
            Button saveButton = new Button();
            saveButton.Text = "저장하기";
            saveButton.Location = new System.Drawing.Point(20, 200);
            saveButton.Click += (s, ev) =>
            {
                string userAnswer = answerBox.Text;

                if (!string.IsNullOrWhiteSpace(userAnswer))
                {
                    try
                    {
                        // 질문과 답변을 함께 저장
                        string content = $"질문: {question}\n답변: {userAnswer}\n\n";
                        File.AppendAllText("user_input_one.txt", content); // 파일에 추가로 저장
                        MessageBox.Show("질문과 답변이 저장되었습니다.", "저장 완료");
                        modalForm.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("파일 저장 오류: " + ex.Message, "오류");
                    }
                }
                else
                {
                    MessageBox.Show("답변을 입력해주세요.", "오류");
                }
            };
            modalForm.Controls.Add(saveButton);

            // 모달창 띄우기
            modalForm.ShowDialog();
        }

        private void btn2_load_Click(object sender, EventArgs e)
        {
            // 파일 경로 지정
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user_input_one.txt");

            if (File.Exists(filePath))
            {
                string loadedText = File.ReadAllText(filePath);
                MessageBox.Show(loadedText, "저장된 질문 및 답변");
            }
            else
            {
                MessageBox.Show("저장된 파일이 없습니다.", "오류");
            }
        }


    }
}
