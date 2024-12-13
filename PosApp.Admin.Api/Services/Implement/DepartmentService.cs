

using PosApp.Admin.Api.Data.Enums;
using PosApp.Admin.Api.Data.Models;
using PosApp.Admin.Api.Services.Contract;
using Microsoft.EntityFrameworkCore;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;
using URF.Core.EF.Trackable.Entities;
using URF.Core.EF.Trackable.Models;
using URF.Core.Helper;
using URF.Core.Helper.Extensions;

namespace PosApp.Admin.Api.Services.Implement
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;
        private readonly IRepositoryX<User> _userRepository;
        private readonly IRepositoryX<Department> _repository;

        public DepartmentService(
            IUnitOfWork unitOfWork,
            IUtilityService utilityService,
            IRepositoryX<User> userRepository,
            IRepositoryX<Department> repository)
        {
            _unitOfWork = unitOfWork;
            _repository = repository;
            _utilityService = utilityService;
            _userRepository = userRepository;
        }

        public async Task<ResultApi> Trash(int id)
        {
            var entity = await _repository.FindAsync(id);
            if (entity == null)
                return ResultApi.ToError(ErrorResult.Department.NotExists);

            if (!entity.IsDelete.HasValue || !entity.IsDelete.Value)
            {
                var count = _userRepository.Queryable().AsNoTracking()
                    .Where(c => !c.IsDelete.HasValue || !c.IsDelete.Value)
                    .Where(c => !c.IsActive.HasValue || c.IsActive.Value)
                    .Where(c => c.DepartmentId.HasValue)
                    .Where(c => c.DepartmentId.Value == id)
                    .Count();
                if (count > 0)
                    return ResultApi.ToError("Không thể xóa vì có " + count + " nhân viên trong phòng ban");
            }

            entity.IsDelete = !entity.IsDelete;
            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return ResultApi.ToEntity(id);
        }

        public async Task<ResultApi> AddUsers(int id, List<int> items)
        {
            // add DepartmentId
            var users = items.IsNullOrEmpty()
                ? new List<User>()
                : _userRepository.Queryable().Where(c => items.Contains(c.Id)).ToList() ?? new List<User>();
            foreach (var item in users)
            {
                item.DepartmentId = id;
                _userRepository.Update(item);
            }
            await _unitOfWork.SaveChangesAsync();
            return ResultApi.ToEntity(true);
        }

        public async Task<ResultApi> UpdateUsers(int id, List<int> items)
        {
            // remove DepartmentId
            var removeUsers = _userRepository.Queryable()
                .Where(c => c.DepartmentId.HasValue)
                .Where(c => c.DepartmentId.Value == id)
                .ToList() ?? new List<User>();
            foreach (var item in removeUsers)
            {
                item.DepartmentId = null;
                _userRepository.Update(item);
            }

            // add DepartmentId
            var users = items.IsNullOrEmpty()
                ? new List<User>()
                : _userRepository.Queryable().Where(c => items.Contains(c.Id)).ToList() ?? new List<User>();
            foreach (var item in users)
            {
                item.DepartmentId = id;
                _userRepository.Update(item);
            }
            await _unitOfWork.SaveChangesAsync();
            return ResultApi.ToEntity(true);
        }

        public async Task<ResultApi> AddOrUpdateAsync(DepartmentModel model)
        {
            // check empty
            if (model == null ||
                model.Code.IsStringNullOrEmpty() ||
                model.Name.IsStringNullOrEmpty())
                return ResultApi.ToError(ErrorResult.DataInvalid);

            // save role
            var entity = Mapper.Map<Department>(model);
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
                entityDb = Mapper.MapTo<Department>(entity, entityDb);
                _repository.Update(entityDb);
                await _unitOfWork.SaveChangesAsync();
            }

            // update permission
            _utilityService.ResetCache(CachedType.Department);
            return await UpdateUsers(entity.Id, model.UserIds);
        }
    }
}
