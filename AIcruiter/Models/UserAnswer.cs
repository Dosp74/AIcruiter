using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIcruiter.Models
{
    public class UserAnswer
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string AnswerContent { get; set; }
        public DateTime SubmittedAt { get; set; }
        public Question Question { get; set; }
    }
}