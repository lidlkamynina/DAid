using System;
using System.Threading;
using System.Threading.Tasks;
using DAid.Servers;

namespace DAid.Clients
{
    public class Client
    {
        private readonly Server _server;

        /// <summary>
        /// Initializes the client with a server instance.
        /// </summary>
        public Client(Server server)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
        }

        /// <summary>
        /// Starts the client, handles user commands, and communicates with the server.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Client started. Enter commands: connect, calibrate, start, stop, exit");

            while (!cancellationToken.IsCancellationRequested)
            {
                Console.Write("> ");
                string command = Console.ReadLine()?.Trim().ToLower();

                if (string.IsNullOrWhiteSpace(command)) continue;

                if (command == "exit")
                {
                    Console.WriteLine("Stopping client...");
                    _server.Stop();
                    break;
                }

                try
                {
                    await _server.HandleCommandAsync(command, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing command '{command}': {ex.Message}");
                }
            }
        }
    }
}
