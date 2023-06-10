using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server_.User.Model
{
    public class DriverModel : UserModel
    {
        public int? Order_Id { get; set; }
        public Order.Model.OrderStatus OrderStatus { get; set; } = Order.Model.OrderStatus.Unknown;
        public string? OrderJson { get; set; }
        public DriverModel(string firstName, string lastName, string phoneNumber, string email, string password, UserType userType) : base(firstName, lastName, phoneNumber, email, password, userType)
        {
        }
    }
}
