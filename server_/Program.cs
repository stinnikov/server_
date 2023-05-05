using Microsoft.EntityFrameworkCore;
using Nominatim.API.Geocoders;
using Nominatim.API.Web;
using server_;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using server_.Users.Model;


namespace server_
{
    static class Program
    {
        public static MauiContext mauiContext = new MauiContext();
        public static List<User> Users { get; set; }
        public static IHttpClientFactory httpClientFactory = new DefaultHttpClientFactory();

        static async Task Main()
        {

            TcpListener tcpListener = new TcpListener(IPAddress.Any, 8888);
            mauiContext.Database.EnsureDeleted();
            mauiContext.Database.EnsureCreated();

            mauiContext.Users.Load();
            Users = mauiContext.Users.Local.ToList();
            try
            {
                tcpListener.Start();    // запускаем сервер
                Console.WriteLine("Сервер запущен. Ожидание подключений... ");
                while (true)
                {
                    var tcpClient = await tcpListener.AcceptTcpClientAsync();
                    Task.Run(async () => await ProcessClientAsync(tcpClient));
                }
            }
            finally
            {
                tcpListener.Stop();
            }
            async Task ProcessClientAsync(TcpClient tcpClient)
            {
                var stream = tcpClient.GetStream();
                using var streamReader = new StreamReader(stream);
                using var streamWriter = new StreamWriter(stream);
                List<string> entries = new List<string>();
                var destination = await streamReader.ReadLineAsync();
                if (destination == "REGADD")
                {
                    while (true)
                    {
                        try
                        {
                            var entry = await streamReader.ReadLineAsync();
                            //name -> number -> email -> password
                            entries.Add(entry);
                            if (entry == "END")
                            {
                                if (EntriesValidation(entries[1], entries[2]))
                                {
                                    mauiContext.Users.Add(new User(entries[0], entries[1], entries[2], entries[3]));
                                    mauiContext.SaveChanges();
                                    Users.Add(new User(entries[0], entries[1], entries[2], entries[3]));
                                    string response = "1";
                                    Console.WriteLine($"Запрос:{destination}, Поле: {entry}, Ответ:{response}");

                                    await streamWriter.WriteLineAsync(response);
                                }
                                else
                                {
                                    string response = "0";
                                    Console.WriteLine($"Запрос:{destination}, Поле: {entry}, Ответ:{response}");
                                    await streamWriter.WriteLineAsync(response);
                                }
                                await streamWriter.FlushAsync();
                                break;
                            }
                            Console.WriteLine($"Поле: {entry}");
                            //await streamWriter.WriteLineAsync("0");
                        }
                        catch (System.Exception)
                        {

                        }
                    }
                }
                else if (destination == "REMOVEUSER")
                {
                    //TODO: remove
                }
                else if (destination == "LOGIN")
                {
                    while (true)
                    {
                        try
                        {
                            var entry = await streamReader.ReadLineAsync();
                            //number -> password
                            entries.Add(entry);
                            if (entry == "END")
                            {
                                if (LoginValidation(entries[0], entries[1]))
                                {
                                    string response = "1";
                                    Console.WriteLine($"Запрос:{destination}, Поле: {entry}, Ответ:{response}");
                                    await streamWriter.WriteLineAsync(response);
                                }
                                else
                                {
                                    string response = "0";
                                    Console.WriteLine($"Запрос:{destination}, Поле: {entry}, Ответ:{response}");
                                    await streamWriter.WriteLineAsync(response);
                                }
                                await streamWriter.FlushAsync();
                                return;
                            }
                            Console.WriteLine($"Запрос:{destination}, Поле: {entry}");
                            //await streamWriter.WriteLineAsync("0");
                        }
                        catch (System.Exception)
                        {

                        }
                    }
                }
                else if (destination == "LOGOUT")
                {

                }
                else if (destination == "GETLOC")
                {
                    while (true)
                    {
                        var entry = await streamReader.ReadLineAsync();
                        entries.Add(entry);
                        if (entry == "END")
                        {
                            //TODO:обработать темку по локации
                            string address = "";
                            foreach(var element in entries)
                            {
                                if(element == "END")
                                {
                                    break;
                                }
                                address += element;
                            }
                            GetLonLatFromAddress(address.ToLower());
                        }
                    }
                }
            }
            static async Task<byte[]> GetUserAvatar(int userId)
            {
                // Получаем путь к файлу аватара пользователя
                string userAvatarFilePath = Path.Combine(GetUserFolderPath(userId), "avatar.jpg");

                if (!System.IO.File.Exists(userAvatarFilePath))
                {
                    // Если аватар не найден, возвращаем стандартное изображение
                    userAvatarFilePath = Path.Combine(Environment.CurrentDirectory, "Images", "default-avatar.jpg");
                }

                // Читаем файл и возвращаем его в виде массива байтов
                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(userAvatarFilePath);
                return fileBytes;
            }
            static string GetUserFolderPath(int userId)
            {
                string userFolderPath = Path.Combine(Environment.CurrentDirectory, "UserAvatars", userId.ToString());
                Directory.CreateDirectory(userFolderPath);
                return userFolderPath;
            }
            static void GetLonLatFromAddress(string address)
            {
                //using IHost host = Host.CreateDefaultBuilder()
                //.ConfigureServices(services =>
                //{
                //    services.AddHttpClient();
                //})
                //.Build();
                string[] address_points = address.Split(' ');
                string country;
                string city;
                string street;
                switch (address_points.Length)
                {
                    case 4:
                        country = address_points[0];
                        city = address_points[1];
                        street = address_points[2] + " " + address_points[3];
                        break;
                    case 5:
                        country = address_points[0];
                        city = address_points[1];
                        street = address_points[2] + " " + address_points[3] + " " + address_points[4];
                        break;
                }
                NominatimWebInterface nominatim = new(httpClientFactory);
                ForwardGeocoder forwardGeocoder = new ForwardGeocoder(nominatim);
                var a = forwardGeocoder.Geocode(new Nominatim.API.Models.ForwardGeocodeRequest() { City = "", StreetAddress = "Базинская 5", }).Result;
            }
            static bool EntriesValidation(string number, string email)
            {
                if (NumberValidation(number))
                {
                    if (EmailValidation(email))
                    {
                        return true;
                    }
                }
                return false;
            }
            static bool NumberValidation(string number)
            {
                if (!Users.Any(user => user.Number == number))
                {
                    return true;
                }
                return false;
            }
            static bool EmailValidation(string email)
            {
                if (!Users.Any(user => user.Email == email))
                {
                    return true;
                }
                return false;
            }
            static bool LoginValidation(string number, string password)
            {
                if (Users.Any(user => user.Number == number && user.Password == password))
                {
                    return true;
                }
                return false;
            }
        }
    }
}

