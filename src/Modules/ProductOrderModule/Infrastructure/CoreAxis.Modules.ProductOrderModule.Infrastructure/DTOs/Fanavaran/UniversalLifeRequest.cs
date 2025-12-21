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
    public int AgentId { get; set; } = 1035;
    public string BeginDate { get; set; }
    public int CapitalChangePercent { get; set; }
    public int ContractId { get; set; } = 10743;
    public long CustomerId { get; set; }
    public int CustomerJobId { get; set; } = 1159; // Hardcoded default for now
    public int Duration { get; set; }
    public decimal FirstPrm { get; set; }
    public int FirstPrmContainsExtraCov { get; set; } = 0;
    public decimal FirstReserved { get; set; } = 0;
    public int? FreeRegionId { get; set; }
    public int InsuredPersonCount { get; set; } = 58;
    public int IsInFreeRegion { get; set; } = 0;
    public string? IssuDate { get; set; }
    public string Note { get; set; } = "يادداشت متفرقه";
    public int PayPeriodId { get; set; } = 275;
    public string? PersonnelCode { get; set; }
    public int PlanId { get; set; } = 21;
    public int PrmChangePercent { get; set; }
    public int SaleManagerId { get; set; } = 1035;
    public string SpecialCondition { get; set; } = "شرايط خصوصي";
    public int? WithMedicalExperiment { get; set; }
    public int? MarketerId { get; set; }
    public int PolicyUsageTypeId { get; set; } = 2898;
    public int? SalesTeamCompId { get; set; }
    
    public List<InsuredPerson> InsuredPeople { get; set; } = new();
    // public List<FileItem> Files { get; set; } = new();
}

public class InsuredPerson
{
    public int IncreasedAge { get; set; } = 7;
    public long InsuredPersonId { get; set; }
    public int InsuredPersonJobId { get; set; } = 2;
    public int InsuredPersonRoleKindId { get; set; } = 793;
    public int InsurerAndInsuredRelationId { get; set; } = 105;
    public int MedicalRate { get; set; } = 0;
    
    public List<Beneficiary> Beneficiaries { get; set; } = new();
    public List<Cov> Covs { get; set; } = new();
    public List<DoctorRecommendation> DoctorRecommendations { get; set; } = new();
    public List<FamilyMedicalHistory> FamilyMedicalHistories { get; set; } = new();
    public List<MedicalHistory> MedicalHistories { get; set; } = new();
    public List<Surcharge> Surcharges { get; set; } = new();
}

public class Beneficiary
{
    public long? AnotherInsuredPersonId { get; set; }
    public long? BeneficiaryId { get; set; }
    public int BeneficiaryKindId { get; set; }
    public int BeneficiaryRelationId { get; set; }
    public int CapitalPercent { get; set; }
    public int PriorityId { get; set; }
}

public class Cov
{
    public decimal? CapitalAmount { get; set; }
    public int? CapitalRatio { get; set; }
    public int CovKindId { get; set; }
}

public class DoctorRecommendation
{
    public string Date { get; set; } = "1403/11/08";
    public string? Desc { get; set; }
    public int? DoctorId { get; set; }
    public int? ExperimentsReasonId { get; set; }
    public int MedicalProhibition { get; set; } = 0;
    public int MedicalRatio { get; set; } = 0;
    public int NeedMedicalExperiments { get; set; } = 0;
    public int NeedSupplementalExperiments { get; set; } = 0;
}

