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

namespace server_
{
    static class Program
    {
        public const string projectFolderPath = "C:\\Users\\XE\\source\\repos\\server_\\server_\\User\\Users";
        public const string sercretKey = "we_have_to_be_the_greatest_than_256_bits";
        public const bool isdbneeded = false;
        public const bool isDatabaseNeedToDeletedBefore = true;
        public const string AdminLogin = "Admin";
        public const string AdminPassword = "Admin";

        public static MauiContext mauiContext = new MauiContext();
        public static List<UserModel> Users { get; set; } = new() 
        { 
            new UserModel("admin", "admin", "admin", "admin@mail.ru", "admin", UserType.Administrator),
            new UserModel("client", "client", "client", "client@mail.ru", "client", UserType.Client),
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
                else if (destination == "LOGIN")
                {
                    try
                    {
                        await SendMessageToClientAsync(streamWriter, "1");
                        var fileSize = await streamReader.ReadLineAsync();
                        FileStream fileStream = new FileStream($"{projectFolderPath}\\1\\Images\\user_login.json", FileMode.Create, FileAccess.Write);
                        var e = binaryReader.ReadBytes(Convert.ToInt32(fileSize));
                        fileStream.Write(e, 0, e.Length);
                        fileStream.Close();
                        var userCredentials = GetUserCredentialsFromJson($"{projectFolderPath}\\1\\Images\\user_login.json");

                        //проверка на логин
                        var user = Users.Where(item => item.PhoneNumber == userCredentials.Login && item.Password == userCredentials.Password).FirstOrDefault();
                        if (user != null)
                        {
                            await SendMessageToClientAsync(streamWriter, "1");
                            var tokenString = await GetToken(user.PhoneNumber, user.Email, user.UserType);
                            await SendMessageToClientAsync(streamWriter, tokenString);
                            await SendMessageToClientAsync(streamWriter, user.UserType.ToString());
                        }
                        else
                        {
                            await SendMessageToClientAsync(streamWriter, "0");
                        }
                        return;
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                }
                else if (destination == "LOGOUT")
                {

                }
                else if (destination == "REG")
                {
                    await SendMessageToClientAsync(streamWriter, "1");
                    var fileSize = await streamReader.ReadLineAsync();
                    FileStream fileStream = new FileStream($"{projectFolderPath}\\1\\Images\\user.json", FileMode.Create, FileAccess.Write);
                    var e = binaryReader.ReadBytes(Convert.ToInt32(fileSize));
                    fileStream.Write(e, 0, e.Length);
                    fileStream.Close();
                    var user = GetUserFromJson($"{projectFolderPath}\\1\\Images\\user.json");
                    var tokenString = await GetToken(user.PhoneNumber, user.Email, UserType.Client);
                    user.Token = tokenString;
                    Users.Add(user);
                    await streamWriter.WriteLineAsync(user.UserType.ToString());
                    await streamWriter.FlushAsync();
                    return;
                }
                else if(destination == "GETUSERS")
                {
                    try
                    {
                        await SendMessageToClientAsync(streamWriter, "1");
                        var resp = CheckToken(await streamReader.ReadLineAsync(), "Administrator");
                        await SendMessageToClientAsync(streamWriter, resp.ToString());
                        if(resp)
                        {
                            var json = GetUsersJson(Users.ToArray());
                            await SendMessageToClientAsync(streamWriter, json);
                        }
                    }
                    catch
                    {

                    }
                }
                else if (destination == "test")
                {
                    await streamWriter.WriteLineAsync("1");
                    await streamWriter.FlushAsync();
                    var token = await streamReader.ReadLineAsync();
                    await streamWriter.WriteLineAsync(CheckToken(token, "Administrator").ToString());
                    await streamWriter.FlushAsync();
                    return;
                }
                else if (destination == "AUTH")
                {
                    try
                    {
                        await SendMessageToClientAsync(streamWriter, "1");
                        var user = GetUserAuth(await streamReader.ReadLineAsync());
                        
                        if (user != null)
                        {
                            await SendMessageToClientAsync(streamWriter, bool.TrueString);
                            var json = GetUserJson(user);
                            await SendMessageToClientAsync(streamWriter, json);
                        }
                        await SendMessageToClientAsync(streamWriter, bool.FalseString);
                    }
                    catch
                    {

                    }
                }
                else if (destination == "SAVEAVATAR")
                {
                    await streamWriter.WriteLineAsync("1");
                    await streamWriter.FlushAsync();
                    var fileSize = await streamReader.ReadLineAsync();
                    //BinaryReader binaryReader = new BinaryReader(stream);
                    FileStream fileStream = new FileStream($"{projectFolderPath}\\1\\Images\\avatar.png", FileMode.Create, FileAccess.Write);
                    var e = binaryReader.ReadBytes(Convert.ToInt32(fileSize));
                    fileStream.Write(e, 0, e.Length);
                    fileStream.Close();
                    await streamWriter.WriteLineAsync("1");
                    await streamWriter.FlushAsync();
                }
                else if (destination == "SENDAVATAR")
                {
                    using var binaryWriter = new BinaryWriter(stream);
                    using (FileStream fileStream = new FileStream($"{projectFolderPath}\\1\\Images\\avatar.png", FileMode.Open, FileAccess.Read))
                    {
                        var fileSize = fileStream.Length;
                        var buffer = new byte[fileStream.Length];
                        await fileStream.ReadAsync(buffer, 0, buffer.Length);
                        await streamWriter.WriteLineAsync(fileSize.ToString());
                        await streamWriter.FlushAsync();
                        binaryWriter.Write(buffer, 0, buffer.Length);
                    }
                    await streamWriter.WriteLineAsync("1");
                    await streamWriter.FlushAsync();
                }
            }
        }
        private static string GetUserJson(UserModel user)
        {
            var json = JsonConvert.SerializeObject(user);
            return json;
        }
        private static string GetUsersJson(UserModel[] users)
        {
            var json = JsonConvert.SerializeObject(users);
            return json;
        }

        private async static Task SendMessageToClientAsync(StreamWriter streamWriter, string message)
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
                if (jwtTokenClaims?.FirstOrDefault(item => item.Type == ClaimsIdentity.DefaultRoleClaimType).Value == roleReq)
                {
                    return true;
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
                expires: DateTime.Now.AddMinutes(30),
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
        private static UserCredentials GetUserCredentialsFromJson(string jsonUserDataFilePath)
        {
            string json = File.ReadAllText(jsonUserDataFilePath);
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

