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
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;

namespace AIcruiter
{
    public partial class Form1 : Form
    {
        public class Question
        {
            // 인덱스, 질문, 정답, 카테고리로 구성
            public int idx;
            public string question;
            public string answer;
            public string category; // 카테고리 추가

            public Question(int idx, string question, string answer, string category)
            {
                this.idx = idx;
                this.question = question;
                this.answer = answer;
                this.category = category;
            }
        }

        private static readonly string apiKey = Environment.GetEnvironmentVariable("OPEN_API_KEY"); // 시스템 환경 변수 설정.
        private static readonly string apiEndpoint = "https://api.openai.com/v1/chat/completions";

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

            // 질문 내용
            string question = questions[rNumber].question;
            int questionIdx = questions[rNumber].idx;
            string category = questions[rNumber].category;

            // 카테고리별 답변 저장 경로 설정
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, category + "Answer");
            string answerPath = Path.Combine(folderPath, $"{category}Answer{questionIdx}.txt");

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

            if (File.Exists(answerPath))
            {
                Button btnShowPrevious = new Button
                {
                    Text = "이전 답변 보기",
                    Location = new Point(110, 200),
                    Size = new Size(120, 30)
                };

                btnShowPrevious.Click += (s, ev) =>
                {
                    string previousAnswer = File.ReadAllText(answerPath);
                    MessageBox.Show(previousAnswer, "이전 답변");
                };

                modalForm.Controls.Add(btnShowPrevious);
            }

            // 저장 버튼
            Button saveButton = new Button();
            saveButton.Text = "저장하기";
            saveButton.Location = new System.Drawing.Point(20, 200);
            saveButton.Size = new System.Drawing.Size(80, 30);

            saveButton.Click += (s, ev) =>
            {
                string userAnswer = answerBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(userAnswer))
                {
                    MessageBox.Show("답변을 입력해주세요.", "입력 필요");
                    return;
                }

                // 폴더가 없으면 생성
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // 덮어쓰기 확인
                if (File.Exists(answerPath))
                {
                    DialogResult result = MessageBox.Show(
                        "이 질문에 대한 기존 답변이 존재합니다.\n새 답변으로 덮어쓰시겠습니까?",
                        "답변 덮어쓰기 확인",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );

                    if (result == DialogResult.No)
                    {
                        return; // 저장 안 함
                    }
                }

                // 답변 저장
                try
                {
                    string content = $"질문: {question}\r\n\r\n답변: {userAnswer}";

                    File.WriteAllText(answerPath, content);
                    MessageBox.Show("답변이 저장되었습니다.", "저장 완료");

                    modalForm.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("파일 저장 오류: " + ex.Message, "오류");
                }
            };
            modalForm.Controls.Add(saveButton);

            // 채점 버튼 추가
            Button gradeButton = new Button();
            gradeButton.Text = "채점";
            gradeButton.Location = new System.Drawing.Point(390, 200);
            gradeButton.Size = new System.Drawing.Size(80, 30);

            gradeButton.Click += async (s, ev) =>
            {
                // GPT에 보내는 질문을 입력합니다.
                string query = questions[rNumber].question + "에 대해서 " + questions[rNumber].answer +"라는 정답을 기준으로 " + answerBox.Text + "의 점수와 피드백을 제공해줘(200자 이하).";

                // 응답을 받아오는 메서드 호출
                string response = await GetGptResponse(query);
                MessageBox.Show("GPT 응답: " + response);
            };
            modalForm.Controls.Add(gradeButton);

            // 모달창 띄우기
            modalForm.ShowDialog();
        }

        private void btn2_load_Click(object sender, EventArgs e)
        {
            Form selectionForm = new Form();
            selectionForm.Text = "답변 선택";
            selectionForm.Size = new Size(400, 300);
            selectionForm.StartPosition = FormStartPosition.CenterParent;

            ListBox listBox = new ListBox();
            listBox.Size = new Size(360, 180);
            listBox.Location = new Point(10, 10);

            // 질문 인덱스 매핑용 리스트
            List<int> validIndexes = new List<int>();

            foreach (var q in questions)
            {
                string category = q.category;
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, category + "Answer", $"{category}Answer{q.idx}.txt");

                if (File.Exists(filePath))
                {
                    listBox.Items.Add($"{q.idx}. {q.question}");
                    validIndexes.Add(q.idx); // .txt 파일이 존재하는 질문의 인덱스만 저장
                }
            }

            Button btnOpen = new Button()
            {
                Text = "보기",
                Location = new Point(290, 200),
                Size = new Size(80, 30)
            };

            btnOpen.Click += (s, ev) =>
            {
                if (listBox.SelectedIndex >= 0)
                {
                    int selectedIdx = validIndexes[listBox.SelectedIndex];
                    string category = questions.First(q => q.idx == selectedIdx).category;
                    string answerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, category + "Answer", $"{category}Answer{selectedIdx}.txt");

                    if (!File.Exists(answerPath))
                    {
                        MessageBox.Show("답변을 찾을 수 없습니다.", "오류");
                        return;
                    }

                    string answer = File.ReadAllText(answerPath);

                    Form editor = new Form()
                    {
                        Text = "답변 수정",
                        Size = new Size(500, 350)
                    };

                    TextBox answerEditor = new TextBox()
                    {
                        Multiline = true,
                        Text = answer,
                        Size = new Size(450, 200),
                        Location = new Point(10, 10)
                    };

                    answerEditor.SelectionStart = answerEditor.Text.Length;
                    answerEditor.SelectionLength = 0;

                    Button btnSave = new Button()
                    {
                        Text = "저장",
                        Location = new Point(360, 220),
                        Size = new Size(100, 30)
                    };

                    btnSave.Click += (se, ee) =>
                    {
                        File.WriteAllText(answerPath, answerEditor.Text);
                        MessageBox.Show("수정 완료!");
                        editor.Close();
                    };

                    editor.Controls.Add(answerEditor);
                    editor.Controls.Add(btnSave);
                    editor.ShowDialog();
                }
            };

            selectionForm.Controls.Add(listBox);
            selectionForm.Controls.Add(btnOpen);
            selectionForm.ShowDialog();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //.txt파일 위치는 \temp\bin\Debug 폴더
            var files = new Dictionary<string, string>
            {
                // key는 파일 이름, value는 카테고리로, 각 파일이 어떤 카테고리에 속하는지를 매핑
                { "DataStructure.txt", "DataStructure" },
                { "OS.txt", "OS" }
            };

            foreach (var kvp in files)
            {
                string path = kvp.Key;
                string category = kvp.Value;

                // 줄 단위로 하여 질문을 리스트에 추가
                foreach (string line in File.ReadAllLines(path))
                {
                    string[] columns = line.Split('/');

                    Question tQuestion = new Question(int.Parse(columns[0]), columns[1], columns[2], category);
                    questions.Add(tQuestion);
                }
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

        // GPT에 쿼리 보내고 응답 받아오는 함수
        private static async Task<string> GetGptResponse(string query)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);

                var requestData = new
                {
                    model = "gpt-4-turbo", // 최신 모델 사용
                    messages = new[]
                    {
            new { role = "user", content = query }
        },
                    max_tokens = 500
                };

                string jsonData = JsonConvert.SerializeObject(requestData);

                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(apiEndpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);
                    return jsonResponse.choices[0].message.content.ToString().Trim();
                }
                else
                {
                    return "Error: " + response.StatusCode;
                }
            }
        }
    }
}