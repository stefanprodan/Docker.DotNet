using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Docker.DotNet.Models;
using System.IO;
using System.Threading;
namespace Docker.DotNet.Examples
{
    public class Program
    {
        private static DockerClient docker;

        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();

            var config = builder.Build();

            docker = new DockerClientConfiguration(new Uri(config["DockerHost"])).CreateClient();
            
            Console.WriteLine($"Connected to {docker.Configuration.EndpointBaseUri}");

            // bug: https://github.com/Microsoft/Docker.DotNet/issues/116
            //var info = docker.Miscellaneous.GetSystemInfoAsync().Result;

            var containers = docker.Containers.ListContainersAsync(new ContainersListParameters()
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["status"] = new Dictionary<string, bool>
                    {
                        ["exited"] = true
                    }
                }
            }).Result;

            foreach (var container in containers)
            {
                Console.WriteLine(JsonConvert.SerializeObject(container));
            }

            Stats((docker.Containers.ListContainersAsync(new ContainersListParameters()).Result)[0].ID);

            //docker.Images.CreateImageAsync(new ImagesCreateParameters()
            //{
            //    Parent = "fedora/memcached",
            //    Tag = "latest"
            //}, new AuthConfig()
            //{
            //    Email = "ahmetb@microsoft.com",
            //    Username = "ahmetalpbalkan",
            //    Password = "pa$$w0rd"
            //});

            //docker.Containers.StartContainerAsync("39e3317fd258", new HostConfig()
            //{
            //    DNS = new[] { "8.8.8.8", "8.8.4.4" }
            //});

            //docker.Containers.StopContainerAsync("39e3317fd258", new ContainerStopParameters()
            //{
            //    WaitBeforeKillSeconds = 30
            //}, CancellationToken.None);



            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }

        public static void MonitorEvents()
        {
            Task.Factory.StartNew(() =>
            {
                using (var stream = docker.Miscellaneous.MonitorEventsAsync(new ContainerEventsParameters(), CancellationToken.None).Result)
                {
                    using (var sr = new StreamReader(stream))
                    {
                        while (stream.CanRead)
                        {
                            Console.WriteLine(sr.ReadLine());
                        }
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        public static void Stats(string id)
        {
            Task.Factory.StartNew(() =>
            {
                using (var stream = docker.Containers.GetContainerStatsAsync(id, new ContainerStatsParameters() { Stream = false }, CancellationToken.None).Result)
                {
                    using (var sr = new StreamReader(stream))
                    {
                        Console.WriteLine(sr.ReadLine());
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }
    }
}
