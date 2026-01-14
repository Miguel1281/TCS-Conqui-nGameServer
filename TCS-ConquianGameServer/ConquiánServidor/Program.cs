using System;
using System.Collections.Generic;
using System.ServiceModel;
using ConquiánServidor.Services;
using NLog;

namespace ConquiánServidor
{
    public class Program
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly List<ServiceHost> serviceHosts = new List<ServiceHost>();

        public static void Main(string[] args)
        {
            Console.Title = "Conquián Server";

            try
            {
                logger.Info("Starting Conquián Server...");
                PrintBanner();

                StartAllServices();

                Console.WriteLine();
                Console.WriteLine("========================================");
                Console.WriteLine("Todos los servicios están activos.");
                Console.WriteLine("Presiona ENTER para detener el servidor");
                Console.WriteLine("========================================");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Error fatal al iniciar el servidor.");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FATAL] {ex.Message}");
                Console.ResetColor();
                Console.ReadKey();
            }
            finally
            {
                StopAllServices();
            }
        }

        private static void PrintBanner()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("        CONQUIÁN SERVER");
            Console.WriteLine("========================================");
            Console.WriteLine();
        }

        private static void StartAllServices()
        {
            StartService<SignUp>("SignUp");
            StartService<Login>("Login");
            StartService<UserProfile>("UserProfile");
            StartService<FriendList>("FriendList");
            StartService<Invitation>("Invitation");
            StartService<PasswordRecovery>("PasswordRecovery");
            StartService<Presence>("Presence");
            StartService<GuestInvitation>("GuestInvitation");
            StartService<Lobby>("Lobby");
            StartService<Game>("Game");
        }

        private static void StartService<T>(string serviceName) where T : class
        {
            try
            {
                var host = new ServiceHost(typeof(T));
                host.Open();

                serviceHosts.Add(host);

                foreach (var endpoint in host.Description.Endpoints)
                {
                    logger.Info($"{serviceName} escuchando en {endpoint.Address}");
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[OK] Servicio {serviceName} iniciado.");
                Console.ResetColor();
            }
            catch (AddressAlreadyInUseException ex)
            {
                logger.Error(ex, $"Puerto en uso para {serviceName}");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FAIL] {serviceName} - Puerto en uso.");
                Console.ResetColor();
                throw;
            }
            catch (CommunicationException ex)
            {
                logger.Error(ex, $"Error de comunicación en {serviceName}");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FAIL] {serviceName} - {ex.Message}");
                Console.ResetColor();
                throw;
            }
        }

        private static void StopAllServices()
        {
            Console.WriteLine();
            Console.WriteLine("Deteniendo servicios...");

            foreach (var host in serviceHosts)
            {
                try
                {
                    if (host.State == CommunicationState.Opened)
                        host.Close();
                }
                catch
                {
                    host.Abort();
                }
            }

            serviceHosts.Clear();
            logger.Info("Servidor detenido correctamente.");
            Console.WriteLine("Servidor apagado.");
        }
    }
}
