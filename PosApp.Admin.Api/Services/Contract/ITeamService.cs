using URF.Core.EF.Trackable.Models;

namespace PosApp.Admin.Api.Services.Contract
{
    public interface ITeamService
    {
        public Task<ResultApi> Trash(int id);
        public ResultApi AllTeams(int? userId);
        public Task<ResultApi> AddUsers(int id, List<int> items);
        public Task<ResultApi> AddOrUpdateAsync(TeamModel model);
        public Task<ResultApi> UpdateUsers(int id, List<int> items);
    }
}
