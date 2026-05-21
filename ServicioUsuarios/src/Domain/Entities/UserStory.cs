using Domain.Entities.Enums;

namespace Domain.Entities;

public class UserStory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int OperatorId { get; set; }
    public UserState PreviousState { get; set; }
    public UserState ActualState { get; set; }
    public string DisableReason { get; set; }
    public DateTime CreatedAt { get; set; }
}