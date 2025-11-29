using Autofac;
using ConquiánServidor.BusinessLogic;
using ConquiánServidor.Contracts.ServiceContracts;
using System.ServiceModel;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class Presence : IPresence
    {
        private readonly PresenceManager presenceManager;

        public Presence()
        {
            Bootstrapper.Init();
            this.presenceManager = Bootstrapper.Container.Resolve<PresenceManager>();
        }

        public Presence(PresenceManager presenceManager)
        {
            this.presenceManager = presenceManager;
        }
        public void Subscribe(int idPlayer)
        {
            var callback = OperationContext.Current.GetCallbackChannel<IPresenceCallback>();
            if (callback != null)
            {
                presenceManager.Subscribe(idPlayer, callback);
            }
        }

        public void Unsubscribe(int idPlayer)
        {
            presenceManager.Unsubscribe(idPlayer);
        }
    }
}