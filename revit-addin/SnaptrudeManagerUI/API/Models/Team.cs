namespace SnaptrudeManagerUI.API
{
    internal class Team
    {
        public bool isManuallyPaid { get; set; }
        public string role { get; set; }
        public string name { get; set; }
        public int id { get; set; }
        public string manualPlanLastsUntil { get; set; }
    }
}