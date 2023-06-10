using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server_.User.Model
{
    internal class ClientModel : UserModel
    {
        public int? Order_Id { get; set; }
        public Order.Model.OrderStatus OrderStatus { get; set; } = Order.Model.OrderStatus.Unknown;
        public string? OrderJson { get; set; }
        public ClientModel(string firstName, string lastName, string phoneNumber, string email, string password, UserType userType) : base(firstName, lastName, phoneNumber, email, password, userType)
        {
        }
    }
}
