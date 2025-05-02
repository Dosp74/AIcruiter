using System;
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
    public partial class Answer : Form
    {
        //Form1의 questions를 받아올 리스트
        List<Form1.Question> questions;

        public Answer(List<Form1.Question> list)
        {
            InitializeComponent();
            questions = list;
        }

        private void Answer_Load(object sender, EventArgs e)
        {
            foreach (Form1.Question q in questions) {
                string question = q.question;
                string answer = string.IsNullOrWhiteSpace(q.answer) ? "AI 평가 기준에 따릅니다." : q.answer;

                AnswerNote.Text += "질문 : " + question + "\r\n";
                AnswerNote.Text += "답 : " + answer + "\r\n\r\n\r\n";
            }
        }
    }
}