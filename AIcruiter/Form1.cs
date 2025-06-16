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
using AIcruiter.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections;
using Microsoft.EntityFrameworkCore;

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

                    if (line == null || line.Trim() == "[END]")
                    {
                        Disconnect();
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

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            btn1_random.FlatStyle = FlatStyle.Flat;
            btn1_random.FlatAppearance.BorderSize = 0;
            btnAnswer.FlatStyle = FlatStyle.Flat;
            btnAnswer.FlatAppearance.BorderSize = 0;
            btn2_load.FlatStyle = FlatStyle.Flat;
            btn2_load.FlatAppearance.BorderSize = 0;
            btnSharedAnswers.FlatStyle = FlatStyle.Flat;
            btnSharedAnswers.FlatAppearance.BorderSize = 0;

            using (var db = new AppDbContext())
            {
                db.Database.Migrate();

                // 파일→카테고리 매핑
                var files = new Dictionary<string, string>
                {
                    { "DataStructure.txt", "DataStructure" },
                    { "OS.txt",            "OS"            },
                    { "Network.txt",       "Network"       },
                    { "DataBase.txt",      "Database"      },
                    { "Character.txt",     "Character"     },
                };

                bool anyNew = false;
                foreach (var kvp in files)
                {
                    string file = kvp.Key;
                    string category = kvp.Value;

                    // 이 카테고리는 이미 로드되었으면 건너뛰기
                    if (db.Questions.Any(q => q.Category == category))
                        continue;

                    string path = Path.Combine(AppContext.BaseDirectory, file);
                    if (!File.Exists(path))
                        continue;

                    foreach (var line in File.ReadAllLines(path))
                    {
                        var cols = line.Split('/');
                        db.Questions.Add(new Question
                        {
                            Id = int.Parse(cols[0]),
                            Text = cols[1],
                            Answer = cols.Length > 2 ? cols[2] : null,
                            Category = category
                        });
                    }
                    anyNew = true;
                }

                if (anyNew)
                    db.SaveChanges();
            }
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

        private readonly Random rand = new Random();

        private void btn1_random_Click(object sender, EventArgs e)
        {
            using (var db = new AppDbContext())
            {
                // 랜덤으로 질문 하나 선택
                int totalCount = db.Questions.Count();
                
                if (totalCount == 0)
                {
                    MessageBox.Show("등록된 질문이 없습니다.");
                    return;
                }

                int randomIndex = rand.Next(totalCount);

                var question = db.Questions
                    .Skip(randomIndex)
                    .Take(1)
                    .First();

                // 이전 답변 불러오기
                var previousAnswer = db.UserAnswers
                    .Where(a => a.QuestionId == question.Id)
                    .OrderByDescending(a => a.SubmittedAt)
                    .FirstOrDefault();

                // 스톱워치 초기화
                stopwatchTimer = new System.Windows.Forms.Timer();
                stopwatchTimer.Interval = 1000; // 1초마다 실행
                stopwatchTimer.Tick += StopwatchTimer_Tick;
                elapsedTime = TimeSpan.Zero;

                // 모달창 생성
                Form modalForm = new Form();
                modalForm.Text = "면접 질문";
                modalForm.Size = new System.Drawing.Size(510, 350); // 크기 늘림
                modalForm.BackColor = Color.FromArgb(10, 10, 16);
                modalForm.StartPosition = FormStartPosition.CenterParent;
                modalForm.FormClosed += modalForm_FormClosed;

                // 질문을 보여줄 라벨
                Label questionLabel = new Label();
                questionLabel.Text = question.Text;
                questionLabel.AutoSize = true;
                questionLabel.Location = new System.Drawing.Point(20, 20);
                questionLabel.MaximumSize = new System.Drawing.Size(450, 0); // 줄바꿈 설정
                questionLabel.ForeColor = System.Drawing.Color.White;
                questionLabel.Font = new Font("맑은 고딕", 11, FontStyle.Bold);
                modalForm.Controls.Add(questionLabel);

                // 텍스트박스 (사용자 답변 입력)
                TextBox answerBox = new TextBox();
                answerBox.Multiline = true;
                answerBox.Size = new System.Drawing.Size(450, 100);
                answerBox.Location = new System.Drawing.Point(20, 80);
                modalForm.Controls.Add(answerBox);

                // 이전 답변 보기 버튼 추가
                if (previousAnswer != null)
                {
                    Button btnShowPrevious = new Button
                    {
                        Text = "이전 답변",
                        Location = new Point(130, 200),
                        Size = new Size(90, 40),
                        ForeColor = System.Drawing.Color.White,
                        Font = new Font("맑은 고딕", 11, FontStyle.Bold),
                        BackColor = Color.FromArgb(27, 28, 34),
                    };
                    btnShowPrevious.FlatStyle = FlatStyle.Flat;
                    btnShowPrevious.FlatAppearance.BorderSize = 0;

                    btnShowPrevious.Click += (s, ev) =>
                    {
                        MessageBox.Show(previousAnswer.AnswerContent, "이전 답변");
                    };

                    modalForm.Controls.Add(btnShowPrevious);
                }

                // 저장 버튼
                Button saveButton = new Button();
                saveButton.Text = "저장하기";
                saveButton.ForeColor = System.Drawing.Color.White;
                saveButton.Font = new Font("맑은 고딕", 11, FontStyle.Bold);
                saveButton.BackColor = Color.FromArgb(27, 28, 34);
                saveButton.FlatStyle = FlatStyle.Flat;
                saveButton.FlatAppearance.BorderSize = 0;
                saveButton.Location = new System.Drawing.Point(20, 200);
                saveButton.Size = new System.Drawing.Size(90, 40);

                saveButton.Click += (s, ev) =>
                {
                    string userAnswer = answerBox.Text.Trim();

                    if (string.IsNullOrWhiteSpace(userAnswer))
                    {
                        MessageBox.Show("답변을 입력해주세요.");
                        return;
                    }

                    var existing = db.UserAnswers
                        .Where(a => a.QuestionId == question.Id)
                        .OrderByDescending(a => a.SubmittedAt)
                        .FirstOrDefault();

                    // 덮어쓰기 확인
                    if (existing != null)
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

                        existing.AnswerContent = userAnswer;
                        existing.SubmittedAt = DateTime.Now;
                    }
                    else
                    {
                        db.UserAnswers.Add(new UserAnswer
                        {
                            QuestionId = question.Id,
                            AnswerContent = userAnswer,
                            SubmittedAt = DateTime.Now
                        });
                    }

                    db.SaveChanges();

                    MessageBox.Show("답변이 저장되었습니다.", "저장 완료");
                    // modalForm.Close();
                };
                modalForm.Controls.Add(saveButton);

                // 스톱워치 타이머를 시작
                stopwatchTimer.Start();

                // 스톱워치 시간을 표시할 라벨
                stopwatchLabel = new Label()
                {
                    Location = new Point(20, 250),
                    Size = new Size(200, 30),
                    Text = "00:00",
                    Font = new Font("맑은 고딕", 11, FontStyle.Bold),
                    ForeColor = Color.White
                };
                modalForm.Controls.Add(stopwatchLabel);

                // 채점 버튼 추가
                Button gradeButton = new Button();
                gradeButton.Text = "채점";
                gradeButton.Location = new System.Drawing.Point(380, 200);
                gradeButton.Size = new System.Drawing.Size(90, 40);
                gradeButton.ForeColor = System.Drawing.Color.White;
                gradeButton.Font = new Font("맑은 고딕", 11, FontStyle.Bold);
                gradeButton.BackColor = Color.FromArgb(27, 28, 34);
                gradeButton.FlatStyle = FlatStyle.Flat;
                gradeButton.FlatAppearance.BorderSize = 0;

                gradeButton.Click += async (s, ev) =>
                {
                    // 스톱워치 멈추기
                    stopwatchTimer.Stop();

                    // 응답을 받아오는 메서드 호출
                    string query;
                    if (question.Category == "Character")
                    {
                        query = $"grading\n" +
                                $"질문: {question.Text}\n" +
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
                                $"질문: {question.Text}\n" +
                                $"정답 키워드: {question.Answer}\n" +
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
                    string feedbackText = null;

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
                        Size = new Size(440, 540),
                        BackColor = Color.FromArgb(27, 28, 34),
                        StartPosition = FormStartPosition.CenterParent
                    };

                    // 점수를 이모지로 시각화
                    Label resultLabel = new Label()
                    {
                        Text = $"{score}점 {emoji}",
                        Font = new Font("Segoe UI", 14, FontStyle.Bold),
                        ForeColor = Color.White,
                        AutoSize = true,
                        Location = new Point(30, 30)
                    };
                    resultForm.Controls.Add(resultLabel);

                    TextBox feedbackBox = new TextBox()
                    {
                        Text = feedbackText,
                        Size = new Size(360, 160),
                        Location = new Point(30, 80),
                        ScrollBars = ScrollBars.Vertical,
                        Multiline = true,
                        ReadOnly = true
                    };
                    resultForm.Controls.Add(feedbackBox);

                    // 항목별 점수를 차트로 시각화
                    if (accuracy > 0 || logic > 0 || clarity > 0)
                    {
                        Chart chart = new Chart()
                        {
                            Size = new Size(360, 200),
                            Location = new Point(30, 270),
                        };

                        ChartArea area = new ChartArea("ScoreArea");
                        area.AxisY.Maximum = 100;
                        chart.ChartAreas.Add(area);

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

                    // 답변 공유 버튼
                    Button btnShare = new Button()
                    {
                        Text = "답변 공유",
                        Size = new Size(90, 30),
                        Location = new Point(300, 30),
                        ForeColor = System.Drawing.Color.Black,
                        BackColor = Color.White,
                        Font = new Font("맑은 고딕", 10, FontStyle.Bold)
                    };
                    btnShare.FlatStyle = FlatStyle.Flat;
                    btnShare.FlatAppearance.BorderSize = 0;

                    btnShare.Click += async (s3, e3) =>
                    {
                        string userId = Environment.UserName; // 윈도우 사용자 이름
                        //string userId = "testuser1";
                        string userAnswer = answerBox.Text.Trim();

                        if (string.IsNullOrWhiteSpace(userAnswer))
                        {
                            MessageBox.Show("답변이 비어 있습니다.", "오류");
                            return;
                        }

                        // 공유 메시지
                        string shareMessage =
                            $"sharing\n" +
                            $"QuestionId: {question.Id}\n" +
                            $"UserId: {userId}\n" +
                            $"Score: {score}\n" +
                            $"Answer: {userAnswer}\n" +
                            $"SubmittedAt: {DateTime.Now:yyyy-MM-dd HH:mm}\n" +
                            $"[END]";

                        // 서버 전송
                        try
                        {
                            Connect();
                            await Send(shareMessage);
                            await Receive(cancellationtoken1);
                            Disconnect();
                            MessageBox.Show("답변 공유 성공", "성공");
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("공유 중 오류 발생", "실패");
                            return;
                        }
                    };

                    resultForm.Controls.Add(btnShare);

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
        }

        private void btn2_load_Click(object sender, EventArgs e)
        {
            using (var db = new AppDbContext())
            {
                Form selectionForm = new Form();
                selectionForm.Text = "답변 선택";
                selectionForm.AutoScroll = true;
                selectionForm.Size = new Size(450, 800);
                selectionForm.BackColor = Color.FromArgb(27, 28, 34);
                selectionForm.StartPosition = FormStartPosition.CenterParent;

                Label lblSearch = new Label()
                {
                    Text = "검색",
                    Location = new Point(10, 10),
                    Size = new Size(360, 20),
                    ForeColor = System.Drawing.Color.White,
                    Font = new Font("맑은 고딕", 11, FontStyle.Bold)
                };

                // 검색용 텍스트박스 추가
                TextBox searchBox = new TextBox();
                searchBox.Size = new Size(360, 30);
                searchBox.Location = new Point(50, 10);
                selectionForm.Controls.Add(searchBox);

                // 각 카테고리별 제목 라벨 추가
                Label lblDataStructure = new Label()
                {
                    Text = "자료구조",
                    Location = new Point(10, 50),
                    Size = new Size(360, 20),
                    ForeColor = System.Drawing.Color.White,
                    Font = new Font("맑은 고딕", 11, FontStyle.Bold)
                };

                Label lblOS = new Label()
                {
                    Text = "운영체제",
                    Location = new Point(10, 180),
                    Size = new Size(360, 20),
                    ForeColor = System.Drawing.Color.White,
                    Font = new Font("맑은 고딕", 11, FontStyle.Bold)
                };

                Label lblCharacterInterview = new Label()
                {
                    Text = "인성",
                    Location = new Point(10, 310),
                    Size = new Size(360, 20),
                    ForeColor = System.Drawing.Color.White,
                    Font = new Font("맑은 고딕", 11, FontStyle.Bold)
                };

                // 네트워크 (추가)
                Label lblNet = new Label()
                {
                    Text = "네트워크",
                    Location = new Point(10, 440),
                    Size = new Size(360, 20),
                    ForeColor = Color.White,
                    Font = new Font("맑은 고딕", 11, FontStyle.Bold)
                };

                // 데이터베이스 (추가)
                Label lblDB = new Label()
                {
                    Text = "데이터베이스",
                    Location = new Point(10, 570),
                    Size = new Size(360, 20),
                    ForeColor = Color.White,
                    Font = new Font("맑은 고딕", 11, FontStyle.Bold)
                };

                // 각 카테고리별 ListBox 추가
                ListBox dataStructureListBox = new ListBox() { Size = new Size(400, 90), Location = new Point(10, 80) };
                ListBox OSListBox = new ListBox() { Size = new Size(400, 90), Location = new Point(10, 210) };
                ListBox characterInterviewListBox = new ListBox() { Size = new Size(400, 90), Location = new Point(10, 340) };
                //네트워크, db 추가
                ListBox networkListBox = new ListBox() { Size = new Size(400, 90), Location = new Point(10, 470) };
                ListBox databaseListBox = new ListBox() { Size = new Size(400, 90), Location = new Point(10, 600) };

                dataStructureListBox.IntegralHeight = false;
                OSListBox.IntegralHeight = false;
                networkListBox.IntegralHeight = false;
                databaseListBox.IntegralHeight = false;
                characterInterviewListBox.IntegralHeight = false;

                // 각 카테고리별 데이터 준비
                Dictionary<ListBox, List<int>> listMap = new Dictionary<ListBox, List<int>>
                {
                    { dataStructureListBox, new List<int>() },
                    { OSListBox, new List<int>() },
                    { characterInterviewListBox, new List<int>() },
                    { networkListBox, new List<int>() },  // ← 추가
                    { databaseListBox,new List<int>() }   // ← 추가
                };

                // DB에서 답변이 존재하는 질문만 분류하여 삽입
                var answeredQuestions = db.UserAnswers
                    .GroupBy(a => a.QuestionId)
                    .Select(g => g.Key)
                    .ToList();

                var allQuestions = db.Questions.ToList();

                foreach (var q in allQuestions)
                {
                    if (!answeredQuestions.Contains(q.Id)) continue;

                    string display = $"{q.Id}. {q.Text}";

                    if (q.Category == "DataStructure")
                    {
                        dataStructureListBox.Items.Add(display);
                        listMap[dataStructureListBox].Add(q.Id);
                    }
                    else if (q.Category == "OS")
                    {
                        OSListBox.Items.Add(display);
                        listMap[OSListBox].Add(q.Id);
                    }
                    else if (q.Category == "Character")
                    {
                        characterInterviewListBox.Items.Add(display);
                        listMap[characterInterviewListBox].Add(q.Id);
                    }
                    else if (q.Category == "Network")
                    {
                        networkListBox.Items.Add(display);
                        listMap[networkListBox].Add(q.Id);
                    }
                    else if (q.Category == "Database")
                    {
                        databaseListBox.Items.Add(display);
                        listMap[databaseListBox].Add(q.Id);
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
                    networkListBox.Items.Clear();
                    databaseListBox.Items.Clear();

                    listMap[dataStructureListBox].Clear();
                    listMap[OSListBox].Clear();
                    listMap[characterInterviewListBox].Clear();
                    listMap[networkListBox].Clear();
                    listMap[databaseListBox].Clear();

                    foreach (var q in allQuestions)
                    {
                        if (!answeredQuestions.Contains(q.Id)) continue;
                        if (!q.Text.ToLower().Contains(searchText)) continue;

                        string display = $"{q.Id}. {q.Text}";

                        if (q.Category == "DataStructure")
                        {
                            dataStructureListBox.Items.Add(display);
                            listMap[dataStructureListBox].Add(q.Id);
                        }
                        else if (q.Category == "OS")
                        {
                            OSListBox.Items.Add(display);
                            listMap[OSListBox].Add(q.Id);
                        }
                        else if (q.Category == "Character")
                        {
                            characterInterviewListBox.Items.Add(display);
                            listMap[characterInterviewListBox].Add(q.Id);
                        }
                        else if (q.Category == "Network")
                        {
                            networkListBox.Items.Add(display);
                            listMap[networkListBox].Add(q.Id);
                        }
                        else if (q.Category == "Database")
                        {
                            databaseListBox.Items.Add(display);
                            listMap[databaseListBox].Add(q.Id);
                        }
                    }
                };

                // 리스트 간 중복 선택 방지
                void ClearOthers(ListBox selected)
                {
                    if (selected.SelectedIndex < 0) return;

                    foreach (var list in listMap.Keys)
                    {
                        if (list != selected)
                            list.ClearSelected();
                    }
                }

                dataStructureListBox.SelectedIndexChanged += (s, ev) => ClearOthers(dataStructureListBox);
                OSListBox.SelectedIndexChanged += (s, ev) => ClearOthers(OSListBox);
                characterInterviewListBox.SelectedIndexChanged += (s, ev) => ClearOthers(characterInterviewListBox);
                networkListBox.SelectedIndexChanged += (s, ev) => ClearOthers(networkListBox);
                databaseListBox.SelectedIndexChanged += (s, ev) => ClearOthers(databaseListBox);


                // 보기 버튼 추가
                Button btnOpen = new Button()
                {
                    Text = "보기",
                    Location = new Point(330, 710),  // Y 값 조정
                    Size = new Size(80, 30),
                    ForeColor = System.Drawing.Color.Black,
                    BackColor = Color.White,
                    Font = new Font("맑은 고딕", 11, FontStyle.Bold)
                };
                btnOpen.FlatStyle = FlatStyle.Flat;
                btnOpen.FlatAppearance.BorderSize = 0;

                btnOpen.Click += (s, ev) =>
                {
                    // 어떤 리스트에서 선택됐는지 파악
                    ListBox selectedList = listMap.Keys.FirstOrDefault(lb => lb.SelectedIndex >= 0);
                    if (selectedList == null) return;

                    int selectedId = listMap[selectedList][selectedList.SelectedIndex];

                    // 해당 질문의 가장 최근 답변 로딩
                    var answer = db.UserAnswers
                                   .Where(a => a.QuestionId == selectedId)
                                   .OrderByDescending(a => a.SubmittedAt)
                                   .FirstOrDefault();

                    if (answer == null)
                    {
                        MessageBox.Show("답변이 없습니다.", "오류");
                        return;
                    }

                    Form editor = new Form
                    {
                        Text = "답변 수정",
                        Size = new Size(500, 350),
                        BackColor = Color.FromArgb(27, 28, 34)
                };

                    Label lblmyAnswer = new Label()
                    {
                        Text = "나의 답변",
                        Location = new Point(10, 10),
                        Size = new Size(360, 20),
                        ForeColor = System.Drawing.Color.White,
                        Font = new Font("맑은 고딕", 11, FontStyle.Bold)
                    };

                    TextBox answerEditor = new TextBox
                    {
                        Multiline = true,
                        Text = answer.AnswerContent,
                        Size = new Size(450, 200),
                        Location = new Point(10, 40),
                        TabStop = false
                    };

                    Button btnSave = new Button
                    {
                        Text = "저장",
                        Location = new Point(360, 250),
                        Size = new Size(100, 30),
                        Font = new Font("맑은 고딕", 10, FontStyle.Bold),
                        BackColor = Color.White
                    };
                    btnSave.FlatStyle = FlatStyle.Flat;
                    btnSave.FlatAppearance.BorderSize = 0;

                    btnSave.Click += (se, ee) =>
                    {
                        answer.AnswerContent = answerEditor.Text;
                        db.SaveChanges();
                        MessageBox.Show("수정 완료!");
                        editor.Close();
                    };

                    editor.Controls.Add(answerEditor);
                    editor.Controls.Add(btnSave);
                    editor.Controls.Add(lblmyAnswer);
                    editor.ShowDialog();
                };

                // 폼에 추가
                selectionForm.Controls.Add(lblSearch);
                selectionForm.Controls.Add(lblDataStructure);
                selectionForm.Controls.Add(lblOS);
                selectionForm.Controls.Add(lblCharacterInterview);
                selectionForm.Controls.Add(dataStructureListBox);
                selectionForm.Controls.Add(OSListBox);
                selectionForm.Controls.Add(characterInterviewListBox);
                selectionForm.Controls.Add(lblNet);
                selectionForm.Controls.Add(networkListBox);
                selectionForm.Controls.Add(lblDB);
                selectionForm.Controls.Add(databaseListBox);
                selectionForm.Controls.Add(btnOpen);

                selectionForm.ShowDialog();
            }
        }

        private void btnAnswer_Click(object sender, EventArgs e)
        {
            using (var db = new AppDbContext())
            {
                Form answerForm = new Form();
                answerForm.Text = "정답 확인";
                answerForm.AutoScroll = true;
                answerForm.Size = new Size(450, 660);
                answerForm.StartPosition = FormStartPosition.CenterParent;
                answerForm.BackColor = Color.FromArgb(27, 28, 34);


                // 검색용 텍스트박스 추가
                TextBox searchBox = new TextBox();
                searchBox.Size = new Size(360, 30);
                searchBox.Location = new Point(50, 10);
                answerForm.Controls.Add(searchBox);

                Label lblSearch = new Label()
                {
                    Text = "검색",
                    Location = new Point(10, 10),
                    Size = new Size(360, 20),
                    ForeColor = System.Drawing.Color.White,
                    Font = new Font("맑은 고딕", 11, FontStyle.Bold)
                };

                // 각 카테고리별 제목 라벨 추가
                Label lblDataStructure = new Label()
                {
                    Text = "자료구조",
                    Location = new Point(10, 50),
                    Size = new Size(360, 20),
                    ForeColor = System.Drawing.Color.White,
                    Font = new Font("맑은 고딕", 11, FontStyle.Bold)
                };

                Label lblOperatingSystem = new Label()
                {
                    Text = "운영체제",
                    Location = new Point(10, 180),
                    Size = new Size(360, 20),
                    ForeColor = System.Drawing.Color.White,
                    Font = new Font("맑은 고딕", 11, FontStyle.Bold)
                };

                Label lblNetwork = new Label() 
                { 
                    Text = "네트워크", 
                    Location = new Point(10, 310), 
                    Size = new Size(360, 20),
                    ForeColor = System.Drawing.Color.White,
                    Font = new Font("맑은 고딕", 11, FontStyle.Bold)
                };
               
                Label lblDatabase = new Label() 
                { 
                    Text = "데이터베이스",
                    Location = new Point(10, 440),
                    ForeColor = System.Drawing.Color.White,
                    Font = new Font("맑은 고딕", 11, FontStyle.Bold)
                };
                


                // 각 카테고리별 ListBox 추가
                ListBox dataStructureListBox = new ListBox() { Size = new Size(400, 90), Location = new Point(10, 80), SelectionMode = SelectionMode.One };
                ListBox operatingSystemListBox = new ListBox() { Size = new Size(400, 90), Location = new Point(10, 210), SelectionMode = SelectionMode.One };
                ListBox networkListBox = new ListBox() { Size = new Size(400, 90), Location = new Point(10, 340), SelectionMode = SelectionMode.One };
                ListBox databaseListBox = new ListBox() { Size = new Size(400, 90), Location = new Point(10, 470), SelectionMode = SelectionMode.One };

                // 카테고리별 데이터
                var dsQuestions = db.Questions.Where(q => q.Category == "DataStructure").ToList();
                var osQuestions = db.Questions.Where(q => q.Category == "OS").ToList();
                var networkQuestions = db.Questions.Where(q => q.Category == "Network").ToList();
                var dbQuestions = db.Questions.Where(q => q.Category == "Database").ToList();

                foreach (var q in dsQuestions)
                    dataStructureListBox.Items.Add($"{q.Id}. {q.Text}");
                foreach (var q in osQuestions)
                    operatingSystemListBox.Items.Add($"{q.Id}. {q.Text}");
                foreach (var q in networkQuestions) 
                    networkListBox.Items.Add($"{q.Id}. {q.Text}");
                foreach (var q in dbQuestions) 
                    databaseListBox.Items.Add($"{q.Id}. {q.Text}");

                dataStructureListBox.SelectedIndexChanged += (s, ev) =>
                {
                    if (dataStructureListBox.SelectedIndex >= 0)
                    {
                        operatingSystemListBox.SelectedIndex = -1;
                        networkListBox.SelectedIndex = -1;
                        databaseListBox.SelectedIndex = -1;
                    }
                };

                operatingSystemListBox.SelectedIndexChanged += (s, ev) =>
                {
                    if (operatingSystemListBox.SelectedIndex >= 0)
                    {
                        dataStructureListBox.SelectedIndex = -1;
                        networkListBox.SelectedIndex = -1;
                        databaseListBox.SelectedIndex = -1;
                    }
                };

                networkListBox.SelectedIndexChanged += (s, ev) =>
                {
                    if (networkListBox.SelectedIndex >= 0)
                    {
                        dataStructureListBox.SelectedIndex = -1;
                        operatingSystemListBox.SelectedIndex = -1;
                        databaseListBox.SelectedIndex = -1;
                    }
                };

                databaseListBox.SelectedIndexChanged += (s, ev) =>
                {
                    if (databaseListBox.SelectedIndex >= 0)
                    {
                        dataStructureListBox.SelectedIndex = -1;
                        operatingSystemListBox.SelectedIndex = -1;
                        networkListBox.SelectedIndex = -1;
                    }
                };

                // 검색 기능 구현
                searchBox.TextChanged += (s, ev) =>
                {
                    string search = searchBox.Text.ToLower();

                    // 각 ListBox의 항목을 초기화하고 필터링된 항목만 추가
                    dataStructureListBox.Items.Clear();
                    operatingSystemListBox.Items.Clear();
                    networkListBox.Items.Clear();
                    databaseListBox.Items.Clear();

                    // 자료구조 질문 검색
                    foreach (var q in dsQuestions)
                        if (q.Text.ToLower().Contains(search))
                            dataStructureListBox.Items.Add($"{q.Id}. {q.Text}");

                    // 운영체제 질문 검색
                    foreach (var q in osQuestions)
                        if (q.Text.ToLower().Contains(search))
                            operatingSystemListBox.Items.Add($"{q.Id}. {q.Text}");

                    //네트워크 질문 검색
                    foreach (var q in networkQuestions)
                        if (q.Text.ToLower().Contains(search))
                            networkListBox.Items.Add($"{q.Id}. {q.Text}");
                    //데이터베이스 질문 검색
                    foreach (var q in dbQuestions)
                        if (q.Text.ToLower().Contains(search))
                            databaseListBox.Items.Add($"{q.Id}. {q.Text}");
                };

                // 보기 버튼 추가
                Button btnOpen = new Button()
                {
                    Text = "보기",
                    Location = new Point(330, 580),  // Y 값 조정
                    Size = new Size(80, 30),
                    Font = new Font("맑은 고딕", 10, FontStyle.Bold),
                    BackColor = Color.White
                };
                btnOpen.FlatStyle = FlatStyle.Flat;
                btnOpen.FlatAppearance.BorderSize = 0;

                btnOpen.Click += (s, ev) =>
                {
                    Question selected = null;

                    // 각 카테고리에서 선택된 항목을 구분
                    if (dataStructureListBox.SelectedIndex >= 0)
                        selected = dsQuestions[dataStructureListBox.SelectedIndex];
                    else if (operatingSystemListBox.SelectedIndex >= 0)
                        selected = osQuestions[operatingSystemListBox.SelectedIndex];
                    else if (networkListBox.SelectedIndex >= 0) 
                        selected = networkQuestions[networkListBox.SelectedIndex];
                    else if (databaseListBox.SelectedIndex >= 0) 
                        selected = dbQuestions[databaseListBox.SelectedIndex];

                    if (selected == null || string.IsNullOrWhiteSpace(selected.Answer))
                    {
                        MessageBox.Show("정답이 등록되지 않았습니다.", "오류");
                        return;
                    }

                    // 답변을 텍스트박스에 표시
                    Form editor = new Form
                    {
                        Text = "모범 답안 보기",
                        Size = new Size(500, 350),
                        BackColor = Color.FromArgb(27, 28, 34)
                    };

                    Label lblAnswer = new Label()
                    {
                        Text = "모범 답안",
                        Location = new Point(10, 10),
                        Size = new Size(360, 20),
                        ForeColor = System.Drawing.Color.White,
                        Font = new Font("맑은 고딕", 11, FontStyle.Bold)
                    };

                    TextBox answerEditor = new TextBox
                    {
                        Multiline = true,
                        Text = selected.Answer,
                        Size = new Size(450, 200),
                        Location = new Point(10, 40),
                        ReadOnly = true,
                        TabStop = false
                    };

                    editor.Controls.Add(lblAnswer);
                    editor.Controls.Add(answerEditor);
                    editor.ShowDialog();
                };

                // 폼에 추가
                answerForm.Controls.Add(lblSearch);
                answerForm.Controls.Add(lblDataStructure);
                answerForm.Controls.Add(lblOperatingSystem);
                answerForm.Controls.Add(lblNetwork);
                answerForm.Controls.Add(lblDatabase);

                answerForm.Controls.Add(dataStructureListBox);
                answerForm.Controls.Add(operatingSystemListBox);
                answerForm.Controls.Add(networkListBox);
                answerForm.Controls.Add(databaseListBox);

                answerForm.Controls.Add(btnOpen);
                answerForm.ShowDialog();
            }
        }

        private async void btnSharedAnswers_Click(object sender, EventArgs e)
        {
            Connect();
            if (!m_bConnect)
            {
                MessageBox.Show("서버 연결 실패");
                return;
            }

            await Send("loading\n[END]");
            string jsonResponse = await Receive(cancellationtoken1);
            Disconnect();

            if (jsonResponse.StartsWith("공유 답변 로딩 실패"))
            {
                MessageBox.Show("공유 답변 로딩 실패.\n" + jsonResponse, "실패");
                return;
            }

            List<SharedAnswer> sharedList = JsonConvert.DeserializeObject<List<SharedAnswer>>(jsonResponse);

            if (sharedList.Count == 0)
            {
                MessageBox.Show("공유된 답변이 없습니다.");
                return;
            }

            // 질문별 그룹핑
            var grouped = sharedList.GroupBy(sa => sa.QuestionId)
                                    .OrderBy(g => g.Key);

            Form sharedForm = new Form()
            {
                Text = "공유 답변 확인",
                Size = new Size(500, 390),
                BackColor = Color.FromArgb(27, 28, 34),
                StartPosition = FormStartPosition.CenterParent
            };

            Label lblquestion = new Label()
            {
                Text = "질문",
                Location = new Point(10, 10),
                Size = new Size(360, 20),
                ForeColor = System.Drawing.Color.White,
                Font = new Font("맑은 고딕", 11, FontStyle.Bold)
            };

            ListBox questionListBox = new ListBox()
            {
                Location = new Point(10, 40),
                Size = new Size(450, 90),
                IntegralHeight = false
            };

            Dictionary<int, List<SharedAnswer>> answerMap = new Dictionary<int, List<SharedAnswer>>();

            foreach (var group in grouped)
            {
                var q = group.First();
                using (var db = new AppDbContext())
                {
                    var question = db.Questions.FirstOrDefault(qq => qq.Id == q.QuestionId);
                    if (question != null)
                        questionListBox.Items.Add($"{q.QuestionId}. {question.Text}");
                    else
                        questionListBox.Items.Add($"{q.QuestionId}. (알 수 없는 질문)");

                    answerMap[q.QuestionId] = group.ToList();
                }
            }

            Label lblanswer = new Label()
            {
                Text = "사용자 / 점수 / 등록일",
                Location = new Point(10, 150),
                Size = new Size(360, 20),
                ForeColor = System.Drawing.Color.White,
                Font = new Font("맑은 고딕", 11, FontStyle.Bold)
            };

            ListBox answerListBox = new ListBox()
            {
                Location = new Point(10, 180),
                Size = new Size(450, 90),
                IntegralHeight = false
            };

            Button btnView = new Button()
            {
                Text = "보기",
                Location = new Point(380, 300),
                Size = new Size(80, 30),
                Font = new Font("맑은 고딕", 10, FontStyle.Bold),
                BackColor = Color.White
            };

            questionListBox.SelectedIndexChanged += (s, ev) =>
            {
                answerListBox.Items.Clear();
                int idx = questionListBox.SelectedIndex;
                if (idx < 0) return;

                var questionId = answerMap.Keys.ElementAt(idx);
                foreach (var ans in answerMap[questionId])
                {
                    answerListBox.Items.Add($"{ans.UserId} / {ans.Score}점 / {ans.SubmittedAt}");
                }
            };

            btnView.Click += (s, ev) =>
            {
                int qIdx = questionListBox.SelectedIndex;
                int aIdx = answerListBox.SelectedIndex;

                if (qIdx < 0 || aIdx < 0) return;

                int questionId = answerMap.Keys.ElementAt(qIdx);
                var answer = answerMap[questionId][aIdx];

                MessageBox.Show(answer.AnswerContent, $"공유 답변 - {answer.UserId}");
            };

            sharedForm.Controls.Add(lblquestion);
            sharedForm.Controls.Add(lblanswer);
            sharedForm.Controls.Add(questionListBox);
            sharedForm.Controls.Add(answerListBox);
            sharedForm.Controls.Add(btnView);
            sharedForm.ShowDialog();
        }

        private async void TestSharedAnswers()
        {
            var random = new Random();
            List<Question> questions;

            using (var db = new AppDbContext())
            {
                questions = db.Questions.ToList();

                if (questions.Count == 0)
                {
                    MessageBox.Show("질문이 없습니다.");
                    return;
                }
            }

            string[] sampleAnswers =
            {
                "자료구조는 데이터를 효율적으로 저장하는 방식입니다.",
                "운영체제는 하드웨어 자원을 관리하고 사용자와 소통하는 소프트웨어입니다.",
                "책임감은 맡은 일을 끝까지 수행하는 자세입니다.",
                "퀵 정렬은 평균 시간 복잡도가 O(n log n)입니다.",
                "협업 능력은 팀원들과의 커뮤니케이션과 조율 능력을 의미합니다."
            };

            for (int i = 0; i < 10; i++)
            {
                var q = questions[random.Next(questions.Count)];
                string userId = $"testuser_{random.Next(1000, 9999)}";
                var answer = sampleAnswers[random.Next(sampleAnswers.Length)];
                int score = random.Next(60, 100);
                string submittedAt = DateTime.Now.AddMinutes(-random.Next(1000)).ToString("yyyy-MM-dd HH:mm");

                string shareMessage =
                    $"sharing\n" +
                    $"QuestionId: {q.Id}\n" +
                    $"UserId: {userId}\n" +
                    $"Score: {score}\n" +
                    $"Answer: {answer}\n" +
                    $"SubmittedAt: {submittedAt}\n" +
                    $"[END]";

            try
            {
                Connect();
                await Send(shareMessage);
                string response = await Receive(cancellationtoken1);
                Disconnect();
            }
            catch
            {
                MessageBox.Show("테스트 데이터 전송 중 오류 발생", "실패");
                return;
            }
}

            MessageBox.Show($"테스트 데이터 전송 완료", "성공");
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            TestSharedAnswers();
        }
    }
}