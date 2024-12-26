using MixVel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using TestTask;

public class ResponseGenerator
{
    public static ProviderOneSearchResponse GenerateResponse(ProviderOneSearchRequest request)
    {
        return new ProviderOneSearchResponse
        {
            Routes = Enumerable.Range(1, 3).Select(i => new ProviderOneRoute
            {
                From = request.From,
                To = request.To,
                DateFrom = request.DateFrom.AddHours(i),
                Price = 100 + (i * 10),
                TimeLimit = DateTime.UtcNow.AddMinutes(1)
            }).ToArray()
        };
    }

    public static ProviderTwoSearchResponse GenerateResponse(ProviderTwoSearchRequest request)
    {
        return new ProviderTwoSearchResponse
        {
            Routes = Enumerable.Range(1, 3).Select(i => new ProviderTwoRoute
            {
                Departure = new ProviderTwoPoint{ Point = request.Departure, Date = request.DepartureDate.AddHours(i) },
                Arrival = new ProviderTwoPoint { Point = request.Arrival },

                Price = 100 + (i * 10),
                TimeLimit = DateTime.UtcNow.AddMinutes(1)
            }).ToArray()
        };
    }
}
