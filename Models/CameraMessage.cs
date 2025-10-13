using System.Collections.Generic;

namespace Ava
{
    public class PlatePosition
    {
        public string? X { get; set; }
        public string? Y { get; set; }
    }

    public class Tracking
    {
        public string? Timestamp { get; set; }
        public string? X { get; set; }
        public string? Y { get; set; }
        public string? CharacterHeight { get; set; }
    }

    public class CameraMessage
    {
        public string? MessageType { get; set; }
        public string? Exposure { get; set; }
        public string? Gain { get; set; }
        public string? CaptureTimeStamp { get; set; }
        public string? Vrm { get; set; }
        public string? Confidence { get; set; }
        public string? ConfidenceOfPresence { get; set; }
        public string? FirstSeenWallClock { get; set; }
        public string? LastSeenWallClock { get; set; }
        public string? Direction { get; set; }
        public string? LogicalDirection { get; set; }
        public string? ImageFormat { get; set; }
        public PlatePosition? PlatePosition { get; set; }
        public string? CharacterHeight { get; set; }
        public string? TrackingId { get; set; }
        public string? IsNewVehicle { get; set; }
        public List<Tracking>? Tracking { get; set; }
        public Dictionary<string, string>? Images { get; set; }
        public string? IsPartial { get; set; }
        public string? InstanceId { get; set; }
        public string? Country { get; set; }
        public string? CountryConfidence { get; set; }
        public string? CameraSerial { get; set; }
        public string? PatchImageIndex { get; set; }
        public string? OverviewImageIndex { get; set; }
        public string? PrimaryImageIndex { get; set; }
        public string? CroppedPrimaryImageIndex { get; set; }
    }
}
