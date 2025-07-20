using AutoMapper;
using DocumentExplorer.Core.Domain;
using DocumentExplorer.Infrastructure.DTO;
using Microsoft.Extensions.Logging;

namespace DocumentExplorer.Infrastructure.Mappers
{
    public static class AutoMapperConfig
    {
        public static IMapper Initialize(ILoggerFactory loggerFactory)
            => new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<User, UserDto>();
                cfg.CreateMap<Order, OrderDto>();
                cfg.ReplaceMemberName("CreationDateString", "Date");
                cfg.CreateMap<File,FileDto>();
                cfg.CreateMap<Order, ExtendedOrderDto>();
                cfg.CreateMap<Permissions,PermissionsDto>();
                cfg.CreateMap<Log,LogDto>();
            }, loggerFactory)
            .CreateMapper();
    }
}