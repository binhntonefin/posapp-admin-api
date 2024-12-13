using PosApp.Admin.Api.Data.Enums;
using PosApp.Admin.Api.Data.Models;
using PosApp.Admin.Api.Services.Contract;
using Microsoft.EntityFrameworkCore;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;
using URF.Core.EF.Trackable.Entities;
using URF.Core.EF.Trackable.Enums;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper;
using URF.Core.Helper.Extensions;

namespace PosApp.Admin.Api.Services.Implement
{
    public class TeamService : ITeamService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotifyService _notifyService;
        private readonly IRepositoryX<Team> _repository;
        private readonly IUtilityService _utilityService;
        private readonly IRepositoryX<UserTeam> _userTeamRepository;

        public TeamService(
            IUnitOfWork unitOfWork,
            INotifyService notifyService,
            IRepositoryX<Team> repository,
            IUtilityService utilityService,
            IRepositoryX<UserTeam> userTeamRepository)
        {
            _unitOfWork = unitOfWork;
            _repository = repository;
            _notifyService = notifyService;
            _utilityService = utilityService;
            _userTeamRepository = userTeamRepository;
        }

        public ResultApi AllTeams(int? userId)
        {
            var TeamIds = userId.IsNumberNull()
                ? new List<int>()
                : _userTeamRepository.Queryable().AsNoTracking()
                        .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                        .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                        .Where(c => c.UserId == userId)
                        .Select(c => c.TeamId)
                        .Distinct()
                        .ToList() ?? new List<int>();

            var query = _repository.Queryable().AsNoTracking()
                        .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                        .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                        .Select(c => new
                        {
                            c.Id,
                            c.Name,
                            c.Code,
                            c.Description,
                            Allow = TeamIds.Contains(c.Id),
                        });
            return ResultApi.ToEntity(query.ToList());
        }
        public async Task<ResultApi> Trash(int id)
        {
            var entity = await _repository.FindAsync(id);
            if (entity == null)
                return ResultApi.ToError(ErrorResult.Department.NotExists);

            if (!entity.IsDelete.HasValue || !entity.IsDelete.Value)
            {
                var count = _userTeamRepository.Queryable().FilterQueryNoTraking()
                    .Where(c => c.TeamId == id)
                    .Count();
                if (count > 0)
                    return ResultApi.ToError("Không thể xóa vì có " + count + " nhân viên trong nhóm");
            }

            entity.IsDelete = !entity.IsDelete;
            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return ResultApi.ToEntity(id);
        }
        public async Task<ResultApi> AddUsers(int id, List<int> items)
        {
            var userTeams = _userTeamRepository.Queryable().Where(c => c.TeamId == id).ToList() ?? new List<UserTeam>();
            if (!items.IsNullOrEmpty())
            {
                foreach (var userId in items)
                {
                    var itemDb = userTeams
                            .Where(c => c.TeamId == id)
                            .FirstOrDefault(c => c.UserId == userId);
                    if (itemDb == null)
                    {
                        itemDb = new UserTeam
                        {
                            TeamId = id,
                            UserId = userId
                        };
                        _userTeamRepository.Insert(itemDb);
                    }
                    else
                    {
                        itemDb.IsActive = true;
                        _userTeamRepository.Update(itemDb);
                    }
                }
                await _unitOfWork.SaveChangesAsync();

                await _notifyService.AddNotifyAsync(new Notify
                {
                    IsRead = false,
                    DateTime = DateTime.Now,
                    Type = (int)NotifyType.UpdateRole,
                    Title = "Admin hệ thống cập nhật lại nhóm",
                }, items);
            }
            return ResultApi.ToEntity(true);
        }

        public async Task<ResultApi> AddOrUpdateAsync(TeamModel model)
        {
            // check empty
            if (model == null ||
                model.Code.IsStringNullOrEmpty() ||
                model.Name.IsStringNullOrEmpty())
                return ResultApi.ToError(ErrorResult.DataInvalid);

            // save role
            var entity = Mapper.Map<Team>(model);
            if (entity.Id.IsNumberNull())
            {
                _repository.Insert(entity);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                var entityDb = await _repository.FindAsync(entity.Id);
                if (entityDb == null)
                    return ResultApi.ToError(ErrorResult.DataInvalid);

                entity.Id = entityDb.Id;
                entityDb = Mapper.MapTo<Team>(entity, entityDb);
                _repository.Update(entityDb);
                await _unitOfWork.SaveChangesAsync();
            }

            // update permission
            _utilityService.ResetCache(CachedType.Team);
            return await UpdateUsers(entity.Id, model.UserIds);
        }

        public async Task<ResultApi> UpdateUsers(int id, List<int> items)
        {
            var userTeams = _userTeamRepository.Queryable().Where(c => c.TeamId == id).ToList() ?? new List<UserTeam>();
            var nextIds = items.IsNullOrEmpty() ? new List<int>() : items.Distinct().OrderBy(c => c).ToList();
            var currentIds = userTeams.Select(c => c.UserId).Distinct().OrderBy(c => c).ToList();
            var needNotify = nextIds.ToJson() != currentIds.ToJson();
            foreach (var item in userTeams)
                item.IsActive = false;

            // update permission
            foreach (var item in userTeams) item.IsActive = false;

            if (!items.IsNullOrEmpty())
            {
                foreach (var userId in items)
                {
                    var itemDb = userTeams
                            .Where(c => c.TeamId == id)
                            .FirstOrDefault(c => c.UserId == userId);
                    if (itemDb == null)
                    {
                        itemDb = new UserTeam
                        {
                            TeamId = id,
                            UserId = userId
                        };
                        _userTeamRepository.Insert(itemDb);
                    }
                    else
                    {
                        itemDb.IsActive = true;
                        _userTeamRepository.Update(itemDb);
                    }
                }
                await _unitOfWork.SaveChangesAsync();
            }

            // notify
            //if (needNotify)
            //{
            //    var notEffectUsers = nextIds.Intersect(currentIds).Distinct().ToList();
            //    var unionUserIds = nextIds.Union(currentIds).Distinct().ToList();
            //    var userIds = unionUserIds
            //        .Where(c => !notEffectUsers.Contains(c))
            //        .Distinct()
            //        .ToList();
            //    if (!userIds.IsNullOrEmpty())
            //    {
            //        await _notifyService.AddNotifyAsync(new Notify
            //        {
            //            IsRead = false,
            //            DateTime = DateTime.Now,
            //            Type = (int)NotifyType.UpdateRole,
            //            Title = "Admin hệ thống cập nhật lại nhóm",
            //        }, userIds);
            //    }
            //}
            return ResultApi.ToEntity(true);
        }
    }
}
