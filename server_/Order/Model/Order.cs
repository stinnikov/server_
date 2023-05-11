using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace server_.Order
{
    public enum OrderStatus
    {
        Waiting,
        InProgress,
        Completed,
        Cancelled
    }
    public class Order
    {
        public int Id { get; set; }
        public DateTime StartingTime { get; set; } //дата начала заказа
        public DateTime EndingTime { get; set; } //дата окончания заказа
        public int Client_Id { get; set; } //идентификатор клиента
        public string StartingPoint { get; set; } //начальная точка
        public string EndingPoint { get; set; } //конечная точка
        public float Price { get; set; } //цена
        public OrderStatus Status { get; set; } //статус заказа
        public int Driver_Id { get; set; } //id виодителя который выполнил/яет заказ
                                                //public string Tariff; //тариф
        public Order(string startingPoint, string endingPoint)
        {
            this.StartingPoint = startingPoint;
            this.EndingPoint = endingPoint;
        }
        public Order(DateTime start_Time, string start, string end, float price)
        {
            StartingTime = start_Time;
            StartingPoint = start;
            EndingPoint = end;
            Price = price;
        }
    }
}
