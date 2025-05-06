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
using System.Windows.Forms.DataVisualization.Charting;
using System.Text.RegularExpressions;

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

        //선아 - mdi폼 수정 확인중
        private void btn1_random1_Click(object sender, EventArgs e)
        {
            // Form3을 생성하고 부모 폼 설정
            Form4 form4 = new Form4();
            form4.MdiParent = this;  // Form1을 부모 폼으로 설정

            // 부모 폼에서 랜덤 질문 버튼의 위치 가져오기
            int btn1RandomBottom = btn1_random.Bottom;  // 버튼의 하단 위치 (버튼 위로 자식 폼 배치)

            // Form3 크기 설정
            form4.Size = new Size(500, 300);  // 자식 폼 크기 설정

            // 자식 폼을 부모 폼의 중앙 아래에 위치시킴
            form4.Location = new Point(
                this.Left + (this.Width - form4.Width) / 2,  // 부모 폼 중앙에 위치하도록 X 좌표 설정
                this.Top + btn1RandomBottom + 20  // 버튼 아래에 여유 공간 20px을 두고 Y 좌표 설정
            );

            // 자식 폼 띄우기
            form4.Show();
        }





        private Timer stopwatchTimer;  // 타이머 객체
        private TimeSpan elapsedTime;
        private Label stopwatchLabel;  // 경과 시간을 표시할 레이블

        // StopwatchTimer_Tick 메서드는 그대로 사용
        private void StopwatchTimer_Tick(object sender, EventArgs e)
        {
            elapsedTime = elapsedTime.Add(TimeSpan.FromSeconds(1));
            stopwatchLabel.Text = elapsedTime.ToString(@"mm\:ss");
        }


        private void btn1_random_Click(object sender, EventArgs e)
        {
            // 0 ~ Count-1 중 난수 생성
            rNumber = rand.Next(questions.Count);

            // 질문 내용
            string question = questions[rNumber].question;
            int questionIdx = questions[rNumber].idx;
            string category = questions[rNumber].category;

            // 카테고리별 답변 저장 경로 설정
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, category + "Answer");
            string answerPath = Path.Combine(folderPath, $"{category}Answer{questionIdx}.txt");

            // 스톱워치 초기화
            stopwatchTimer = new Timer();
            stopwatchTimer.Interval = 1000; // 1초마다 실행
            stopwatchTimer.Tick += StopwatchTimer_Tick;

            elapsedTime = TimeSpan.Zero;

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

            // 이전 답변 보기 버튼 추가
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

            // 스톱워치 타이머를 시작
            stopwatchTimer.Start();

            // 스톱워치 시간을 표시할 라벨
            stopwatchLabel = new Label()
            {
                Location = new Point(20, 240),
                Size = new Size(200, 30),
                Text = "00:00"
            };
            modalForm.Controls.Add(stopwatchLabel);

            // 채점 버튼 추가
            Button gradeButton = new Button();
            gradeButton.Text = "채점";
            gradeButton.Location = new System.Drawing.Point(390, 200);
            gradeButton.Size = new System.Drawing.Size(80, 30);

           
            gradeButton.Click += async (s, ev) =>
            {
                // 스톱워치 멈추기
                stopwatchTimer.Stop();
                string currentCategory = questions[rNumber].category;

                // GPT에 보내는 질문을 입력합니다.
                string query;

                if (currentCategory == "Character")
                    query = $"질문: {questions[rNumber].question}\n" +
                            $"답변: {answerBox.Text}\n" +
                            $"회사의 인성 면접 기준(정직성, 책임감, 협업 능력 등)에 따라 답변을 평가해줘. " +
                            $"답변이 구체적이고 진정성이 느껴지며 실제 상황을 바탕으로 한 예시가 포함되어야 높은 점수를 받을 수 있어. " +
                            $"형식적인 답변과 일반적인 말만 나열된 경우에는 낮은 점수를 줘. " +
                            $"점수를 부여할 때 엄격하게 판단하고 지나치게 후한 점수를 주지 마. " +
                            $"100점 만점 기준으로 채점하고, '정확성', '논리성', '표현력' 항목별 점수를 포함해 아래 형식의 정확한 JSON으로 응답해줘. 각 점수는 정수형 숫자여야 하고, '점'이라는 단어는 포함하지 마. " +
                            $"예시:\r\n{{\r\n  \"점수\": ?,\r\n  \"정확성\": ?,\r\n  \"논리성\": ?,\r\n  \"표현력\": ?,\r\n  \"피드백\": \"(200자 이하의 구체적인 내용)\"\r\n}}";

                else
                {
                    query = $"질문: {questions[rNumber].question}\n" +
                            $"정답 키워드: {questions[rNumber].answer}\n" +
                            $"답변: {answerBox.Text}\n\n" +
                            $"답변이 정답과 다르더라도 개념 설명이 정확하다면 점수를 부여해도 돼. " +
                            $"정확성과 이해도를 고려해 100점 만점으로 채점하고, '정확성', '논리성', '표현력' 항목별 점수를 포함하여 아래 형식의 정확한 JSON으로 응답해줘. 각 점수는 정수형 숫자여야 하고, '점'이라는 단어는 포함하지 마. " +
                            $"예시:\r\n{{\r\n  \"점수\": ?,\r\n  \"정확성\": ?,\r\n  \"논리성\": ?,\r\n  \"표현력\": ?,\r\n  \"피드백\": \"(200자 이하의 구체적인 내용)\"\r\n}}";
                }
                // query = questions[rNumber].question + "에 대해서 " + questions[rNumber].answer + "라는 정답을 기준으로 " + answerBox.Text + "의 점수와 피드백을 제공해줘(200자 이하).";

                // 응답을 받아오는 메서드 호출
                string response = await GetGptResponse(query);
                // MessageBox.Show(response);

                int score = 0, accuracy = 0, logic = 0, clarity = 0;
                string feedbackText = response;

                try
                {
                    // JSON 파싱
                    dynamic json = JsonConvert.DeserializeObject(response);

                    score = (int)json["점수"];
                    accuracy = (int)json["정확성"];
                    logic = (int)json["논리성"];
                    clarity = (int)json["표현력"];
                    feedbackText = json["피드백"];
                }
                catch
                {
                    // JSON 파싱 실패 시 정규식으로 매칭
                    var matchScore = Regex.Match(response, @"점수\s*[:：]?\s*(\d+)");
                    if (matchScore.Success)
                        score = Math.Min(100, int.Parse(matchScore.Groups[1].Value));

                    var matchAcc = Regex.Match(response, @"정확성\s*[:：]?\s*(\d+)");
                    if (matchAcc.Success)
                        accuracy = int.Parse(matchAcc.Groups[1].Value);

                    var matchLogic = Regex.Match(response, @"논리성\s*[:：]?\s*(\d+)");
                    if (matchLogic.Success)
                        logic = int.Parse(matchLogic.Groups[1].Value);

                    var matchClarity = Regex.Match(response, @"표현력\s*[:：]?\s*(\d+)");
                    if (matchClarity.Success)
                        clarity = int.Parse(matchClarity.Groups[1].Value);

                    var matchFeedback = Regex.Match(response, @"피드백\s*[:：]?\s*(.+)");
                    if (matchFeedback.Success)
                        feedbackText = matchFeedback.Groups[1].Value.Trim();
                }

                string emoji;
                if (score >= 90) emoji = "🏆";
                else if (score >= 80) emoji = "😄";
                else if (score >= 60) emoji = "🙂";
                else emoji = "😞";

                Form resultForm = new Form()
                {
                    Text = "채점 결과",
                    Size = new Size(400, 500),
                    StartPosition = FormStartPosition.CenterParent
                };

                // 점수를 이모지로 시각화
                Label resultLabel = new Label()
                {
                    Text = $"{score}점 {emoji}",
                    Font = new Font("Segoe UI", 14, FontStyle.Bold),
                    AutoSize = true,
                    Location = new Point(30, 30)
                };
                resultForm.Controls.Add(resultLabel);

                // 점수를 프로그레스 바로 시각화
                ProgressBar progressBar = new ProgressBar()
                {
                    Value = score,
                    Maximum = 100,
                    Minimum = 0,
                    Size = new Size(300, 25),
                    Location = new Point(30, 70),
                    Style = ProgressBarStyle.Continuous
                };
                resultForm.Controls.Add(progressBar);

                // 상세 피드백
                Button detailButton = new Button()
                {
                    Text = "상세 피드백 확인",
                    Size = new Size(150, 30),
                    Location = new Point(30, 110)
                };
                detailButton.Click += (s2, e2) =>
                {
                    MessageBox.Show(feedbackText, "상세 피드백");
                };
                resultForm.Controls.Add(detailButton);

                // 항목별 점수를 차트로 시각화
                if (accuracy > 0 || logic > 0 || clarity > 0)
                {
                    Chart chart = new Chart()
                    {
                        Size = new Size(300, 200),
                        Location = new Point(30, 160)
                    };
                    chart.ChartAreas.Add(new ChartArea("ScoreArea"));

                    Series series = new Series("항목별 점수")
                    {
                        ChartType = SeriesChartType.Column,
                        Color = Color.CornflowerBlue
                    };
                    series.Points.AddXY("정확성", accuracy);
                    series.Points.AddXY("논리성", logic);
                    series.Points.AddXY("표현력", clarity);

                    chart.Series.Add(series);
                    resultForm.Controls.Add(chart);
                }

                resultForm.ShowDialog();
                
            };
            modalForm.Controls.Add(gradeButton);

            // 모달창 띄우기
            modalForm.ShowDialog();
        }


        private void btn2_load_Click(object sender, EventArgs e)
        {
            Form selectionForm = new Form();
            selectionForm.Text = "답변 선택";
            selectionForm.Size = new Size(400, 600);
            selectionForm.StartPosition = FormStartPosition.CenterParent;

            // 검색용 텍스트박스 추가
            TextBox searchBox = new TextBox();
            searchBox.Size = new Size(360, 30);
            searchBox.Location = new Point(10, 10);
            selectionForm.Controls.Add(searchBox);

            // 각 카테고리별 제목 라벨 추가
            Label lblDataStructure = new Label()
            {
                Text = "자료구조 질문",
                Location = new Point(10, 50),
                Size = new Size(360, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            Label lblAlgorithm = new Label()
            {
                Text = "알고리즘 질문",
                Location = new Point(10, 180),
                Size = new Size(360, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            Label lblCharacterInterview = new Label()
            {
                Text = "인성 면접 질문",
                Location = new Point(10, 310),
                Size = new Size(360, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            // 각 카테고리별 ListBox 추가
            ListBox dataStructureListBox = new ListBox() { Size = new Size(360, 120), Location = new Point(10, 70) };
            ListBox algorithmListBox = new ListBox() { Size = new Size(360, 120), Location = new Point(10, 200) };
            ListBox characterInterviewListBox = new ListBox() { Size = new Size(360, 120), Location = new Point(10, 330) };

            // 각 카테고리별 데이터 준비
            List<int> dataStructureIndexes = new List<int>();
            List<int> algorithmIndexes = new List<int>();
            List<int> characterInterviewIndexes = new List<int>();

            // 카테고리별 항목 추가
            foreach (var q in questions)
            {
                string category = q.category;
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, category + "Answer", $"{category}Answer{q.idx}.txt");

                if (File.Exists(filePath))
                {
                    if (category == "DataStructure")
                    {
                        dataStructureListBox.Items.Add($"{q.idx}. {q.question}");
                        dataStructureIndexes.Add(q.idx);
                    }
                    else if (category == "Algorithm")
                    {
                        algorithmListBox.Items.Add($"{q.idx}. {q.question}");
                        algorithmIndexes.Add(q.idx);
                    }
                    else if (category == "Character")
                    {
                        characterInterviewListBox.Items.Add($"{q.idx}. {q.question}");
                        characterInterviewIndexes.Add(q.idx);
                    }
                }
            }

            // 검색 기능 구현
            searchBox.TextChanged += (s, ev) =>
            {
                string searchText = searchBox.Text.ToLower(); // 소문자로 변환하여 대소문자 구분 없이 검색

                // 각 ListBox의 항목을 초기화하고 필터링된 항목만 추가
                dataStructureListBox.Items.Clear();
                algorithmListBox.Items.Clear();
                characterInterviewListBox.Items.Clear();

                foreach (var q in questions)
                {
                    string questionText = q.question.ToLower(); // 소문자로 변환하여 검색

                    // 카테고리별로 검색어가 포함된 항목만 추가
                    if (questionText.Contains(searchText))
                    {
                        string category = q.category;
                        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, category + "Answer", $"{category}Answer{q.idx}.txt");

                        if (File.Exists(filePath))
                        {
                            if (category == "DataStructure")
                            {
                                dataStructureListBox.Items.Add($"{q.idx}. {q.question}");
                            }
                            else if (category == "Algorithm")
                            {
                                algorithmListBox.Items.Add($"{q.idx}. {q.question}");
                            }
                            else if (category == "Character")
                            {
                                characterInterviewListBox.Items.Add($"{q.idx}. {q.question}");
                            }
                        }
                    }
                }
            };

            // 보기 버튼 추가
            Button btnOpen = new Button()
            {
                Text = "보기",
                Location = new Point(290, 460),  // Y 값 조정
                Size = new Size(80, 30)
            };

            btnOpen.Click += (s, ev) =>
            {
                ListBox selectedListBox = null;

                // 각 카테고리에서 선택된 항목을 구분
                if (dataStructureListBox.SelectedIndex >= 0)
                    selectedListBox = dataStructureListBox;
                else if (algorithmListBox.SelectedIndex >= 0)
                    selectedListBox = algorithmListBox;
                else if (characterInterviewListBox.SelectedIndex >= 0)
                    selectedListBox = characterInterviewListBox;

                if (selectedListBox != null && selectedListBox.SelectedIndex >= 0)
                {
                    int selectedIdx = -1;
                    if (selectedListBox == dataStructureListBox)
                        selectedIdx = dataStructureIndexes[selectedListBox.SelectedIndex];
                    else if (selectedListBox == algorithmListBox)
                        selectedIdx = algorithmIndexes[selectedListBox.SelectedIndex];
                    else if (selectedListBox == characterInterviewListBox)
                        selectedIdx = characterInterviewIndexes[selectedListBox.SelectedIndex];

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

            // 폼에 추가
            selectionForm.Controls.Add(lblDataStructure);
            selectionForm.Controls.Add(lblAlgorithm);
            selectionForm.Controls.Add(lblCharacterInterview);
            selectionForm.Controls.Add(dataStructureListBox);
            selectionForm.Controls.Add(algorithmListBox);
            selectionForm.Controls.Add(characterInterviewListBox);
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
                { "OS.txt", "OS" },
                { "Character.txt", "Character" },
            };

            foreach (var kvp in files)
            {
                string path = kvp.Key;
                string category = kvp.Value;

                // 줄 단위로 하여 질문을 리스트에 추가
                foreach (string line in File.ReadAllLines(path))
                {
                    string[] columns = line.Split('/');

                    int idx = int.Parse(columns[0]);
                    string question = columns[1];
                    string answer = columns.Length > 2 ? columns[2] : ""; // 인성 질문은 정답 키워드가 없음

                    questions.Add(new Question(idx, question, answer, category));
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