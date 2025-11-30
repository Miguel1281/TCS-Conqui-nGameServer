using Autofac;
using ConquiánServidor.BusinessLogic;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.ConquiánDB.Repositories; 
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
using ConquiánServidor.Utilities.Email;
using NLog;

namespace ConquiánServidor
{
    public static class Bootstrapper
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public static IContainer Container { get; private set; }
        private static bool isInitialized;
        private static readonly object @lock = new object();

        public static void Init()
        {
            if (isInitialized) return;

            lock (@lock)
            {
                if (isInitialized) return;

                try
                {
                    var builder = new ContainerBuilder();

                    builder.RegisterType<ConquiánDBEntities>().AsSelf().InstancePerDependency();

                    builder.RegisterType<PlayerRepository>().As<IPlayerRepository>();
                    builder.RegisterType<LobbyRepository>().As<ILobbyRepository>();
                    builder.RegisterType<SocialRepository>().As<ISocialRepository>();
                    builder.RegisterType<FriendshipRepository>().As<IFriendshipRepository>();

                    builder.RegisterType<EmailService>().As<IEmailService>();

                    builder.RegisterType<AuthenticationLogic>().AsSelf();
                    builder.RegisterType<LobbyLogic>().AsSelf();
                    builder.RegisterType<UserProfileLogic>().AsSelf();
                    builder.RegisterType<FriendshipLogic>().AsSelf();

                    builder.RegisterType<PresenceManager>().AsSelf().SingleInstance();
                    builder.RegisterType<InvitationManager>().AsSelf().SingleInstance();
                    builder.RegisterType<GuestInvitationManager>().AsSelf().SingleInstance();
                    builder.RegisterType<GameSessionManager>().AsSelf().SingleInstance();
                    builder.RegisterType<LobbySessionManager>().AsSelf().SingleInstance();

                    Container = builder.Build();
                    isInitialized = true;
                }
                catch (System.Exception ex)
                {
                    logger.Fatal(ex, "Error initializing Bootstrapper.");
                    throw;
                }
            }
        }
    }
}