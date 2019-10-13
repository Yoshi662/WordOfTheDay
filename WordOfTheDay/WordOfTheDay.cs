using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordOfTheDay
{
    public class WordOfTheDay
    {
        public string es_word { get; set; }
        public string en_word { get; set; }
        public string es_sentence { get; set; }
        public string en_sentence { get; set; }
        public string link { get; set; }

        public WordOfTheDay(string es_word, string en_word, string es_sentence, string en_sentence, string link)
        {
            this.es_word = es_word ?? throw new ArgumentNullException(nameof(es_word));
            this.en_word = en_word ?? throw new ArgumentNullException(nameof(en_word));
            this.es_sentence = es_sentence ?? throw new ArgumentNullException(nameof(es_sentence));
            this.en_sentence = en_sentence ?? throw new ArgumentNullException(nameof(en_sentence));
            this.link = link ?? throw new ArgumentNullException(nameof(link));
        }
    }
}