﻿namespace camp.Models
{
    public class User
    {
        public int id { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string password { get; set; }

    }
    public class Spot
    {
        public int id { get; set; }
        public string spotname { get; set; }
        public string location { get; set; }
        public int capacity { get; set; }
        public string description { get; set; }
        public int price { get; set; }
    }
    public class UserLoginRequest
    {
        public string email { get; set; }
        public string password { get; set; }
    }
    public class FeaturedSite
    {
        public string spotname { get; set; }
        public string description { get; set; }
    }
}
   
    
