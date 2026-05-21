using AutoMapper;
using SponsorshipApproval.Application.Requests.Models;
using SponsorshipApproval.Domain.Requests;

namespace SponsorshipApproval.Application.Requests.Mapping;

public sealed class RequestMappingProfile : Profile
{
    public RequestMappingProfile()
    {
        CreateMap<SponsorshipRequest, RequestDetailDto>()
            .ForMember(
                destination => destination.SponsorshipTypeName,
                member => member.MapFrom(source => source.SponsorshipType!.Name));

        CreateMap<SponsorshipRequest, RequestListItemDto>()
            .ForMember(
                destination => destination.SponsorshipTypeName,
                member => member.MapFrom(source => source.SponsorshipType!.Name));
    }
}
