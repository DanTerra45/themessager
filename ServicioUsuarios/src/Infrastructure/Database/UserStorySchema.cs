using Application.Options;
using Domain.Database;
using Domain.Database.Fields;

namespace Infrastructure.Database
{
    public sealed class UserStorySchema: ITableSchema<UserStoryFields>
    {
        private static readonly Dictionary<UserStoryFields, Dictionary<SqlAction, string>> Fields = new()
        {
            {
                UserStoryFields.Id,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "id AS Id" },
                    { SqlAction.Insert, "id" },
                    { SqlAction.Update, "id" },
                    { SqlAction.Delete, "id" }
                }
            },
            {
                UserStoryFields.UserId,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "user_id AS UserId" },
                    { SqlAction.Insert, "user_id" },
                    { SqlAction.Update, "user_id" },
                    { SqlAction.Delete, "user_id" }
                }
            },
            {
                UserStoryFields.OperatorId,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "operator_id AS OperatorId" },
                    { SqlAction.Insert, "operator_id" },
                    { SqlAction.Update, "operator_id" },
                    { SqlAction.Delete, "operator_id" }
                }
            },
            {
                UserStoryFields.PreviousState,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "previous_state AS PreviousState" },
                    { SqlAction.Insert, "previous_state" },
                    { SqlAction.Update, "previous_state" },
                    { SqlAction.Delete, "previous_state" }
                }
            },
            {
                UserStoryFields.NewState,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "actual_state AS NewState" },
                    { SqlAction.Insert, "actual_state" },
                    { SqlAction.Update, "actual_state" },
                    { SqlAction.Delete, "actual_state" }
                }
            },
            {
                UserStoryFields.DisableReason,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "disable_reason AS DisableReason" },
                    { SqlAction.Insert, "disable_reason" },
                    { SqlAction.Update, "disable_reason" },
                    { SqlAction.Delete, "disable_reason" }
                }
            },
            {
                UserStoryFields.CreatedAt,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "created_at AS CreatedAt" },
                    { SqlAction.Insert, "created_at" },
                    { SqlAction.Update, "created_at" },
                    { SqlAction.Delete, "created_at" }
                }
            }
        };

        public string GetAll() => string.Join(", ", Fields.Values.Select(actions => actions[SqlAction.Select]));
        public string Get(UserStoryFields field, SqlAction action)  => Fields.TryGetValue(field, out var actions) && actions.TryGetValue(action, out var value) ? value : throw new Exception($"Field {field} with action {action} not found in schema.");
    }
}