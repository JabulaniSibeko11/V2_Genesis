namespace V2_Genesis.Models
{
    public class PropertyModelDisplay
    {
        public Obj_Property_InfoModel PropertyInfo { get; set; }
        public Obj_Section1Model Section1 { get; set; }
        public Obj_Section2Model Section2 { get; set; }
        public Obj_Section3ResModel Section3Res { get; set; }
        public Obj_Section3BusModel Section3Bus { get; set; }
        public Obj_Section3AgriModel Section3Agri { get; set; }
        public Obj_Section4BusModel Section4Bus { get; set; }
        public Obj_Section4ResModel Section4Res { get; set; }
        public Obj_Section5Model Section5 { get; set; }
        public Obj_Section6Model Section6 { get; set; }
        public Obj_Section7Model Section7 { get; set; }
        public Obj_Files Files { get; set; }
        public string AppealStatus { get; set; }
        public string AppealDetails { get; set; }
        public Obj_Property_Info_AppealModel Appeal { get; set; }
    }
}
