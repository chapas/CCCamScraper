using System.Text;

namespace CCCamScraper.Models
{
    public class OsCamReader // https://www.faalsoft.com/knowledgebase/448/oscamserver.html
    {
        public const string Reader = @"[reader]";
        public string Label { get; set; } = ""; // same as label
        public string Description { get; set; } // error;off;unknown;no data;username
        public string Enable { get; set; } = "1";
        public string Protocol { get; set; } = "cccam"; //cccam ou newcam
        public string Key { get; set; } = "";
        public string Device { get; set; } = ""; //IP ou URL
        public string Port { get; set; } = ""; //device port => "device,port"
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
        public string Group { get; set; } = "1";
        public string Inactivitytimeout { get; set; } = "30";
        public string Reconnecttimeout { get; set; } = "30";
        public string LbWeight { get; set; } = "100";
        public string Cccversion { get; set; } = "2.1.2";
        public string Cccmaxhops { get; set; } = "10";
        public string Cccwantemu { get; set; } = "0";
        public string Ccckeepalive { get; set; } = "0";

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(Reader);
            sb.AppendLine("label                    = " + Label);
            if (!string.IsNullOrEmpty(Description))
                sb.AppendLine("description              = " + Description);
            sb.AppendLine("enable                   = " + Enable);
            sb.AppendLine("protocol                 = " + Protocol);
            if (!string.IsNullOrEmpty(Key))
                sb.AppendLine("key= " + Key);
            sb.AppendLine("device                   = " + Device + "," + Port);
            sb.AppendLine("user                     = " + User);
            sb.AppendLine("password                 = " + Password);
            sb.AppendLine("group                    = " + Group);
            sb.AppendLine("inactivitytimeout        = " + Inactivitytimeout);
            sb.AppendLine("reconnecttimeout         = " + Reconnecttimeout);
            sb.AppendLine("lb_weight                = " + LbWeight); 
            sb.AppendLine("cccversion               = " + Cccversion);
            sb.AppendLine("cccmaxhops               = " + Cccmaxhops);
            if (!string.IsNullOrEmpty(Cccwantemu))
                sb.AppendLine("cccwantemu               = " + Cccwantemu);
            sb.AppendLine("ccckeepalive             = " + Ccckeepalive);
            sb.AppendLine();

            return sb.ToString();
        }

        public void UpdateNewFoundStateOnDescription(string newFoundState)
        {
            var readerDescriptionModel = new OsCamReaderDescription(Description);
            readerDescriptionModel.UpdateDescriptionWithNewData(newFoundState);
            Description = readerDescriptionModel.ToString();
        }
    }
}