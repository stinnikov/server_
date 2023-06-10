using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server_.User.Model
{
    public class Dispatcher : UserModel
    {

        public Dispatcher(string firstName, string lastName, string phoneNumber, string email, string password, UserType userType) : base(firstName, lastName, phoneNumber, email, password, userType)
        {
        }
    }
}
