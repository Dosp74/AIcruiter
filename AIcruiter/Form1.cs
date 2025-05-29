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
using System.Windows.Forms.DataVisualization.Charting;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AIcruiter
{
    public partial class Form1 : Form
    {
        public NetworkStream m_Stream;
        public StreamReader m_Read;
        public StreamWriter m_Write;
        const int PORT = 7777;

        private CancellationTokenSource cancellationTokenSource1;
        private CancellationToken cancellationtoken1;

        TcpClient m_Client;
        public bool m_bConnect = false;

        public void Disconnect()
        {
            if (!m_bConnect)
                return;

            m_bConnect = false;

            m_Read?.Close();
            m_Write?.Close();

            m_Stream?.Close();
            cancellationTokenSource1?.Cancel();
        }

        public void Connect()
        {
            m_Client = new TcpClient();

            try
            {
                m_Client.Connect("127.0.0.1", PORT);    //루프백 주소
            }
            catch
            {
                m_bConnect = false;
                return;
            }
            m_bConnect = true;
            m_Stream = m_Client.GetStream();

            m_Read = new StreamReader(m_Stream);
            m_Write = new StreamWriter(m_Stream);

            cancellationTokenSource1 = new CancellationTokenSource();
            cancellationtoken1 = cancellationTokenSource1.Token;
        }

        public async Task<string> Receive(CancellationToken token)
        {
            try
            {
                if (!m_bConnect || token.IsCancellationRequested)
                    return "";

                StringBuilder sb = new StringBuilder();
                string line;

                while (!token.IsCancellationRequested)
                {
                    line = await m_Read.ReadLineAsync();

                    if (line == null)
                    {
                        Disconnect();
                        break;
                    }

                    if (line.Trim() == "[END]")
                    {
                        // 종료 조건 ([END] 신호)
                        break;
                    }

                    sb.AppendLine(line);
                }

                return sb.ToString().TrimEnd(); // 마지막 개행 제거
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    MessageBox.Show("서버 응답 수신 중 오류 발생: " + ex.Message);
                }
                Disconnect();
                return "";
            }
        }

        async public Task Send(string query)
        {
            try
            {
                await m_Write.WriteLineAsync(query);
                await m_Write.FlushAsync();
            }
            catch
            {
                MessageBox.Show("쿼리 전송에 실패했습니다.");
            }
        }

        public class Question
        {
            // 인덱스, 질문, 정답, 카테고리로 구성
            public int idx;
            public string question;
            public string answer;
            public string category;

            public Question(int idx, string question, string answer, string category)
            {
                this.idx = idx;
                this.question = question;
                this.answer = answer;
                this.category = category;
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

        private System.Windows.Forms.Timer stopwatchTimer;  // 타이머 객체
        private TimeSpan elapsedTime;
        private Label stopwatchLabel;  // 경과 시간을 표시할 레이블

        // StopwatchTimer_Tick 메서드는 그대로 사용
        private void StopwatchTimer_Tick(object sender, EventArgs e)
        {
            elapsedTime = elapsedTime.Add(TimeSpan.FromSeconds(1)); ;
            stopwatchLabel.Text = elapsedTime.ToString(@"mm\:ss");
        }

        //채점 버튼 안 누르고, 모달폼 종료 시 타이머 정지 처리
        private void modalForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            stopwatchTimer.Stop();  // 타이머 정지
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
            stopwatchTimer = new System.Windows.Forms.Timer();
            stopwatchTimer.Interval = 1000; // 1초마다 실행
            stopwatchTimer.Tick += StopwatchTimer_Tick;

            elapsedTime = TimeSpan.Zero;

            // 모달창 생성
            Form modalForm = new Form();
            modalForm.Text = "면접 질문";
            modalForm.Size = new System.Drawing.Size(500, 350); // 크기 늘림
            modalForm.StartPosition = FormStartPosition.CenterParent;
            modalForm.FormClosed += modalForm_FormClosed;

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

                // 응답을 받아오는 메서드 호출
                string query;
                if (currentCategory == "Character")
                {
                    query = $"grading\n" +
                            $"질문: {questions[rNumber].question}\n" +
                            $"답변: {answerBox.Text}\n" +
                            $"회사의 인성 면접 기준(정직성, 책임감, 협업 능력 등)에 따라 답변을 평가해줘. " +
                            $"답변이 구체적이고 진정성이 느껴지며 실제 상황을 바탕으로 한 예시가 포함되어야 높은 점수를 받을 수 있어. " +
                            $"형식적인 답변과 일반적인 말만 나열된 경우에는 낮은 점수를 줘. " +
                            $"점수를 부여할 때 엄격하게 판단하고 지나치게 후한 점수를 주지 마. " +
                            $"답변이 아무것도 입력되지 않았다면 점수를 부여하지마. " +
                            $"100점 만점 기준으로 채점하고, '정확성', '논리성', '표현력' 항목별 점수를 포함해 아래 형식의 정확한 JSON으로 응답해줘. 각 점수는 정수형 숫자여야 하고, '점'이라는 단어는 포함하지 마. " +
                            $"예시:\r\n{{\r\n  \"점수\": ?,\r\n  \"정확성\": ?,\r\n  \"논리성\": ?,\r\n  \"표현력\": ?,\r\n  \"피드백\": \"(200자 이하의 구체적인 내용)\"\r\n}}" + "\n[END]";
                }
                else
                {
                    query = $"grading\n" +
                            $"질문: {questions[rNumber].question}\n" +
                            $"정답 키워드: {questions[rNumber].answer}\n" +
                            $"답변: {answerBox.Text}\n\n" +
                            $"답변이 정답과 다르더라도 개념 설명이 정확하다면 점수를 부여해도 돼. " +
                            $"답변이 아무것도 입력되지 않았다면 점수를 부여하지마. " +
                            $"정확성과 이해도를 고려해 100점 만점으로 채점하고, '정확성', '논리성', '표현력' 항목별 점수를 포함하여 아래 형식의 정확한 JSON으로 응답해줘. 각 점수는 정수형 숫자여야 하고, '점'이라는 단어는 포함하지 마. " +
                            $"예시:\r\n{{\r\n  \"점수\": ?,\r\n  \"정확성\": ?,\r\n  \"논리성\": ?,\r\n  \"표현력\": ?,\r\n  \"피드백\": \"(200자 이하의 구체적인 내용)\"\r\n}}" + "\n[END]";
                }

                Connect();
                await Send(query);
                string response = await Receive(cancellationtoken1);

                if (string.IsNullOrWhiteSpace(response))
                {
                    MessageBox.Show("채점에 실패하였습니다.");
                    return;
                }

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

                resultForm.FormClosing += (s2, e2) =>
                {
                    Disconnect();
                };

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

            Label lblOS = new Label()
            {
                Text = "운영체제 질문",
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
            ListBox OSListBox = new ListBox() { Size = new Size(360, 120), Location = new Point(10, 200) };
            ListBox characterInterviewListBox = new ListBox() { Size = new Size(360, 120), Location = new Point(10, 330) };

            // 각 카테고리별 데이터 준비
            List<int> dataStructureIndexes = new List<int>();
            List<int> OSIndexes = new List<int>();
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
                    else if (category == "OS")
                    {
                        OSListBox.Items.Add($"{q.idx}. {q.question}");
                        OSIndexes.Add(q.idx);
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
                OSListBox.Items.Clear();
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
                            else if (category == "OS")
                            {
                                OSListBox.Items.Add($"{q.idx}. {q.question}");
                            }
                            else if (category == "Character")
                            {
                                characterInterviewListBox.Items.Add($"{q.idx}. {q.question}");
                            }
                        }
                    }
                }
            };

            // 각 카테고리에서 하나씩만 선택하도록 만들기
            dataStructureListBox.SelectedIndexChanged += (s, ev) =>
            {
                if (dataStructureListBox.SelectedIndex >= 0)
                {
                    OSListBox.ClearSelected();
                    characterInterviewListBox.ClearSelected();
                }
            };

            OSListBox.SelectedIndexChanged += (s, ev) =>
            {
                if (OSListBox.SelectedIndex >= 0)
                {
                    dataStructureListBox.ClearSelected();
                    characterInterviewListBox.ClearSelected();
                }
            };

            characterInterviewListBox.SelectedIndexChanged += (s, ev) =>
            {
                if (characterInterviewListBox.SelectedIndex >= 0)
                {
                    dataStructureListBox.ClearSelected();
                    OSListBox.ClearSelected();
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
                else if (OSListBox.SelectedIndex >= 0)
                    selectedListBox = OSListBox;
                else if (characterInterviewListBox.SelectedIndex >= 0)
                    selectedListBox = characterInterviewListBox;

                if (selectedListBox != null && selectedListBox.SelectedIndex >= 0)
                {
                    int selectedIdx = -1;
                    if (selectedListBox == dataStructureListBox)
                        selectedIdx = dataStructureIndexes[selectedListBox.SelectedIndex];
                    else if (selectedListBox == OSListBox)
                        selectedIdx = OSIndexes[selectedListBox.SelectedIndex];
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
                        Location = new Point(10, 10),
                        TabStop = false
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
            selectionForm.Controls.Add(lblOS);
            selectionForm.Controls.Add(lblCharacterInterview);
            selectionForm.Controls.Add(dataStructureListBox);
            selectionForm.Controls.Add(OSListBox);
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

        private void btnAnswer_Click(object sender, EventArgs e)
        {
            // 데이터 파일 경로 설정
            string dataStructureFilePath = "DataStructure.txt";
            string operatingSystemFilePath = "OS.txt";

            Form answerForm = new Form();
            answerForm.Text = "정답 확인";
            answerForm.Size = new Size(500, 600);
            answerForm.StartPosition = FormStartPosition.CenterParent;

            // 검색용 텍스트박스 추가
            TextBox searchBox = new TextBox();
            searchBox.Size = new Size(360, 30);
            searchBox.Location = new Point(10, 10);
            answerForm.Controls.Add(searchBox);

            // 각 카테고리별 제목 라벨 추가
            Label lblDataStructure = new Label()
            {
                Text = "자료구조 질문",
                Location = new Point(10, 50),
                Size = new Size(360, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            Label lblOperatingSystem = new Label()
            {
                Text = "운영체제 질문",
                Location = new Point(10, 180),
                Size = new Size(360, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            // 각 카테고리별 ListBox 추가
            ListBox dataStructureListBox = new ListBox() { Size = new Size(360, 120), Location = new Point(10, 70), SelectionMode = SelectionMode.One };
            ListBox operatingSystemListBox = new ListBox() { Size = new Size(360, 120), Location = new Point(10, 200), SelectionMode = SelectionMode.One };

            // 자료구조 질문 파일 경로 및 운영체제 질문 파일 경로를 통해 질문 추가
            if (File.Exists(dataStructureFilePath))
            {
                foreach (string line in File.ReadAllLines(dataStructureFilePath))
                {
                    string[] columns = line.Split('/');
                    string question = columns[1];
                    dataStructureListBox.Items.Add(question); // 질문을 ListBox에 추가
                }
            }

            if (File.Exists(operatingSystemFilePath))
            {
                foreach (string line in File.ReadAllLines(operatingSystemFilePath))
                {
                    string[] columns = line.Split('/');
                    string question = columns[1];
                    operatingSystemListBox.Items.Add(question); // 질문을 ListBox에 추가
                }
            }

            // ListBox에서 선택된 항목을 추적하여 다른 ListBox에서 선택이 취소되도록 하기
            dataStructureListBox.SelectedIndexChanged += (s, ev) =>
            {
                if (dataStructureListBox.SelectedIndex >= 0)
                {
                    operatingSystemListBox.ClearSelected(); // 운영체제 ListBox 선택 해제
                }
            };

            operatingSystemListBox.SelectedIndexChanged += (s, ev) =>
            {
                if (operatingSystemListBox.SelectedIndex >= 0)
                {
                    dataStructureListBox.ClearSelected(); // 자료구조 ListBox 선택 해제
                }
            };

            // 검색 기능 구현
            searchBox.TextChanged += (s, ev) =>
            {
                string searchText = searchBox.Text.ToLower(); // 소문자로 변환하여 대소문자 구분 없이 검색

                // 각 ListBox의 항목을 초기화하고 필터링된 항목만 추가
                dataStructureListBox.Items.Clear();
                operatingSystemListBox.Items.Clear();

                // 자료구조 질문 검색
                foreach (var line in File.ReadAllLines(dataStructureFilePath))
                {
                    string[] columns = line.Split('/');
                    string question = columns[1].ToLower(); // 소문자로 변환하여 검색

                    if (question.Contains(searchText))
                    {
                        dataStructureListBox.Items.Add(columns[1]);
                    }
                }

                // 운영체제 질문 검색
                foreach (var line in File.ReadAllLines(operatingSystemFilePath))
                {
                    string[] columns = line.Split('/');
                    string question = columns[1].ToLower(); // 소문자로 변환하여 검색

                    if (question.Contains(searchText))
                    {
                        operatingSystemListBox.Items.Add(columns[1]);
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
                string selectedFilePath = null;

                // 각 카테고리에서 선택된 항목을 구분
                if (dataStructureListBox.SelectedIndex >= 0)
                {
                    selectedListBox = dataStructureListBox;
                    selectedFilePath = dataStructureFilePath;
                }
                else if (operatingSystemListBox.SelectedIndex >= 0)
                {
                    selectedListBox = operatingSystemListBox;
                    selectedFilePath = operatingSystemFilePath;
                }

                if (selectedListBox != null && selectedListBox.SelectedIndex >= 0)
                {
                    int selectedIdx = selectedListBox.SelectedIndex; // 선택된 질문 인덱스

                    // 선택된 카테고리의 해당 질문에 대한 모범 답안을 가져오기
                    string answerPath = selectedFilePath == dataStructureFilePath ? "DataStructure.txt" : "OS.txt";

                    if (!File.Exists(answerPath))
                    {
                        MessageBox.Show("모범 답변을 찾을 수 없습니다.", "오류");
                        return;
                    }

                    string answer = string.Empty;

                    // 파일을 한 줄씩 읽고, '/'로 나누어 3번째 항목(답변)을 가져옴
                    string[] lines = File.ReadAllLines(answerPath);
                    if (selectedIdx >= 0 && selectedIdx < lines.Length)
                    {
                        string[] columns = lines[selectedIdx].Split('/');
                        if (columns.Length >= 3)
                        {
                            answer = columns[2];  // 3번째 항목이 답변
                        }
                    }

                    if (string.IsNullOrEmpty(answer))
                    {
                        MessageBox.Show("답변을 찾을 수 없습니다.", "오류");
                        return;
                    }

                    // 답변을 텍스트박스에 표시
                    Form editor = new Form()
                    {
                        Text = "모범 답안 보기",
                        Size = new Size(500, 350)
                    };

                    TextBox answerEditor = new TextBox()
                    {
                        Multiline = true,
                        Text = answer,
                        Size = new Size(450, 200),
                        Location = new Point(10, 10),
                        TabStop = false
                    };

                    editor.Controls.Add(answerEditor);
                    editor.ShowDialog();
                }
            };

            // 폼에 추가
            answerForm.Controls.Add(lblDataStructure);
            answerForm.Controls.Add(lblOperatingSystem);
            answerForm.Controls.Add(dataStructureListBox);
            answerForm.Controls.Add(operatingSystemListBox);
            answerForm.Controls.Add(btnOpen);
            answerForm.ShowDialog();
        }
    }
}