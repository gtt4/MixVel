using MixVel.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestClient
{
    internal class RequestsGenerator
    {

        private readonly Random _rand;
        private readonly string[] _cities;
        private readonly ConcurrentBag<SearchRequest> _addedRequests;

        public RequestsGenerator()
        {
            _rand = new Random();
            _cities = new[] { "Moscow", "Sochi", "Budva", "Kotor", "Spb", "Helsinki", "Kouvola", "Tallinn", "Paris", "HongKong" };
            _addedRequests = new ConcurrentBag<SearchRequest>();
        }

        public SearchRequest GenerateAddRequest(Random random)
        {
            var request = new SearchRequest
            {
                Origin = _cities.GetRandom(random),
                Destination = _cities.GetRandom(random),
                OriginDateTime = GetRandomDate(random), 
                Filters = new SearchFilters() {OnlyCached = false }
            };

            _addedRequests.Add(request);
            return request;
        }

        public SearchRequest? GenerateGetRequest(Random random)
        {
            var count = _addedRequests.Count;

            if (count == 0) return null;
            var request = _addedRequests.ElementAt(random.Next(0, count));

            request.Filters = new SearchFilters() { OnlyCached = true };
            return request;
        }

        private DateTime GetRandomDate(Random random)
        {
            var now  = DateTime.UtcNow;
            return now.AddMinutes(random.Next(1000, 100000));
        }
    }

    public static class RandomExtension
    {
        public static string GetRandom(this string[] array, Random random)
        {
            return array[random.Next(0, array.Length)];
        }
    }
}
