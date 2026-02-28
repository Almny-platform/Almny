using Mapster;

namespace Almny.Api.Mapping;

public class MappingConfigurations : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<RegisterRequest, ApplicationUser>()
            .Map(dest => dest.UserName, src => src.Email)
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.FullName, src => src.FullName)
            .Ignore(dest => dest.Id);
    }
}
