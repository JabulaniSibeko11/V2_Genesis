using V2_Genesis.Models;
using V2_Genesis.Models.Results;

namespace V2_Genesis.Services.Interfaces
{
    public interface ISubmittedFormPdfService
    {
        Task<SubmittedFormPdfResult> GenerateObjectionOrAppealFormAsync(
            bool isAppeal,
            string folderPath,
            Obj_Property_InfoModel obj,
            Obj_Property_Info_AppealModel? appeal,
            Obj_Section1Model obj1,
            Obj_Section2Model obj2,
            Obj_Section3ResModel objR3,
            Obj_Section3BusModel objB3,
            Obj_Section3AgriModel objA3,
            Obj_Section4BusModel objB4,
            Obj_Section4ResModel objR4,
            Obj_Section5Model obj5,
            Obj_Section6Model obj6,
            Obj_Section7Model obj7,
            DateTime? dateSubmitted = null);

        Task<SubmittedFormPdfResult> GenerateSection78FormAsync(
            bool isReview,
            string folderPath,
            Que_Property_InfoModel que,
            Obj_Section1Model obj1,
            Obj_Section2Model obj2,
            Obj_Section2QueryModel que1,
            Obj_Section3ResModel objR3,
            Obj_Section3BusModel objB3,
            Obj_Section3AgriModel objA3,
            Obj_Section4BusModel objB4,
            Obj_Section4ResModel objR4,
            Obj_Section5Model obj5,
            Obj_Section6Model obj6,
            Obj_Section7Model obj7,
            DateTime? dateSubmitted = null);
    }

}
