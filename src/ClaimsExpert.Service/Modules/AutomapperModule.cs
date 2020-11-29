using System;
using Autofac;
using AutoMapper;
using NRules.Samples.ClaimsExpert.Contract;
using NRules.Samples.ClaimsExpert.Domain;

namespace NRules.Samples.ClaimsExpert.Service.Modules
{
    public class AutomapperModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AllowNullDestinationValues = false;

                cfg.CreateMap<Claim, ClaimDto>()
                    .ForMember(dest => dest.PatientFirstName, x => x.MapFrom(src => src.Patient.Name.FirstName))
                    .ForMember(dest => dest.PatientMiddleName, x => x.MapFrom(src => src.Patient.Name.MiddleName))
                    .ForMember(dest => dest.PatientLastName, x => x.MapFrom(src => src.Patient.Name.LastName));

                cfg.CreateMap<ClaimAlert, ClaimAlertDto>();

                cfg.CreateMap<ClaimStatus, AdjudicationStatus>()
                    .ConvertUsing((src, tgt) => Enum.TryParse(src.ToString(), true, out AdjudicationStatus result)
                        ? result
                        : AdjudicationStatus.Open);

                cfg.CreateMap<ClaimType, string>()
                    .ConvertUsing(src => src.ToString());
            });
            var mapper = config.CreateMapper();
            builder.Register(c => mapper).As<IMapper>();
        }
    }
}