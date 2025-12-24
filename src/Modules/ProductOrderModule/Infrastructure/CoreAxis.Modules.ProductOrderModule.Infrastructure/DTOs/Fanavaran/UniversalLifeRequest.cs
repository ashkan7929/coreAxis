using System.Text.Json.Serialization;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.DTOs.Fanavaran;

public class UniversalLifeRequest
{
    public int? CapitalReceiveId { get; set; }
    public int? PensionPayDuration { get; set; }
    public int? PensionPayPeriodId { get; set; }
    public int? GuaranteeDuration { get; set; }
    public int? PensionChangePercent { get; set; }
    public int? PensionDeathCalcKindId { get; set; }
    public int? AgentId { get; set; }
    public string? BeginDate { get; set; }
    public int? CapitalChangePercent { get; set; }
    public int? ContractId { get; set; }
    public long? CustomerId { get; set; }
    public int? CustomerJobId { get; set; }
    public int? Duration { get; set; }
    public decimal? FirstPrm { get; set; }
    public int? FirstPrmContainsExtraCov { get; set; }
    public decimal? FirstReserved { get; set; }
    public int? FreeRegionId { get; set; }
    public int? InsuredPersonCount { get; set; }
    public int? IsInFreeRegion { get; set; }
    public string? IssuDate { get; set; }
    public string? Note { get; set; }
    public int? PayPeriodId { get; set; }
    public string? PersonnelCode { get; set; }
    public int? PlanId { get; set; }
    public int? PrmChangePercent { get; set; }
    public int? SaleManagerId { get; set; }
    public string? SpecialCondition { get; set; }
    public int? WithMedicalExperiment { get; set; }
    public int? MarketerId { get; set; }
    public int? PolicyUsageTypeId { get; set; }
    public int? SalesTeamCompId { get; set; }
    
    public List<InsuredPerson>? InsuredPeople { get; set; }
    public List<FileItem>? Files { get; set; }
}

public class InsuredPerson
{
    public int? IncreasedAge { get; set; }
    public long? InsuredPersonId { get; set; }
    public int? InsuredPersonJobId { get; set; }
    public int? InsuredPersonRoleKindId { get; set; }
    public int? InsurerAndInsuredRelationId { get; set; }
    public int? MedicalRate { get; set; }
    
    public List<Beneficiary>? Beneficiaries { get; set; }
    public List<Cov>? Covs { get; set; }
    public List<DoctorRecommendation>? DoctorRecommendations { get; set; }
    public List<FamilyMedicalHistory>? FamilyMedicalHistories { get; set; }
    public List<MedicalHistory>? MedicalHistories { get; set; }
    public List<Surcharge>? Surcharges { get; set; }
}

public class Beneficiary
{
    public long? AnotherInsuredPersonId { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? BeneficiaryId { get; set; }
    
    public int? BeneficiaryKindId { get; set; }
    public int? BeneficiaryRelationId { get; set; }
    public int? CapitalPercent { get; set; }
    public int? PriorityId { get; set; }
}

public class Cov
{
    public decimal? CapitalAmount { get; set; }
    public int? CapitalRatio { get; set; }
    public int? CovKindId { get; set; }
}

public class DoctorRecommendation
{
    public string? Date { get; set; }
    public string? Desc { get; set; }
    public int? DoctorId { get; set; }
    public int? ExperimentsReasonId { get; set; }
    public int? MedicalProhibition { get; set; }
    public int? MedicalRatio { get; set; }
    public int? NeedMedicalExperiments { get; set; }
    public int? NeedSupplementalExperiments { get; set; }
}

public class FamilyMedicalHistory
{
    public int? BloodDisease { get; set; }
    public int? BoneDisease { get; set; }
    public int? BreatheDisease { get; set; }
    public int? CancerDisease { get; set; }
    public string? CauseDeathFamilyMembersBefore65 { get; set; }
    public string? Desc { get; set; }
    public int? Diabetes { get; set; }
    public int? DigestiveDisease { get; set; }
    public int? EndocrineDisease { get; set; }
    public int? ENTDisease { get; set; }
    public int? Epilepsy { get; set; }
    public int? EyeDisease { get; set; }
    public int? HeartDisease { get; set; }
    public int? HepatitsDisease { get; set; }
    public int? Hypertension { get; set; }
    public int? InfectiousDisease { get; set; }
    public int? InternalDisease { get; set; }
    public int? IsFamilyMemberDiedBefore65 { get; set; }
    public int? KidneyDisease { get; set; }
    public int? MentalDisease { get; set; }
    public int? MovementDisorder { get; set; }
    public int? NeurologicalDisease { get; set; }
    public int? Paroxysm { get; set; }
    public int? SkinDisease { get; set; }
    public int? Stroke { get; set; }
    public int? TuberculosisDisease { get; set; }
    public int? ZymoticDisease { get; set; }
}

public class MedicalHistory
{
    public string? AddictionAmount { get; set; }
    public string? AddictionDuration { get; set; }
    public int? BloodDisease { get; set; }
    public int? BoneDisease { get; set; }
    public int? BreatheDisease { get; set; }
    public int? CancerDisease { get; set; }
    public int? ChanegWeightIn6Month { get; set; }
    public string? Desc { get; set; }
    public int? Diabetes { get; set; }
    public string? DiabetesType { get; set; }
    public int? DigestiveDisease { get; set; }
    public int? Dismemberment { get; set; }
    public string? DismembermentCause { get; set; }
    public string? DismembermentPercent { get; set; }
    public int? EndocrineDisease { get; set; }
    public int? ENTDisease { get; set; }
    public int? Epilepsy { get; set; }
    public int? EyeDisease { get; set; }
    public string? HealthInsuranceName { get; set; }
    public int? HeartDisease { get; set; }
    public int? Height { get; set; }
    public int? HepatitsDisease { get; set; }
    public int? HIVDisease { get; set; }
    public int? Hospitalization { get; set; }
    public int? Hypertension { get; set; }
    public int? InfectiousDisease { get; set; }
    public int? InternalDisease { get; set; }
    public int? IsInsuredAddicted { get; set; }
    public int? IsMedicalExemptionMilitaryService { get; set; }
    public int? IsPregnant { get; set; }
    public int? KidneyDisease { get; set; }
    public string? MedicalExemptionMilitaryServiceCause { get; set; }
    public string? MedicineConsumptionIn24h { get; set; }
    public string? MedicineName { get; set; }
    public int? MentalDisease { get; set; }
    public int? MovementDisorder { get; set; }
    public int? MSDisease { get; set; }
    public int? NeurologicalDisease { get; set; }
    public int? Paroxysm { get; set; }
    public string? PregnancyMonth { get; set; }
    public int? SkinDisease { get; set; }
    public int? Stroke { get; set; }
    public string? SurgeryClinicAndPhysician { get; set; }
    public string? SurgeryDate { get; set; }
    public int? SurgeryHistory { get; set; }
    public string? SurgeryResult { get; set; }
    public string? SurgeryType { get; set; }
    public int? TuberculosisDisease { get; set; }
    public int? TumorDisease { get; set; }
    public int? UseMedicineContinuously { get; set; }
    public int? Weight { get; set; }
    public string? WeightChangeCause { get; set; }
    public string? WeightChanged { get; set; }
    public int? ZymoticDisease { get; set; }
}

public class Surcharge
{
    public string? ExerciseDuration { get; set; }
    public int? SurchargeId { get; set; }
}

public class FileItem
{
    public string? FileName { get; set; }
}
