using Microsoft.EntityFrameworkCore;
using Nominatim.API.Geocoders;
using Nominatim.API.Web;
using server_;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using server_.User.Model;
using System.IO;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using Newtonsoft.Json;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net.WebSockets;
using Newtonsoft.Json.Linq;
using System.Linq;
using server_.Order.Model;

namespace server_
{
    static class Program
    {
        public const string projectFolderPath = "C:\\Users\\XE\\source\\repos\\server_\\server_\\User\\Users";
        public const string sercretKey = "we_have_to_be_the_greatest_than_256_bits";
        public const bool isdbneeded = false;
        public const bool isDatabaseNeedToDeletedBefore = false;
        public const string AdminLogin = "Admin";
        public const string AdminPassword = "Admin";

        public static MauiContext mauiContext = new MauiContext();

        public static List<UserModel> Users { get; set; } = new()
        {
            new UserModel("admin", "admin", "admin", "admin@mail.ru", "admin", UserType.Administrator),
            new UserModel("client", "client", "client", "client@mail.ru", "client", UserType.Client) { Longitude = 15, Latitude = 15 },
            new UserModel("dispatcher", "dispatcher", "dispatcher", "dispatcher@mail.ru", "dispatcher", UserType.Dispatcher) { Longitude = 30, Latitude = 30},
            new UserModel("driver", "driver", "driver", "driver@mail.ru", "driver", UserType.Driver) {Longitude = 60, Latitude = 60},
        };
        public static List<OrderModel> Orders { get; set; } = new List<OrderModel>()
        {
            new OrderModel("Улица Киренского 26, Красноярск", (double)Users[1].Longitude, (double)Users[1].Latitude, "Проспект Свободный 76Н", 35,35, Users[1],  Users[2], Users[3]),
        };
        public static IHttpClientFactory httpClientFactory = new DefaultHttpClientFactory();

