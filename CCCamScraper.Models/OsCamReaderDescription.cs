using System.Text;

namespace CCCamScraper.Models
{
    public class OsCamReaderDescription
    {
        public OsCamReaderDescription(string description)
        {
            var statusArray = description.Split(';');

            if (statusArray.Length != 5)
                statusArray = new[] { "0", "0", "0", "0", "0" }; //don't care whats on description field, gets 0;0;0;0

            Error = int.Parse(statusArray[0]);
            Off = int.Parse(statusArray[1]);
            Unknown = int.Parse(statusArray[2]);
            LbValueReader = int.Parse(statusArray[3]);
            Username = string.IsNullOrEmpty(statusArray[4]) ? "" : statusArray[4];
        }

        private int Error { get; set; }
        private int Off { get; set; }
        private int Unknown { get; set; }
        private int LbValueReader { get; set; }
        public string Username { get; set; } = "";

        public void UpdateDescriptionWithNewData(string newFoundState)
        {
            switch (newFoundState.ToLower())
            {
                case "off":
                    Off += 1;
                    break;
                case "unknown":
                    Unknown += 1;
                    break;
                case "error":
                    Error += 1;
                    break;
                case "lbvaluereader":
                    LbValueReader += 1;
                    break;
                default:
                    //connected to server, so reset fail counters
                    Off = 0;
                    Unknown = 0;
                    Error = 0;
                    LbValueReader = 0;
                    break;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(Error.ToString());
            sb.Append(";");
            sb.Append(Off.ToString());
            sb.Append(";");
            sb.Append(Unknown.ToString());
            sb.Append(";");
            sb.Append(LbValueReader.ToString());
            sb.Append(";");
            sb.Append(Username);

            return sb.ToString();
        }
    }
}