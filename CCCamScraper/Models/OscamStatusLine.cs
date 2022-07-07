namespace CCCamScraper.Models
{
    public class OscamUiStatusLine
    {
        public string Hide { get; set; }
        public string Reset { get; set; }
        public string ReaderUser { get; set; }
        public string Au { get; set; }
        public string Address { get; set; }
        public string Port { get; set; }
        public string Protocol { get; set; }
        public string SrvidCaidProvid { get; set; }
        public string LastChannel { get; set; }
        public string LbValueReader { get; set; }
        public string OnlineIdle { get; set; }
        public string Status { get; set; }

        /// <summary>
        ///     Description is shown on 'Reader/User' field hint
        /// </summary>
        public string Description { get; set; }

        public OsCamReaderDescription OsCamReaderDescription => new OsCamReaderDescription(Description);
    }
}