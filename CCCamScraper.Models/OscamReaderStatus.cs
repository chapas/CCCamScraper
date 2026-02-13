namespace CCCamScraper.Models;

public class OscamUIReaderStatus
{
    public string OnOff { get; set; } = string.Empty;
    public string Reader { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public int OK { get; set; }
    public int NOK { get; set; }
    public int TOut { get; set; }
}