using V2_Genesis.Models.Results.Admin;

namespace V2_Genesis.Services.Interfaces;

public interface IAdminEnquiryNoticeService
{
    AdminEnquiryNotices Build(AdminEnquiryFoundation foundation);
}
