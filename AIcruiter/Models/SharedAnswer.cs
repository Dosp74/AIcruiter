using System;
using System.ComponentModel.DataAnnotations;

namespace AICruiter_Server.Models
{
    public class SharedAnswer
    {
        [Key]
        public int Id { get; set; }

        public int QuestionId { get; set; }
        public string UserId { get; set; }
        public int Score { get; set; }
        public string AnswerContent { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}