        static async Task Main()
        {

            TcpListener tcpListener = new TcpListener(IPAddress.Any, 8888);
            if (isdbneeded)
            {
                if (isDatabaseNeedToDeletedBefore)
                {
                    mauiContext.Database.EnsureDeleted();
                }
                mauiContext.Database.EnsureCreated();
                mauiContext.Users.Load();
                Users = mauiContext.Users.Local.ToList();
            }
            tcpListener.Start();    // запускаем сервер
            Console.WriteLine("Сервер запущен. Ожидание подключений... ");
            while (true)
            {
                var tcpClient = await tcpListener.AcceptTcpClientAsync();
                Console.WriteLine("Есть подключение!");
                Task.Run(async () => await ProcessClientAsync(tcpClient));
            }

            async Task ProcessClientAsync(TcpClient tcpClient)
            {
                var stream = tcpClient.GetStream();
                using var streamReader = new StreamReader(stream);
                using var streamWriter = new StreamWriter(stream);
                using var binaryReader = new BinaryReader(stream);
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
                                if (EntriesValidation(entries[1], entries[2]) || Users.Count == 0)
                                {
                                    //mauiContext.Users.Add(new UserModel(entries[0], entries[1], entries[2], entries[3]));
                                    //mauiContext.SaveChanges();
                                    //Users.Add(new UserModel(entries[0], entries[1], entries[2], entries[3]));
                                    string response = "1";
                                    Console.WriteLine($"Запрос:{destination}, Поле: {entry}, Ответ:{response}");
                                    //TODO:создаём папку пользователя
                                    if (Users.Count != 0)
                                    {
                                        MakeUserDirectory(Users.Count);
                                    }
                                    else
                                    {
                                        MakeUserDirectory(0);
                                    }
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
                else if (destination == "CREATEDRIVERORDERREQUEST")
                {
                    //TODO:проверка токена
                    var token = await streamReader.ReadLineAsync();
                    var isTokenValid = CheckToken(token, UserType.Dispatcher.ToString());
                    if (isTokenValid)
                    {
                        await SendMessageToClientAsync(streamWriter, "1");
                        var driver_phone = await streamReader.ReadLineAsync();
                        var client_phone = await streamReader.ReadLineAsync();
                        var order = Orders.FirstOrDefault(item => item.ClientPhoneNumber == client_phone);
                        if (order != null)
                        {
                            var driver = Users.FirstOrDefault(item => item.PhoneNumber == driver_phone && item.UserType == UserType.Driver);
                            if (driver != null)
                            {
                                if (order.Status == OrderStatus.Waiting)
                                {
                                    order.DriverPhoneNumber = driver.PhoneNumber;
                                }
                            }
                        }
                    }
                }
                else if (destination == "DRIVERANSWER")
                {
                    //TODO:проверка токена
                    var token = await streamReader.ReadLineAsync();
                    var user = GetUserFromToken(token);
                    if (user != null)
                    {
                        var order = Orders.FirstOrDefault(item => item.DriverPhoneNumber == user.PhoneNumber);
                        if (order != null)
                        {
                            await SendMessageToClientAsync(streamWriter, "1");
                            var response = await streamReader.ReadLineAsync();
                            bool.TryParse(response, out bool answer);
                            if (answer)
                            {
                                if (order.Status == OrderStatus.Waiting)
                                {
                                    order.Status = OrderStatus.InProgress;
                                }
                            }
                            else
                            {
                                order.DriverAvoidList.Add(user.PhoneNumber);
                            }
                        }
                    }
                }
                else if(destination == "GETDATA")
                {
                    var token = await streamReader.ReadLineAsync();
                    var user = GetUserFromToken(token);
                    if (user != null)
                    {
                        await SendMessageToClientAsync(streamWriter, "1");

                        if (user.UserType == UserType.Client)
                        {
                            var order = Orders.FirstOrDefault(item => item.ClientPhoneNumber == user.PhoneNumber);
                            var orderJson = CreateJsonFromObject(order);
                            await SendMessageToClientAsync(streamWriter, orderJson);

                            var driver = Users.FirstOrDefault(item => item.PhoneNumber == order.DriverPhoneNumber);
                            var driverJson = CreateJsonFromObject(driver);
                            await SendMessageToClientAsync(streamWriter, driverJson);
                        }

                        else if (user.UserType == UserType.Driver)
                        {
                            var order = Orders.FirstOrDefault(item => item.DriverPhoneNumber == user.PhoneNumber);
                            var orderJson = CreateJsonFromObject(order);
                            await SendMessageToClientAsync(streamWriter, orderJson);

                            var client = Users.FirstOrDefault(item => item.PhoneNumber == order?.ClientPhoneNumber);
                            var clientJson = CreateJsonFromObject(client);
                            await SendMessageToClientAsync(streamWriter, clientJson);
                        }

                        else if (user.UserType == UserType.Dispatcher)
                        {
                            var orders = Orders.Where(item => item.DispatcherPhoneNumber == user.PhoneNumber).ToArray();
                            List<UserModel?> ordersUsers = new();
                            foreach (var element in orders)
                            {
                                var client = Users.FirstOrDefault(item => item.PhoneNumber == element.ClientPhoneNumber);
                                var driver = Users.FirstOrDefault(item => item.PhoneNumber == element.DriverPhoneNumber);
                                if (client != null)
                                {
                                    ordersUsers.Add(client);
                                }
                                if (driver != null)
                                {
                                    ordersUsers.Add(driver);
                                }

                            }
                            var ordersJson = CreateJsonFromObject(orders);
                            await SendMessageToClientAsync(streamWriter, ordersJson);
                            var ordersUsersJson = CreateJsonFromObject(ordersUsers);
                            await SendMessageToClientAsync(streamWriter, ordersUsersJson);

                            var drivers = Users.Where(item => item.UserType == UserType.Driver).ToArray();
                            var driversJson = CreateJsonFromObject(drivers);
                            await SendMessageToClientAsync(streamWriter, driversJson);

                        }

                        else if (user.UserType == UserType.Administrator)
                        {
                            var usersJson = CreateJsonFromObject(Users);
                            await SendMessageToClientAsync(streamWriter, usersJson);
                        }
                    }
                }
                else if (destination == "GETUSERLOCATION")
                {
                    var token = await streamReader.ReadLineAsync();
                    var user = GetUserFromToken(token);
                    if (user != null)
                    {
                        await SendMessageToClientAsync(streamWriter, "1");
                        var order = Orders.FirstOrDefault(item => item.ClientPhoneNumber == user.PhoneNumber || item.DriverPhoneNumber == user.PhoneNumber || item.DispatcherPhoneNumber == user.PhoneNumber);
                        var reqUserPhoneNumber = await streamReader.ReadLineAsync();
                        if (reqUserPhoneNumber != null)
                        {
                            if (order.DriverPhoneNumber == reqUserPhoneNumber)
                            {
                                var driver = Users.FirstOrDefault(item => item.PhoneNumber == reqUserPhoneNumber);
                                if (driver != null)
                                {
                                    await SendMessageToClientAsync(streamWriter, driver.Longitude.ToString());
                                    await SendMessageToClientAsync(streamWriter, driver.Latitude.ToString());
                                }
                            }
                            else if (order.ClientPhoneNumber == reqUserPhoneNumber)
                            {
                                var client = Users.FirstOrDefault(item => item.PhoneNumber == reqUserPhoneNumber);
                                if (client != null)
                                {
                                    await SendMessageToClientAsync(streamWriter, client.Longitude.ToString());
                                    await SendMessageToClientAsync(streamWriter, client.Latitude.ToString());
                                }
                            }
                            else if (order.DispatcherPhoneNumber == reqUserPhoneNumber)
                            {
                                var dispatcher = Users.FirstOrDefault(item => item.PhoneNumber == reqUserPhoneNumber);
                                if (dispatcher != null)
                                {
                                    await SendMessageToClientAsync(streamWriter, dispatcher.Longitude.ToString());
                                    await SendMessageToClientAsync(streamWriter, dispatcher.Latitude.ToString());
                                }
                            }
                        }

                    }
                    await SendMessageToClientAsync(streamWriter, "close");
                }
                else if (destination == "REFRESHLOC")
                {
                    var token = await streamReader.ReadLineAsync();
                    var user = GetUserFromToken(token);
                    if (user != null)
                    {
                        await SendMessageToClientAsync(streamWriter, "1");
                        var longitude = await streamReader.ReadLineAsync();
                        var latitude = await streamReader.ReadLineAsync();
                        if (double.TryParse(longitude, out double longi))
                        {
                            user.Longitude = longi;
                        }
                        if (double.TryParse(latitude, out double lati))
                        {
                            user.Latitude = lati;
                        }
                        await SendMessageToClientAsync(streamWriter, null);
                    }
                    else
                    {
                        await SendMessageToClientAsync(streamWriter, null);
                    }
                }
                else if (destination == "LOGIN")
                {
                    try
                    {
                        var loginJson = await streamReader.ReadLineAsync();
                        var userCredentials = GetUserCredentialsFromJson(loginJson);

                        //проверка на логин
                        var user = Users.Where(item => item.PhoneNumber == userCredentials.Login && item.Password == userCredentials.Password).FirstOrDefault();
                        if (user != null)
                        {
                            await SendMessageToClientAsync(streamWriter, "1");
                            var tokenString = await GetToken(user.PhoneNumber, user.Email, user.UserType);
                            await SendMessageToClientAsync(streamWriter, tokenString);
                            var userJson = CreateJsonFromObject(user);
                            await SendMessageToClientAsync(streamWriter, userJson);

                            if (user.UserType == UserType.Client)
                            {
                                var order = Orders.FirstOrDefault(item => item.ClientPhoneNumber == user.PhoneNumber);
                                var orderJson = CreateJsonFromObject(order);
                                await SendMessageToClientAsync(streamWriter, orderJson);
                                if (order != null)
                                {
                                    var driver = Users.FirstOrDefault(item => item.PhoneNumber == order.DriverPhoneNumber);
                                    var driverJson = CreateJsonFromObject(driver);
                                    await SendMessageToClientAsync(streamWriter, driverJson);
                                }
                                else
                                {
                                    await SendMessageToClientAsync(streamWriter, null);
                                }
                            }

                            else if (user.UserType == UserType.Driver)
                            {
                                var order = Orders.FirstOrDefault(item => item.DriverPhoneNumber == user.PhoneNumber);
                                var orderJson = CreateJsonFromObject(order);
                                await SendMessageToClientAsync(streamWriter, orderJson);
                                if (order != null)
                                {
                                    var client = Users.FirstOrDefault(item => item.PhoneNumber == order.ClientPhoneNumber);
                                    var clientJson = CreateJsonFromObject(client);
                                    await SendMessageToClientAsync(streamWriter, clientJson);
                                }
                                else
                                {
                                    await SendMessageToClientAsync(streamWriter, null);
                                }
                                return;
                            }

                            else if (user.UserType == UserType.Dispatcher)
                            {
                                var orders = Orders.Where(item => item.DispatcherPhoneNumber == user.PhoneNumber).ToArray();
                                List<UserModel?> ordersUsers = new();
                                foreach (var element in orders)
                                {
                                    var client = Users.FirstOrDefault(item => item.PhoneNumber == element.ClientPhoneNumber);
                                    var driver = Users.FirstOrDefault(item => item.PhoneNumber == element.DriverPhoneNumber);
                                    if (client != null)
                                    {
                                        ordersUsers.Add(client);
                                    }
                                    if (driver != null)
                                    {
                                        ordersUsers.Add(driver);
                                    }

                                }
                                var ordersJson = CreateJsonFromObject(orders);
                                await SendMessageToClientAsync(streamWriter, ordersJson);
                                var ordersUsersJson = CreateJsonFromObject(ordersUsers);
                                await SendMessageToClientAsync(streamWriter, ordersUsersJson);

                                var drivers = Users.Where(item => item.UserType == UserType.Driver).ToArray();
                                var driversJson = CreateJsonFromObject(drivers);
                                await SendMessageToClientAsync(streamWriter, driversJson);

                            }

                            else if (user.UserType == UserType.Administrator)
                            {
                                var usersJson = CreateJsonFromObject(Users);
                                await SendMessageToClientAsync(streamWriter, usersJson);
                            }
                        }
                        return;
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                }
                else if (destination == "AUTH")
                {
                    try
                    {
                        var token = await streamReader.ReadLineAsync();
                        var user = GetUserAuth(token);
                        if (user != null)
                        {
                            await SendMessageToClientAsync(streamWriter, "1");
                            var userJson = CreateJsonFromObject(user);
                            await SendMessageToClientAsync(streamWriter, userJson);

                            if (user.UserType == UserType.Client)
                            {
                                var order = Orders.FirstOrDefault(item => item.ClientPhoneNumber == user.PhoneNumber);
                                var orderJson = CreateJsonFromObject(order);
                                await SendMessageToClientAsync(streamWriter, orderJson);

                                if (order != null)
                                {
                                    var driver = Users.FirstOrDefault(item => item.PhoneNumber == order.DriverPhoneNumber);
                                    var driverJson = CreateJsonFromObject(driver);
                                    await SendMessageToClientAsync(streamWriter, driverJson);
                                }
                                else
                                {
                                    await SendMessageToClientAsync(streamWriter, null);
                                }
                            }

                            else if (user.UserType == UserType.Driver)
                          {
                                var order = Orders.FirstOrDefault(item => item.DriverPhoneNumber == user.PhoneNumber);
                                var orderJson = CreateJsonFromObject(order);
                                await SendMessageToClientAsync(streamWriter, orderJson);
                                if (order != null)
                                {
                                    var client = Users.FirstOrDefault(item => item.PhoneNumber == order.ClientPhoneNumber);
                                    var clientJson = CreateJsonFromObject(client);
                                    await SendMessageToClientAsync(streamWriter, clientJson);
                                }
                                else
                                {
                                    await SendMessageToClientAsync(streamWriter, null);
                                }
                                return;
                            }

                            else if (user.UserType == UserType.Dispatcher)
                            {
                                var orders = Orders.Where(item => item.DispatcherPhoneNumber == user.PhoneNumber).ToArray();
                                List<UserModel?> ordersUsers = new();
                                foreach (var element in orders)
                                {
                                    var client = Users.FirstOrDefault(item => item.PhoneNumber == element.ClientPhoneNumber);
                                    var driver = Users.FirstOrDefault(item => item.PhoneNumber == element.DriverPhoneNumber);
                                    if (client != null)
                                    {
                                        ordersUsers.Add(client);
                                    }
                                    if (driver != null)
                                    {
                                        ordersUsers.Add(driver);
                                    }

                                }
                                var ordersJson = CreateJsonFromObject(orders);
                                await SendMessageToClientAsync(streamWriter, ordersJson);
                                var ordersUsersJson = CreateJsonFromObject(ordersUsers);
                                await SendMessageToClientAsync(streamWriter, ordersUsersJson);

                                var drivers = Users.Where(item => item.UserType == UserType.Driver).ToArray();
                                var driversJson = CreateJsonFromObject(drivers);
                                await SendMessageToClientAsync(streamWriter, driversJson);

                            }

                            else if (user.UserType == UserType.Administrator)
                            {
                                var usersJson = CreateJsonFromObject(Users);
                                await SendMessageToClientAsync(streamWriter, usersJson);
                            }
                        }
                        return;
                    }
                    catch
                    {

                    }
                }
                else if (destination == "CREATEORDER")
                {
                    var token = await streamReader.ReadLineAsync();
                    var isTokenValid = CheckToken(token, UserType.Client.ToString());
                    if(isTokenValid)
                    {
                        var user = GetUserFromToken(token);
                        if(user != null)
                        {
                            await SendMessageToClientAsync(streamWriter, "1");
                            var orderJson = await streamReader.ReadLineAsync();
                            if (orderJson != null)
                            {
                                var order = JsonConvert.DeserializeObject<OrderModel>(orderJson);
                                var dispatcher = Users.FirstOrDefault(item => item.UserType == UserType.Dispatcher);
                                if(dispatcher != null)
                                {
                                    order.Dispatcher_Id = dispatcher.Id;
                                    order.DispatcherPhoneNumber = dispatcher.PhoneNumber;
                                    Orders.Add(order);
                                    await SendMessageToClientAsync(streamWriter, bool.TrueString);
                                    return;
                                }
                            }
                        }
                    }
                    await SendMessageToClientAsync(streamWriter, bool.FalseString);
                }
                else if (destination == "REG")
                {
                    var userJson = await streamReader.ReadLineAsync();
                    if(userJson != null)
                    {
                        var user = JsonConvert.DeserializeObject<UserModel>(userJson);
                        if (user.FirstName != null && user.LastName != null && user.PhoneNumber != null && user.Email != null && user.Password != null)
                        {
                            user.Created = DateTime.Now;
                            mauiContext.Users.Add(user);
                            mauiContext.SaveChanges();
                            Users.Add(user);
                            await SendMessageToClientAsync(streamWriter, bool.TrueString);
                            return;
                        }
                        else
                        {
                            await SendMessageToClientAsync(streamWriter, bool.FalseString);
                            return;
                        }
                    }
                    else
                    {
                        await SendMessageToClientAsync(streamWriter, bool.FalseString);
                        return;
                    }
                }
                else if (destination == "GETUSERS")
                {
                    try
                    {
                        await SendMessageToClientAsync(streamWriter, "1");
                        var token = await streamReader.ReadLineAsync();
                        var resp = CheckToken(token, UserType.Administrator.ToString());
                        await SendMessageToClientAsync(streamWriter, resp.ToString());
                        if (resp)
                        {
                            var usersJson = JsonConvert.SerializeObject(Users);
                            await SendMessageToClientAsync(streamWriter, usersJson);
                        }
                    }
                    catch
                    {

                    }
                }
            }
        }
        private async static Task SendMessageToClientAsync(StreamWriter streamWriter, string? message)
        {
            await streamWriter.WriteLineAsync(message);
            await streamWriter.FlushAsync();
        }

        private static bool CheckToken(string jwtToken, string roleReq)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(sercretKey));

            // Создаем параметры проверки токена
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "maui1_server_",
                ValidateAudience = true,
                ValidAudience = "maui1_users",
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                IssuerSigningKey = key
            };

