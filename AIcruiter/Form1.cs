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
using System.Drawing.Configuration;

namespace AIcruiter
{
    public partial class Form1 : Form
    {
        public class Question
        {
            //(인덱스, 질문, 정답)으로 구성
            public int idx;
            public string question;
            public string answer;

            public Question(int idx, string question, string answer)
            {
                this.idx = idx;
                this.question = question;
                this.answer = answer;
            }
        }

        //.txt파일의 데이터를 불러올 리스트 생성
        List<Question> questions = new List<Question>();

        //Random 객체 생성
        Random rand = new Random();
        int rNumber = 0;

        public Form1()
        {
            InitializeComponent();
        }
        private void btn1_random_Click(object sender, EventArgs e)
        {
            //0 ~ Count-1 중 난수 생성
            rNumber = rand.Next(questions.Count);

            // 질문 내용 (하나만 하드코딩으로 설정//추후 txt파일로 대체)
            string question = questions[rNumber].question;

            // 모달창 생성
            Form modalForm = new Form();
            modalForm.Text = "면접 질문";
            modalForm.Size = new System.Drawing.Size(500, 350); // 크기 늘림
            modalForm.StartPosition = FormStartPosition.CenterParent;

            // 질문을 보여줄 라벨
            Label questionLabel = new Label();
            questionLabel.Text = question;
            questionLabel.AutoSize = true;
            questionLabel.Location = new System.Drawing.Point(20, 20);
            questionLabel.MaximumSize = new System.Drawing.Size(450, 0); // 줄바꿈 설정
            modalForm.Controls.Add(questionLabel);

            // 텍스트박스 (사용자 답변 입력)
            TextBox answerBox = new TextBox();
            answerBox.Multiline = true;
            answerBox.Size = new System.Drawing.Size(450, 100);
            answerBox.Location = new System.Drawing.Point(20, 80);
            modalForm.Controls.Add(answerBox);

            // 저장 버튼
            Button saveButton = new Button();
            saveButton.Text = "저장하기";
            saveButton.Location = new System.Drawing.Point(20, 200);
            saveButton.Size = new System.Drawing.Size(80, 30);

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

            // 채점 버튼 추가
            Button gradeButton = new Button();
            gradeButton.Text = "채점";
            gradeButton.Location = new System.Drawing.Point(120, 200);
            gradeButton.Size = new System.Drawing.Size(80, 30);

            gradeButton.Click += (s, ev) =>
            {
                // 점수를 100으로 고정하여 표시(추후 변경)
                int score = 100;

                MessageBox.Show($"점수: {score} / 100", "채점 결과");
            };
            modalForm.Controls.Add(gradeButton);

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

        private void Form1_Load(object sender, EventArgs e)
        {
            //.txt파일 위치는 \temp\bin\Debug 폴더
            string path = "DataStructure.txt";

            //줄 단위로 하여 질문을 리스트에 추가
            string[] content = File.ReadAllLines(path);
            foreach (string line in content)
            {
                string[] columns = line.Split('/');

                Question tQuestion = new Question(int.Parse(columns[0]), columns[1], columns[2]);
                questions.Add(tQuestion);
            }

            //바탕색 기본색으로 변경(MDI설정으로 인해 회색으로 설정되어 있음)
            this.Controls[this.Controls.Count - 1].BackColor = SystemColors.Control;
        }

        //정답확인 버튼 추가
        private void btnAnswer_Click(object sender, EventArgs e)
        {
            //Answer 폼 생성
            Answer answer = new Answer(questions);
            answer.Owner = this;
            answer.Show();
        }
    }
}
