using Autofac;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.ServiceContracts;
using System.ServiceModel;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class Presence : IPresence
    {
        private readonly IPresenceManager presenceManager;

        public Presence()
        {
            Bootstrapper.Init();
            this.presenceManager = Bootstrapper.Container.Resolve<IPresenceManager>();
        }

        public Presence(IPresenceManager presenceManager)
        {
            this.presenceManager = presenceManager;
        }

        public void Subscribe(int idPlayer)
        {
            var callback = OperationContext.Current.GetCallbackChannel<IPresenceCallback>();
            if (callback != null)
            {

                ICommunicationObject channel = (ICommunicationObject)callback;

                channel.Closed += (sender, e) =>
                {
                    presenceManager.DisconnectUser(idPlayer);
                };

                channel.Faulted += (sender, e) =>
                {
                    presenceManager.DisconnectUser(idPlayer);
                };

                presenceManager.Subscribe(idPlayer, callback);
            }
        }

        public void Unsubscribe(int idPlayer)
        {
            presenceManager.Unsubscribe(idPlayer);
        }
    }
}