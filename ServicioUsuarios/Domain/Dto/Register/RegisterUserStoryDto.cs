using Domain.Entities.Enums;

namespace Domain.Dto.Register
{
    public record RegisterUserStoryDto(
        int UserId,
        string OperatorId,
        UserState? PreviousState,
        UserState? NewState,
        string DisableReason,
        DateTime? CreatedAt
    )
    {
        public DateTime? CreatedAt { get; init; } = CreatedAt ?? DateTime.UtcNow;
        public UserState? PreviousState { get; init; } = PreviousState ?? UserState.Active;
        public UserState? NewState { get; init; } = NewState ?? UserState.Inactive;

        public static RegisterUserStoryDto ToDisable(int userId,int operatorId, string reason)
        {
            return new RegisterUserStoryDto(
                UserId: userId,
                OperatorId: operatorId.ToString(),
                PreviousState: UserState.Active,
                NewState: UserState.Inactive,
                DisableReason: reason,
                CreatedAt: DateTime.UtcNow
            );
        }
    }
}