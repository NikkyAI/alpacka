namespace GitMC.Lib.MultiMC
{
    public class Instance
    {
        public string InstanceName { get; set; }
        public string IntendedVersion { get; set; }
        public string Notes { get; set; } = ""; //TODO: use description
        public string InstanceType { get; set; } = "OneSix";
        
        public override string ToString() =>
            $"name={ InstanceName }\n" +
            $"IntendedVersion={ IntendedVersion }\n" +
            $"notes={ Notes }\n" +
            $"InstanceType={ InstanceType }";
    }
}