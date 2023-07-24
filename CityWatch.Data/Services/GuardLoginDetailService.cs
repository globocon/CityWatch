using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Services
{
    public interface IGuardLoginDetailService
    {
        IEnumerable<IGrouping<string, GuardLoginDetail>> GetGuardDetailsByLogBookId(int logBookId);
    }

    public class GuardLoginDetailService : IGuardLoginDetailService
    {
        private readonly IGuardDataProvider _guardDataProvider;

        public GuardLoginDetailService(IGuardDataProvider guardDataProvider)
        { 
            _guardDataProvider = guardDataProvider;
        }

        public IEnumerable<IGrouping<string, GuardLoginDetail>> GetGuardDetailsByLogBookId(int logBookId)
        {
            var allGuardLogins = _guardDataProvider.GetGuardLoginsByLogBookId(logBookId).Select(z => new GuardLoginDetail(z));
            
            var guardLoginsArranged = new List<GuardLoginDetail>();
            guardLoginsArranged.AddRange(allGuardLogins.Where(z => !z.IsPosition));

            foreach (var positionGroup in allGuardLogins.Where(z => z.IsPosition).GroupBy(z => z.SmartWandOrPosition))
            {
                // Entries of guards logged in with position and working in parallel has to split.
                foreach (var positionOnDutyGroup in positionGroup.GroupBy(z => z.OnDuty))
                {
                    var index = 0;
                    foreach (var entry in positionOnDutyGroup)
                    {
                        entry.SmartWandOrPosition = index == 0 ? entry.SmartWandOrPosition : $"{entry.SmartWandOrPosition} [{index}]";
                        guardLoginsArranged.Add(entry);
                        index++;
                    }
                }
            }
            return  guardLoginsArranged.GroupBy(z => z.SmartWandOrPosition);
        }
    }
}
