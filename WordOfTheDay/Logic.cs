using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WordOfTheDay
{
    public static class Logic
    {
        public static WordOfTheDay GetXMLWOTD()
        {
            List<String> Strings = new List<string>();

            XmlDocument rssXmlDoc = new XmlDocument();

            // Load the RSS file from the RSS URL
            rssXmlDoc.Load("http://feeds.feedblitz.com/spanish-word-of-the-day&x=1"); //Rss 

            String rawUnparsedXML = rssXmlDoc.OuterXml;
            rawUnparsedXML = rawUnparsedXML.Replace("<![CDATA[", "");
            rawUnparsedXML = rawUnparsedXML.Replace("]]>", "");
            rawUnparsedXML = rawUnparsedXML.Replace("</content:encoded>", "</Img></content:encoded>"); //Creo que esto es lo mas hardcoded que he visto jamas
            rssXmlDoc.LoadXml(rawUnparsedXML);





            Strings.Add(rssXmlDoc.SelectSingleNode("rss/channel/link").InnerText);

            XmlNodeList NodeListTitle = rssXmlDoc.SelectNodes("//title"); //Por que el RSS tiene dos titulos y necesito el segundo

            foreach (XmlNode rssNode in NodeListTitle)
            {
                Strings.Add(rssNode.InnerText);
            }

            XmlNodeList rsstable = rssXmlDoc.SelectNodes("rss/channel/item/description/table/tr");

            foreach (XmlNode rssNode in rsstable)
            {
                XmlNode rssSubNode = rssNode.SelectSingleNode("th");
                if (rssSubNode != null) Strings.Add(rssSubNode.InnerText);

                rssSubNode = rssNode.SelectSingleNode("td");
                if (rssSubNode != null) Strings.Add(rssSubNode.InnerText);
            }
            Strings.AddRange(Strings[2].Split(':')); //Separo uno de los strings dentro del string en otros dos strings ("test:test2") => ("test","test2")


            //PARSE FROM STRING[] TO WOTD
            //Vamos a hacerlo bonito
            String es_word = FirstCharToUpper(Strings[9]);
            String en_word = FirstCharToUpper(Strings[10]);
            String es_sentence = Strings[6];
            String en_sentence = Strings[8];
            String link = Strings[0];

            WordOfTheDay WOTD = new WordOfTheDay(es_word, en_word, es_sentence, en_sentence, link);

            return WOTD;

        }


        public static string FirstCharToUpper(this string input) //thx STACK
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default: return input.First().ToString().ToUpper() + input.Substring(1);
            }
        }
    }
}