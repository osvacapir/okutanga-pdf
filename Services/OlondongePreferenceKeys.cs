namespace OlondongeApp.Services;

public static class OlondongePreferenceKeys
{
    public const string LastSyncPipelineTicks = "ol_last_sync_pipeline_ticks";

    public const string SavedGradesVersion = "ol_saved_grades_version";

    public const string LastEnrolmentsRefreshTicks = "ol_last_enrolments_refresh_ticks";

    /// <summary>Incrementar em <see cref="GradesSyncService"/> quando o payload de matrículas mudar (ex.: novo campo) para forçar novo GET mesmo com a mesma GradesVersion.</summary>
    public const string EnrolmentsApiContractVersion = "ol_enrolments_api_contract_version";
}
