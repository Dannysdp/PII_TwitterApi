using System;
using System.Threading.Tasks;

namespace TwitterUCU
{
    class Program
    {
        static void Main(string[] args)
        {
            var twitter = new TwitterImage();
            Console.WriteLine(twitter.PublishToTwitter("New Employee 2! ",@"bill2.jpg"));
            var twitterDirectMessage = new TwitterMessage();
            Console.WriteLine(twitterDirectMessage.SendMessage("Hola!", "1396065818"));
        }
    }
}