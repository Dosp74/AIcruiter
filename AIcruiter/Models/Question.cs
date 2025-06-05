using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIcruiter.Models
{
    public class Question
    {
        public int Id { get; set; } // idx
        public string Text { get; set; } // question
        public string Answer { get; set; }
        public string Category { get; set; }

        public ICollection<UserAnswer> UserAnswers { get; set; }
    }
}