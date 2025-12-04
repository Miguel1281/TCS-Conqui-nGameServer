using Autofac;
using ConquiánServidor.BusinessLogic;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.ConquiánDB.Repositories; 
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
using ConquiánServidor.Utilities.Email;
using NLog;
using System;

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

                    builder.RegisterType<AuthenticationLogic>().As<IAuthenticationLogic>(); 
                    builder.RegisterType<LobbyLogic>().As<ILobbyLogic>();
                    builder.RegisterType<UserProfileLogic>().As<IUserProfileLogic>();
                    builder.RegisterType<FriendshipLogic>().As<IFriendshipLogic>();

                    builder.RegisterType<PresenceManager>().As<IPresenceManager>().SingleInstance();
                    builder.RegisterType<InvitationManager>().As<IInvitationManager>().SingleInstance();
                    builder.RegisterType<LobbySessionManager>().As<ILobbySessionManager>().SingleInstance();
                    builder.RegisterType<GameSessionManager>().As<IGameSessionManager>().SingleInstance();
                    builder.RegisterType<GuestInvitationManager>().As<IGuestInvitationManager>().SingleInstance();

                    Container = builder.Build();
                    isInitialized = true;
                }
                catch (Exception ex)
                {
                    logger.Fatal(ex, "Error initializing Bootstrapper.");

                    throw new InvalidOperationException("Critical failure: Could not initialize Dependency Injection Container.", ex);
                }
            }
        }
    }
}