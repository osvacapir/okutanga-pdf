namespace OlondongeApp.Services;

/// <summary>Chaves <see cref="IGradesLocalStore.SetMetaAsync"/> para JSON da última sincronização explícita.</summary>
public static class StudentPayloadCacheKeys
{
    public const string WeeklyScheduleJson = "student_payload_weekly_schedule";

    public const string CurriculumJson = "student_payload_curriculum";

    public const string FeesJson = "student_payload_fees";
}
