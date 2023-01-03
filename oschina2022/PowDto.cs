namespace oschina2022
{

    public class pow_data
    {
        /// <summary>
        /// 
        /// </summary>
        public int integral { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int project { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int counter { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int user { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string token { get; set; }
    }

    public class pow_result
    {
        public pow_result()
        {
            data = new List<pow_data>();
        }
        /// <summary>
        /// 
        /// </summary>
        public bool result { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string msg { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<pow_data> data { get; set; }
    }

    public class PowDto
    {
        public PowDto()
        {

        }
        public PowDto(string user, string project, string token, int counter)
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