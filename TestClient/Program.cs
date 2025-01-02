// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using TestClient;
using MixVel.Interfaces;
///api/v1/Search/search
var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://localhost:7241/")
};


await Task.Delay(10000);
var client = new Client(httpClient);
var generator = new RequestsGenerator();
var requests = new List<SearchRequest>();
for (int i = 0; i < 2000; i++)
{
    var random = new Random(i);
    var addRequest = generator.GenerateAddRequest(random);
    var getRequest = generator.GenerateGetRequest(random);
    requests.Add(addRequest);
    if (getRequest is null) continue;
    requests.Add(getRequest);
}
int s = 0;
await Parallel.ForEachAsync(requests, async (x, ct) => 
{
    var random = new Random(s++);
    await client.SendPostRequest(x);
    //if (s % 2 == 0)
    //    await Task.Delay(random.Next(0,10));
});

Console.WriteLine("End");
Console.ReadLine();
