using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using AICruiter_Server.Models;
using System.Linq;

namespace AICruiter_Server
{
    public partial class Server : Form
    {
        public NetworkStream m_Stream;
        public StreamReader m_Read;
        public StreamWriter m_Write;
        const int PORT = 7777;

        public bool m_bStop = false;
        public bool m_bConnect = false;

        private TcpListener m_listener;
        private Thread m_thServer;
        Queue<TcpClient> clientList;

        private CancellationTokenSource cancellationTokenSource1;
        private CancellationTokenSource cancellationTokenSource2;
        private CancellationToken cancellationtoken1;
        private CancellationToken cancellationtoken2;

        private static readonly string apiKey = Environment.GetEnvironmentVariable("OPEN_API_KEY"); // 시스템 환경 변수 설정.
        private static readonly string apiEndpoint = "https://api.openai.com/v1/chat/completions";

        public Server()
        {
            InitializeComponent();
        }

        public void Message(string msg)
        {
            this.Invoke(new MethodInvoker(delegate ()
            {
                txtLog.AppendText(DateTime.Now.ToString("[HH:mm:ss] ") + msg + "\r\n");
                txtLog.Focus();
                txtLog.ScrollToCaret();
            }));
        }

        private void Server_FormClosing(object sender, FormClosingEventArgs e)
        {
            ServerStop();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public void ServerStart(CancellationToken token)
        {
            try
            {
                if (!token.IsCancellationRequested)
                {
                    clientList = new Queue<TcpClient>();
                    m_listener = new TcpListener(PORT);
                    m_listener.Start();

                    m_bStop = true;
                    Message("클라이언트 접속 대기 중");

                    while (m_bStop)
                    {
                        TcpClient hClient = m_listener.AcceptTcpClient();
                        lock (clientList)
                        {
                            clientList.Enqueue(hClient);
                        }

                        Task.Run(() => TryProcessNextClient());
                    }
                }
            }
            catch {
                if (!token.IsCancellationRequested)
                {
                    Message("시작 도중에 오류 발생");
                }
            }
        }

        private async Task TryProcessNextClient()
        {
            TcpClient hClient;
            lock (clientList)
            {
                if (m_bConnect || clientList.Count == 0)
                    return;

                hClient = clientList.Dequeue();
                m_bConnect = true;
                Message("클라이언트 접속");
            }
            
            m_Stream = hClient.GetStream();
            m_Read = new StreamReader(m_Stream);
            m_Write = new StreamWriter(m_Stream);

            cancellationTokenSource1 = new CancellationTokenSource();
            cancellationtoken1 = cancellationTokenSource1.Token;

            string messageType = await m_Read.ReadLineAsync();
            if (messageType.Trim().Equals("grading"))
            {
                await Receive(cancellationtoken1);
            }
            else if (messageType.Trim().Equals("sharing"))
            {
                Message("공유 내용 저장");

                string line;
                Dictionary<string, string> fields = new Dictionary<string, string>();

                while ((line = await m_Read.ReadLineAsync()) != null && line.Trim() != "[END]")
                {
                    int colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        string key = line.Substring(0, colonIndex).Trim();
                        string value = line.Substring(colonIndex + 1).Trim();
                        fields[key] = value;
                    }
                }

                try
                {
                    int questionId = int.Parse(fields["QuestionId"]);
                    string userId = fields["UserId"];
                    int score = int.Parse(fields["Score"]);
                    string answer = fields["Answer"];
                    DateTime submittedAt = DateTime.Parse(fields["SubmittedAt"]);

                    using (var db = new AppDbContext())
                    {
                        db.SharedAnswers.Add(new SharedAnswer
                        {
                            QuestionId = questionId,
                            UserId = userId,
                            Score = score,
                            AnswerContent = answer,
                            SubmittedAt = submittedAt
                        });
                        db.SaveChanges();
                    }

                    await Send("공유 저장 완료\n[END]");
                }
                catch (Exception ex)
                {
                    string errorMessage = ex.Message;
                    if (ex.InnerException != null)
                        errorMessage += "\nInner: " + ex.InnerException.Message;

                    await Send($"공유 저장 실패: {errorMessage}\n[END]");
                }
                finally
                {
                    Disconnect();
                }
            }
            else if (messageType.Trim().Equals("loading"))
            {
                Message("공유 내용 로드");
                try
                {
                    using (var db = new AppDbContext())
                    {
                        var list = db.SharedAnswers
                                     .OrderByDescending(a => a.SubmittedAt)
                                     .Select(a => new
                                     {
                                         a.QuestionId,
                                         a.UserId,
                                         a.Score,
                                         a.AnswerContent,
                                         SubmittedAt = a.SubmittedAt.ToString("yyyy-MM-dd HH:mm")
                                     })
                                     .ToList();

                        string json = JsonConvert.SerializeObject(list, Formatting.Indented);
                        await Send(json + "\n[END]");
                    }
                }
                catch (Exception ex)
                {
                    await Send($"공유 답변 로딩 실패: {ex.Message}\n[END]");
                }
                finally
                {
                    Disconnect();
                }
            }

            m_bConnect = false;
            await TryProcessNextClient();
        }

