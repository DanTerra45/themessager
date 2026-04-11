using System.Globalization;
using System.Text.Json;

namespace Mercadito.Pages.Infrastructure
{
    public readonly record struct PendingModalDraftState<T>(bool ShouldShowModal, T? Draft) where T : class;

    public interface IModalPostbackStateService
    {
        void StorePendingModalDraft<T>(ISession session, string modalSessionKey, string draftSessionKey, T draft) where T : class;
        PendingModalDraftState<T> RestorePendingModalDraft<T>(ISession session, string modalSessionKey, string draftSessionKey, ILogger logger) where T : class;
        void SetPendingEntityId(ISession session, string sessionKey, long entityId);
        long PopPendingEntityId(ISession session, string sessionKey);
        void ClearPendingEntityId(ISession session, string sessionKey);
    }

    public sealed class ModalPostbackStateService : IModalPostbackStateService
    {
        public void StorePendingModalDraft<T>(ISession session, string modalSessionKey, string draftSessionKey, T draft) where T : class
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(draft);

            session.SetString(modalSessionKey, bool.TrueString);
            session.SetString(draftSessionKey, JsonSerializer.Serialize(draft));
        }

        public PendingModalDraftState<T> RestorePendingModalDraft<T>(ISession session, string modalSessionKey, string draftSessionKey, ILogger logger) where T : class
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(logger);

            if (!PopFlag(session, modalSessionKey))
            {
                session.Remove(draftSessionKey);
                return new PendingModalDraftState<T>(false, null);
            }

            var draft = PopDraft<T>(session, draftSessionKey, logger);
            return new PendingModalDraftState<T>(true, draft);
        }

        public void SetPendingEntityId(ISession session, string sessionKey, long entityId)
        {
            ArgumentNullException.ThrowIfNull(session);

            session.SetString(sessionKey, entityId.ToString(CultureInfo.InvariantCulture));
        }

        public long PopPendingEntityId(ISession session, string sessionKey)
        {
            ArgumentNullException.ThrowIfNull(session);

            var rawEntityId = session.GetString(sessionKey);
            session.Remove(sessionKey);

            if (long.TryParse(rawEntityId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var entityId))
            {
                return entityId;
            }

            return 0;
        }

        public void ClearPendingEntityId(ISession session, string sessionKey)
        {
            ArgumentNullException.ThrowIfNull(session);

            session.Remove(sessionKey);
        }

        private static bool PopFlag(ISession session, string sessionKey)
        {
            var rawValue = session.GetString(sessionKey);
            session.Remove(sessionKey);

            return bool.TryParse(rawValue, out var parsedValue) && parsedValue;
        }

        private static T? PopDraft<T>(ISession session, string sessionKey, ILogger logger) where T : class
        {
            var rawValue = session.GetString(sessionKey);
            session.Remove(sessionKey);

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(rawValue);
            }
            catch (JsonException exception)
            {
                logger.LogWarning(exception, "No se pudo restaurar el borrador temporal de modal para key {SessionKey}", sessionKey);
                return null;
            }
        }
    }
}
