using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;

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
                txtLog.AppendText(msg + "\r\n");
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
                //답변 공유 서비스 구현
            }
            else if (messageType.Trim().Equals("loading"))
            {
                //커뮤니티 로드 서비스 구현
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
                    Message(query);
                    Message("GPT 전송 중");
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

                Message(response);
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