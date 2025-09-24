using System;

public class Transaction
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime DateTime { get; set; }
    public string OcrPlate { get; set; }
    public int OcrAccuracy { get; set; }
    public int Direction { get; set; }
    public int LaneId { get; set; }
    public int CameraId { get; set; }
    public string Image1 { get; set; }
    public string Image2 { get; set; }
    public string Image3 { get; set; }
    public int Sent { get; set; }
    public DateTime SentDateTime { get; set; }
}
