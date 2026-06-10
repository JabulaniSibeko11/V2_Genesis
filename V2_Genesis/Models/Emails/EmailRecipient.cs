namespace V2_Genesis.Models.Emails
{
    public record EmailRecipient(
        string Name,
        string Address, 
        string RecipientType = "Client");
}
