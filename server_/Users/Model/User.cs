using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server_.Users.Model
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Number { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public DateTime Created { get; set; }
        public User(string name, string number, string email, string password)
        {
            Name = name;
            Number = number;
            Email = email;
            Password = password;
        }
        public User() { }
    }
}
