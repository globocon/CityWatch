using System;
using System.Linq;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class DataChecker
{
    public static void CheckData(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<CityWatchContext>();
        var statuses = context.RadioCheckStatus.Include(x => x.RadioCheckStatusColor).ToList();
        foreach (var s in statuses)
        {
            Console.WriteLine($"ID: {s.Id}, Ref: {s.ReferenceNo}, Name: '{s.Name}', ColorName: '{s.RadioCheckStatusColorName}', Color.Name: '{s.RadioCheckStatusColor?.Name}'");
        }
    }
}
