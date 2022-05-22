using System;
using System.Text;

namespace CCCamScraper.Models
{
    public class OsCamReaderDescription
    {
        int Error { get; set; }
        int Off { get; set; }
        int Unknown { get; set; }
        int LbValueReader { get; set; }
        public string Username { get; set; } = "";

        public OsCamReaderDescription(string description)
        {
            var statusArray = description.Split(';', StringSplitOptions.None);

            if (statusArray.Length != 5)
                statusArray = new[] {"0", "0", "0", "0", "0"}; //don't care whats on description field, gets 0;0;0;0

                Error = int.Parse(statusArray[0]);
                Off = int.Parse(statusArray[1]);
                Unknown = int.Parse(statusArray[2]);
                LbValueReader = int.Parse(statusArray[3]);
                Username =  string.IsNullOrEmpty(statusArray[4]) ? "" : statusArray[4];
        }

        public void UpdateDescriptionWithNewData(string newFoundState)
        {
            switch (newFoundState.ToLower())
            {
                case "off":
                    this.Off += 1;
                    break;
                case "unknown":
                    this.Unknown += 1;
                    break;
                case "error":
                    this.Error += 1;
                    break;
                case "lbvaluereader":
                    this.LbValueReader += 1;
                    break;
                default:
                    //connected to server, so reset fail counters
                    this.Off = 0;
                    this.Unknown = 0;
                    this.Error = 0;
                    this.LbValueReader = 0;
                    break;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Error.ToString());
            sb.Append(";");
            sb.Append(Off.ToString());
            sb.Append(";");
            sb.Append(Unknown.ToString());
            sb.Append(";");
            sb.Append(LbValueReader.ToString());
            sb.Append(";");
            sb.Append(Username.ToString());

            return sb.ToString();
        }
    }
}