            // Проверяем токен
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var claimsPrincipal = tokenHandler.ValidateToken(jwtToken, validationParameters, out var validatedToken);

                // Токен действительный, извлекаем утверждения (claims)
                var jwtTokenClaims = claimsPrincipal.Claims;
                Console.WriteLine($"Token validated. Claims:");
                var userType = jwtTokenClaims?.FirstOrDefault(item => item.Type == ClaimsIdentity.DefaultRoleClaimType).Value;
                if (userType != null)
                {
                    if (userType == roleReq)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (SecurityTokenException ex)
            {
                // Токен недействительный
                Console.WriteLine($"Token validation failed. Reason: {ex.Message}");
                return false;
            }
        }
        private static (bool, string) CheckToken(string jwtToken)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(sercretKey));

            // Создаем параметры проверки токена
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "maui1_server_",
                ValidateAudience = true,
                ValidAudience = "maui1_users",
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                IssuerSigningKey = key
            };

            // Проверяем токен
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var claimsPrincipal = tokenHandler.ValidateToken(jwtToken, validationParameters, out var validatedToken);

                // Токен действительный, извлекаем утверждения (claims)
                var jwtTokenClaims = claimsPrincipal.Claims;
                Console.WriteLine($"Token validated. Claims:");
                var userType = jwtTokenClaims?.FirstOrDefault(item => item.Type == ClaimsIdentity.DefaultRoleClaimType).Value;
                if (userType != null)
                {
                    return (true, userType);
                }
                return (false, UserType.Unknown.ToString());
            }
            catch (SecurityTokenException ex)
            {
                // Токен недействительный
                Console.WriteLine($"Token validation failed. Reason: {ex.Message}");
                return (false, UserType.Unknown.ToString());
            }
        }
        private static UserModel? GetUserFromToken(string jwtToken)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(sercretKey));

            // Создаем параметры проверки токена
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "maui1_server_",
                ValidateAudience = true,
                ValidAudience = "maui1_users",
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                IssuerSigningKey = key
            };

            // Проверяем токен
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var claimsPrincipal = tokenHandler.ValidateToken(jwtToken, validationParameters, out var validatedToken);

                // Токен действительный, извлекаем утверждения (claims)
                var jwtTokenClaims = claimsPrincipal.Claims;
                Console.WriteLine($"Token validated. Claims:");
                var userPhoneNumber = jwtTokenClaims?.FirstOrDefault(item => item.Type == ClaimTypes.NameIdentifier)?.Value;
                if (userPhoneNumber != null)
                {
                    var user = Users.FirstOrDefault(item => item.PhoneNumber == userPhoneNumber);
                    if (user != null)
                    {
                        return user;
                    }
                }
                return null;
            }
            catch (SecurityTokenException ex)
            {
                // Токен недействительный
                Console.WriteLine($"Token validation failed. Reason: {ex.Message}");
                return null;
            }
        }
        public static string? CreateJsonFromObject(object? obj)
        {
            string json;
            if(obj != null)
            {
                if(obj is IEnumerable<object>)
                {
                    if((obj as IEnumerable<object>).Count() == 0)
                    {
                        return null;
                    }
                }
                json = JsonConvert.SerializeObject(obj);
                return json;
            }
            else
            {
                return null;
            }
        }
        private static UserModel? GetUserAuth(string jwtToken)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(sercretKey));

            // Создаем параметры проверки токена
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "maui1_server_",
                ValidateAudience = true,
                ValidAudience = "maui1_users",
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                IssuerSigningKey = key
            };

            // Проверяем токен
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var claimsPrincipal = tokenHandler.ValidateToken(jwtToken, validationParameters, out var validatedToken);

                // Токен действительный, извлекаем утверждения (claims)
                var jwtTokenClaims = claimsPrincipal.Claims;
                Console.WriteLine($"Token validated. Claims:");
                var nameClaim = jwtTokenClaims.ToArray()[0].Value; //mobila
                return Users?.FirstOrDefault(item => item.PhoneNumber == nameClaim);
            }
            catch (SecurityTokenException ex)
            {
                // Токен недействительный
                Console.WriteLine($"Token validation failed. Reason: {ex.Message}");
                return null;
            }
        }

        private async static Task<string> GetToken(string userPhoneNumber, string userEmail, UserType userType)
        {
            // Создаем список утверждений (claims) для токена
            var claims = new[]
            {
    new Claim(ClaimTypes.NameIdentifier, userPhoneNumber),
    new Claim(ClaimTypes.Email, userEmail),
    new Claim(ClaimTypes.Role, userType.ToString()),
};

            // Получаем секретный ключ
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(sercretKey));

            // Создаем подпись для токена
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Создаем сам токен
            var token = new JwtSecurityToken(
                issuer: "maui1_server_",
                audience: "maui1_users",
                claims: claims,
                expires: DateTime.Now.AddMinutes(300),
                signingCredentials: creds);

            // Получаем строковое представление токена
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtTokenString = await Task.Run(() => tokenHandler.WriteToken(token));
            // Выводим токен в консоль
            Console.WriteLine(jwtTokenString);
            return jwtTokenString;
        }
        private static UserModel GetUserFromJson(string jsonUserDataFilePath)
        {
            string json = File.ReadAllText(jsonUserDataFilePath);
            var newUser = JsonConvert.DeserializeObject<UserModel>(json);
            return newUser;
        }
        private static UserCredentials GetUserCredentialsFromJson(string json)
        {
            var newUserCredentials = JsonConvert.DeserializeObject<UserCredentials>(json);
            return newUserCredentials;
        }
        private static void MakeUserDirectory(int userId)
        {
            Directory.CreateDirectory($"{projectFolderPath}\\{userId}\\Images");
        }

        static async Task<byte[]> GetUserAvatar(int userId)
        {
            // Получаем путь к файлу аватара пользователя
            string userAvatarFilePath = Path.Combine(GetUserFolderPath(userId), "avatar.jpg");

            if (!System.IO.File.Exists(userAvatarFilePath))
            {
                // Если аватар не найден, возвращаем стандартное изображение
                userAvatarFilePath = Path.Combine(projectFolderPath, "Images", "default-avatar.jpg");
            }

            // Читаем файл и возвращаем его в виде массива байтов
            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(userAvatarFilePath);
            return fileBytes;
        }
        static string GetUserFolderPath(int userId)
        {
            string userFolderPath = Path.Combine("Images", userId.ToString());
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
            if (!Users.Any(user => user.PhoneNumber == number))
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
            if (Users.Any(user => user.PhoneNumber == number && user.Password == password))
            {
                return true;
            }
            return false;
        }
    }
}