public class FamilyMedicalHistory
{
    public int BloodDisease { get; set; } = 0;
    public int BoneDisease { get; set; } = 0;
    public int BreatheDisease { get; set; } = 0;
    public int CancerDisease { get; set; } = 0;
    public string? CauseDeathFamilyMembersBefore65 { get; set; }
    public string? Desc { get; set; }
    public int Diabetes { get; set; } = 0;
    public int DigestiveDisease { get; set; } = 0;
    public int EndocrineDisease { get; set; } = 0;
    public int ENTDisease { get; set; } = 0;
    public int Epilepsy { get; set; } = 0;
    public int EyeDisease { get; set; } = 0;
    public int HeartDisease { get; set; } = 0;
    public int HepatitsDisease { get; set; } = 0;
    public int Hypertension { get; set; } = 0;
    public int InfectiousDisease { get; set; } = 0;
    public int InternalDisease { get; set; } = 0;
    public int IsFamilyMemberDiedBefore65 { get; set; } = 0;
    public int KidneyDisease { get; set; } = 0;
    public int MentalDisease { get; set; } = 0;
    public int MovementDisorder { get; set; } = 0;
    public int NeurologicalDisease { get; set; } = 0;
    public int Paroxysm { get; set; } = 0;
    public int SkinDisease { get; set; } = 0;
    public int Stroke { get; set; } = 0;
    public int TuberculosisDisease { get; set; } = 0;
    public int ZymoticDisease { get; set; } = 0;
}

public class MedicalHistory
{
    public string? AddictionAmount { get; set; }
    public string? AddictionDuration { get; set; }
    public int BloodDisease { get; set; } = 0;
    public int BoneDisease { get; set; } = 0;
    public int BreatheDisease { get; set; } = 0;
    public int CancerDisease { get; set; } = 0;
    public int ChanegWeightIn6Month { get; set; } = 0;
    public string? Desc { get; set; }
    public int Diabetes { get; set; } = 0;
    public string? DiabetesType { get; set; }
    public int DigestiveDisease { get; set; } = 0;
    public int Dismemberment { get; set; } = 0;
    public string? DismembermentCause { get; set; }
    public string? DismembermentPercent { get; set; }
    public int EndocrineDisease { get; set; } = 0;
    public int ENTDisease { get; set; } = 0;
    public int Epilepsy { get; set; } = 0;
    public int EyeDisease { get; set; } = 0;
    public string HealthInsuranceName { get; set; } = "تامين اجتماعي، تکميلي درمان";
    public int HeartDisease { get; set; } = 0;
    public int Height { get; set; }
    public int HepatitsDisease { get; set; } = 0;
    public int HIVDisease { get; set; } = 0;
    public int Hospitalization { get; set; } = 0;
    public int Hypertension { get; set; } = 0;
    public int InfectiousDisease { get; set; } = 0;
    public int InternalDisease { get; set; } = 0;
    public int IsInsuredAddicted { get; set; } = 0;
    public int IsMedicalExemptionMilitaryService { get; set; } = 0;
    public int IsPregnant { get; set; } = 0;
    public int KidneyDisease { get; set; } = 0;
    public string? MedicalExemptionMilitaryServiceCause { get; set; }
    public string? MedicineConsumptionIn24h { get; set; }
    public string? MedicineName { get; set; }
    public int MentalDisease { get; set; } = 0;
    public int MovementDisorder { get; set; } = 0;
    public int MSDisease { get; set; } = 0;
    public int NeurologicalDisease { get; set; } = 0;
    public int Paroxysm { get; set; } = 0;
    public string? PregnancyMonth { get; set; }
    public int SkinDisease { get; set; } = 0;
    public int Stroke { get; set; } = 0;
    public string? SurgeryClinicAndPhysician { get; set; }
    public string? SurgeryDate { get; set; }
    public int SurgeryHistory { get; set; } = 0;
    public string? SurgeryResult { get; set; }
    public string? SurgeryType { get; set; }
    public int TuberculosisDisease { get; set; } = 0;
    public int TumorDisease { get; set; } = 0;
    public int UseMedicineContinuously { get; set; } = 0;
    public int Weight { get; set; }
    public string? WeightChangeCause { get; set; }
    public string? WeightChanged { get; set; }
    public int ZymoticDisease { get; set; } = 0;
}

public class Surcharge
{
    public string? ExerciseDuration { get; set; }
    public int SurchargeId { get; set; }
}

public class FileItem
{
    public string FileName { get; set; } = string.Empty;
}
