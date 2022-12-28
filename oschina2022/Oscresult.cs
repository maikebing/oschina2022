namespace oschina2022
{
    public class Oscresult
    {
        public Oscresult()
        {

        }
        public Oscresult(string user, string project, string token, int counter)
        {
            this.user = user;
            this.project = project;
            this.token = token;
            this.counter = counter;
        }

        public string user { get; set; }
        public string project { get; set; }
        public string token { get; set; }
        public int counter { get; set; }
    }
}