using server_.User.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace server_.Order.Model
{
    public enum OrderStatus
    {
        Unknown,
        Waiting,
        InProgress,
        Completed,
        Cancelled
    }
    public class OrderModel
    {
        [NotMapped]
        public List<string> DriverAvoidList { get; set; } = new();
        public int Id { get; set; }
        public DateTime StartingTime { get; set; } //дата начала заказа
        public DateTime EndingTime { get; set; } //дата окончания заказа
        public int Dispatcher_Id { get; set; }
        public string DispatcherPhoneNumber { get; set; }
        public int Client_Id { get; set; } //идентификатор клиента
        public string ClientPhoneNumber { get; set; }
        public string StartingPoint { get; set; } //начальная точка
        public double StartingPointLongitude { get; set; }
        public double StartingPointLatitude { get; set; }
        public string EndingPoint { get; set; } //конечная точка
        public double EndingPointLongitude { get; set; }
        public double EndingPointLatitude { get; set; }
        public float Price { get; set; } //цена
        public OrderStatus Status { get; set; } //статус заказа
        public int? Driver_Id { get; set; } //id виодителя который выполнил/яет заказ
        public string? DriverPhoneNumber { get; set; }
                                                //public string Tariff; //тариф
        public OrderModel(string startingPoint, double startingPointLongitude, double startingPointLatitude, 
            string endingPoint, double endingPointLongitude, double endingPointLatitude,
            UserModel client, UserModel dispatcher)
        {
            this.StartingPoint = startingPoint;
            this.StartingPointLongitude = startingPointLongitude;
            this.StartingPointLatitude = startingPointLatitude;
            this.EndingPoint = endingPoint;
            this.EndingPointLongitude = endingPointLongitude;
            this.EndingPointLatitude = endingPointLatitude;
            Client_Id = client.Id;
            ClientPhoneNumber = client.PhoneNumber;
            Dispatcher_Id = dispatcher.Id;
            DispatcherPhoneNumber = dispatcher.PhoneNumber;
        }
        public OrderModel(string startingPoint, double startingPointLongitude, double startingPointLatitude,
            string endingPoint, double endingPointLongitude, double endingPointLatitude, UserModel client, UserModel dispatcher, UserModel driver)
        {
            this.StartingPoint = startingPoint;
            this.StartingPointLongitude = startingPointLongitude;
            this.StartingPointLatitude = startingPointLatitude;
            this.EndingPoint = endingPoint;
            this.EndingPointLongitude = endingPointLongitude;
            this.EndingPointLatitude = endingPointLatitude;
            Client_Id = client.Id;
            ClientPhoneNumber = client.PhoneNumber;
            Driver_Id = driver.Id;
            DriverPhoneNumber = driver.PhoneNumber;
            Dispatcher_Id = dispatcher.Id;
            DispatcherPhoneNumber = dispatcher.PhoneNumber;
        }
        public OrderModel(string startingPoint, double startingPointLongitude, double startingPointLatitude,
            string endingPoint, double endingPointLongitude, double endingPointLatitude, float price, UserModel client, UserModel dispatcher)
        {
            this.StartingPoint = startingPoint;
            this.StartingPointLongitude = startingPointLongitude;
            this.StartingPointLatitude = startingPointLatitude;
            this.EndingPoint = endingPoint;
            this.EndingPointLongitude = endingPointLongitude;
            this.EndingPointLatitude = endingPointLatitude;
            Client_Id = client.Id;
            ClientPhoneNumber = client.PhoneNumber;
            Dispatcher_Id = dispatcher.Id;
            DispatcherPhoneNumber = dispatcher.PhoneNumber;
            Price = price;
        }
        public OrderModel(string startingPoint, double startingPointLongitude, double startingPointLatitude,
            string endingPoint, double endingPointLongitude, double endingPointLatitude, float price,  UserModel client, UserModel dispatcher, UserModel driver)
        {
            this.StartingPoint = startingPoint;
            this.StartingPointLongitude = startingPointLongitude;
            this.StartingPointLatitude = startingPointLatitude;
            this.EndingPoint = endingPoint;
            this.EndingPointLongitude = endingPointLongitude;
            this.EndingPointLatitude = endingPointLatitude;
            Client_Id = client.Id;
            ClientPhoneNumber = client.PhoneNumber;
            Driver_Id = driver.Id;
            DriverPhoneNumber = driver.PhoneNumber;
            Dispatcher_Id = dispatcher.Id;
            DispatcherPhoneNumber = dispatcher.PhoneNumber;
            Price = price;
        }
        public OrderModel()
        {

        }
    }
}