        public void ServerStop()
        {
            if (!m_bStop)
                return;

            m_bStop = false;

            m_listener?.Stop();
            m_Read?.Close();
            m_Write?.Close();
            m_Stream?.Close();

            cancellationTokenSource1?.Cancel();
            cancellationTokenSource2?.Cancel();

            Message("서비스 종료");
        }

        public void Disconnect()
        {
            if (!m_bConnect)
                return;

            m_bConnect = false;

            m_Read?.Close();
            m_Write?.Close();

            m_Stream?.Close();
            cancellationTokenSource1?.Cancel();

            Message("상대방과 연결 중단");
        }

        async public Task Receive(CancellationToken token)
        {
            try
            {
                if (!m_bConnect || token.IsCancellationRequested)
                    return;

                string query = "";
                string line;

                while (!token.IsCancellationRequested)
                {
                    line = await m_Read.ReadLineAsync();

                    if (line == null || line.Trim() == "[END]")
                        break;

                    query += line + "\n";
                }

                if (!string.IsNullOrWhiteSpace(query))
                {
                    Message("GPT 서비스 처리 중");
                    string response = await GetGptResponse(query);
                    await Send(response + "\n[END]");
                }
            }
            catch
            {
                if (!token.IsCancellationRequested)
                {
                    Message("데이터를 읽는 과정에서 오류가 발생");
                }
            }
            finally
            {
                Disconnect();
            }
        }

        async Task Send(string response)
        {
            try
            {
                await m_Write.WriteLineAsync(response);
                await m_Write.FlushAsync();
            }
            catch
            {
                Message("데이터 전송 실패");
            }
        }

        private void btnServer_Click(object sender, EventArgs e)
        {
            if(btnServer.Text=="서버 켜기")
            {
                cancellationTokenSource2 = new CancellationTokenSource();
                cancellationtoken2 = cancellationTokenSource2.Token;
                m_thServer = new Thread(() => ServerStart(cancellationtoken2));

                m_thServer.Start();

                btnServer.Text = "서버 멈춤";
            }
            else
            {
                ServerStop();
                btnServer.Text = "서버 켜기";
            }
            using (var db = new AppDbContext())
            {
                db.Database.Migrate();  // 자동으로 SharedAnswers 테이블 생성
            }
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

        private void Server_Load(object sender, EventArgs e)
        {
            using (var db = new AppDbContext())
            {
                db.Database.Migrate();
            }

            btnServer.FlatStyle = FlatStyle.Flat;
            btnServer.FlatAppearance.BorderSize = 0;
            btnExit.FlatStyle = FlatStyle.Flat;
            btnExit.FlatAppearance.BorderSize = 0;
            btnCommunity.FlatStyle = FlatStyle.Flat;
            btnCommunity.FlatAppearance.BorderSize = 0;
        }
    }
}