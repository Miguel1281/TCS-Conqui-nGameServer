using ConquiánServidor.BusinessLogic;
using ConquiánServidor.Contracts.ServiceContracts;
using System.ServiceModel;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class Presence : IPresence
    {
        public void Subscribe(int idPlayer)
        {
            var callback = OperationContext.Current.GetCallbackChannel<IPresenceCallback>();
            if (callback != null)
            {
                PresenceManager.Instance.Subscribe(idPlayer, callback);
            }
        }

        public void Unsubscribe(int idPlayer)
        {
            PresenceManager.Instance.Unsubscribe(idPlayer);
        }
    }
}